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
            int total = rows * cols;
            var ids = generator.GenerateCardMatchGame(total, sprites.Count);

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
            // additional visual logic for matched cards (e.g., disable button)
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
    }
}
