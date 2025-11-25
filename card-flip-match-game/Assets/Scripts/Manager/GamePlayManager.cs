using System;

namespace CardMatching.GamePlay
{
    // Lightweight non-Mono singleton to coordinate high-level game state
    public class GamePlayManager
    {
        private static GamePlayManager instance;
        public static GamePlayManager GetInstance => instance ?? (instance = new GamePlayManager());

        // fired to notify listeners to build/reset the board; provides rows, cols
        public event Action<int, int> OnGameStart;

        // fired when the game is over (all pairs found)
        public event Action OnGameOver;

        private int rows;
        private int cols;
        private int totalPairs;

        private GamePlayManager() { }

        public void StartGame(int r, int c)
        {
            rows = Math.Max(1, r);
            cols = Math.Max(1, c);
            totalPairs = (rows * cols) / 2;

            // reset ScoreManager counts when starting a new game
            ScoreManager.GetInstance.Reset();

            OnGameStart?.Invoke(rows, cols);
        }

        // Restart the current game layout (replay the same rows x cols)
        public void Replay()
        {
            StartGame(rows, cols);
        }

        // returns true if the game is over (and raises OnGameOver once)
        public bool CheckGameOver()
        {
            if (totalPairs <= 0) return false;

            if (ScoreManager.GetInstance.MatchCount >= totalPairs)
            {
                OnGameOver?.Invoke();
                return true;
            }
            return false;
        }
    }
}
