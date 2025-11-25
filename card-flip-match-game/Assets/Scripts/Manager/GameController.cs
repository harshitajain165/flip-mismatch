using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using CardMatching.GamePlay;

namespace CardMatching.GamePlay
{
    public class GameController : MonoBehaviour
    {
        [Header("Board")]
        public RectTransform boardParent; // assign BoardContainer
        public GameObject cardPrefab; // Card prefab
        public List<Sprite> sprites; // pool of sprites to assign, index is spriteId

        [Header("UI")]
        public TMPro.TextMeshProUGUI scoreText;
        public TMPro.TextMeshProUGUI turnText;
        public TMPro.TextMeshProUGUI matchText;

        [Header("Settings")]
        public int rows = 3;
        public int cols = 4;

        [Header("Audio")]
        public AudioClip flipClip;
        public AudioClip matchClip;
        public AudioClip mismatchClip;
        public AudioClip gameOverClip;
        private AudioSource audioSource;

        private List<Card> spawnedCards = new List<Card>();
        private Queue<Card> flipQueue = new Queue<Card>();
        private bool processorRunning = false;

        // generator + distributor
        private IGameGenerator generator;
        private IGridDistributor distributor;

        private ScoreManager scoreManager;

        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            generator = new RandomGameGenerator();
            scoreManager = ScoreManager.GetInstance;
        }

        private void Start()
        {
            // Hook GamePlayManager to start game
            GamePlayManager.GetInstance.OnGameStart += OnGameStart;
            GamePlayManager.GetInstance.OnGameOver += OnGameOver;
            // start default level
            GamePlayManager.GetInstance.StartGame(rows, cols);
        }

        private void OnDestroy()
        {
            GamePlayManager.GetInstance.OnGameStart -= OnGameStart;
            GamePlayManager.GetInstance.OnGameOver -= OnGameOver;
        }

        private void OnGameStart(int r, int c)
        {
            rows = r; cols = c;
            ClearBoard();
            BuildBoard();
            UpdateUI();
        }

        private void ClearBoard()
        {
            foreach (var c in spawnedCards)
            {
                if (c != null) Destroy(c.gameObject);
            }
            spawnedCards.Clear();
            flipQueue.Clear();
            scoreManager.Reset();
        }

        private void BuildBoard()
        {
            int requestedTotal = rows * cols;

            if (sprites == null || sprites.Count == 0)
            {
                Debug.LogError("No sprites assigned in GameController. Assign card face sprites before building the board.");
                return;
            }

            var ids = generator.GenerateCardMatchGame(requestedTotal, sprites.Count);
            int total = ids != null ? ids.Count : 0;

            if (total == 0)
            {
                Debug.LogError($"Generator returned no card ids for requested total={requestedTotal}.");
                return;
            }

            // create card instances
            for (int i = 0; i < total; i++)
            {
                GameObject go = Instantiate(cardPrefab, boardParent);
                Card card = go.GetComponent<Card>();
                card.Initialize(ids[i], sprites[ids[i]]);
                // wire events
                card.OnFlipped += HandleCardFlipped;
                card.OnMatched += HandleCardMatched;

                // add click hooking — if using Button:
                var btn = go.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => card.OnPointerClick());
                }
                else
                {
                    // alternative: add EventTrigger or IPointerClick
                }

                spawnedCards.Add(card);
            }

            // distribute using GridPrefabDistributor
            distributor = new GridPrefabDistributor(boardParent, rows, cols, spawnedCards);
            distributor.DistributePrefabs();
        }

        private void HandleCardFlipped(Card card)
        {
            // play flip sound
            PlaySfx(flipClip);

            // enqueue for comparison
            flipQueue.Enqueue(card);

            // start processor if not running
            if (!processorRunning)
                StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            processorRunning = true;
            while (flipQueue.Count >= 2)
            {
                var first = flipQueue.Dequeue();
                var second = flipQueue.Dequeue();

                // If either card was matched or flipped-down meanwhile, skip gracefully
                if (first.IsMatched || second.IsMatched) continue;

                // If they are the same object (unlikely), skip
                if (first == second) continue;

                // Compare
                if (first.SpriteId == second.SpriteId)
                {
                    // matched
                    first.MarkMatched();
                    second.MarkMatched();
                    scoreManager.UpdateMatchCount();
                    scoreManager.UpdateTurnCount();
                    PlaySfx(matchClip);
                    UpdateUI();

                    // small delay to let match animation sound be heard
                    yield return new WaitForSeconds(0.15f);

                    // check game over
                    if (GamePlayManager.GetInstance.CheckGameOver())
                    {
                        // will invoke OnGameOver
                    }
                }
                else
                {
                    // not matched
                    scoreManager.UpdateTurnCount();
                    scoreManager.ResetCombo(); // reset combo if mismatch
                    PlaySfx(mismatchClip);

                    // flip them back after short delay, but allow other flips meanwhile (we are in coroutine)
                    StartCoroutine(first.FlipToBackRoutine(0.4f));
                    StartCoroutine(second.FlipToBackRoutine(0.4f));
                    UpdateUI();

                    // delay to leave time for player to see (but we don't block flipping)
                    yield return new WaitForSeconds(0.45f);
                }
            }
            processorRunning = false;
        }

        private void HandleCardMatched(Card c)
        {
            var btn = c.GetComponent<UnityEngine.UI.Button>();
            if (btn) btn.interactable = false;

            UpdateUI();
        }

        private void UpdateUI()
        {
            scoreText.text = $"Score: {scoreManager.Score}";
            turnText.text = $"Turns: {scoreManager.TurnCount}";
            matchText.text = $"Matches: {scoreManager.MatchCount}";
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            audioSource.PlayOneShot(clip);
        }

        private void OnGameOver()
        {
            PlaySfx(gameOverClip);
            // show simple dialog or UI text
            Debug.Log("Game Over!");
        }

        // Save & Load (simple JSON)
        [ContextMenu("SaveState")]
        public void SaveState()
        {
            var state = new SaveData
            {
                rows = this.rows,
                cols = this.cols,
                score = scoreManager.Score,
                turn = scoreManager.TurnCount,
                match = scoreManager.MatchCount,
                cardSpriteIds = spawnedCards.ConvertAll(c => c.SpriteId),
                matchedFlags = spawnedCards.ConvertAll(c => c.IsMatched)
            };

            string json = JsonUtility.ToJson(state);
            string path = Path.Combine(Application.persistentDataPath, "savegame.json");
            File.WriteAllText(path, json);
            Debug.Log($"Saved to {path}");
        }

        [ContextMenu("LoadState")]
        public void LoadState()
        {
            string path = Path.Combine(Application.persistentDataPath, "savegame.json");
            if (!File.Exists(path))
            {
                Debug.LogWarning("No save file found.");
                return;
            }

            string json = File.ReadAllText(path);
            var state = JsonUtility.FromJson<SaveData>(json);

            // Clear the board first
            ClearBoard();
            rows = state.rows;
            cols = state.cols;

            // Rebuild board using EXACT saved sprite order (not regenerated)
            for (int i = 0; i < state.cardSpriteIds.Count; i++)
            {
                GameObject go = Instantiate(cardPrefab, boardParent);
                Card card = go.GetComponent<Card>();
                int spriteId = state.cardSpriteIds[i];
                card.Initialize(spriteId, sprites[spriteId]);

                // Apply saved matched state
                if (state.matchedFlags[i])
                {
                    card.RevealInstant();
                    card.MarkMatched();
                    var btn = card.GetComponent<UnityEngine.UI.Button>();
                    if (btn) btn.interactable = false;
                }

                card.OnFlipped += HandleCardFlipped;
                card.OnMatched += HandleCardMatched;
                spawnedCards.Add(card);
            }

            // Distribute cards in the grid
            distributor = new GridPrefabDistributor(boardParent, rows, cols, spawnedCards);
            distributor.DistributePrefabs();

            // Restore score/turn/match counters
            scoreManager.Reset();
            for (int i = 0; i < state.match; i++) scoreManager.UpdateMatchCount();
            for (int i = 0; i < state.turn; i++) scoreManager.UpdateTurnCount();
            UpdateUI();
            
            Debug.Log("Game state loaded successfully.");
        }

        [System.Serializable]
        private class SaveData
        {
            public int rows;
            public int cols;
            public int score;
            public int turn;
            public int match;
            public List<int> cardSpriteIds;
            public List<bool> matchedFlags;
        }

        // UI Hook methods
        public void OnReplay()
        {
            GamePlayManager.GetInstance.Replay();
        }

        public void OnSaveButton()
        {
            SaveState();
        }

        public void OnLoadButton()
        {
            LoadState();
        }
    }
}
