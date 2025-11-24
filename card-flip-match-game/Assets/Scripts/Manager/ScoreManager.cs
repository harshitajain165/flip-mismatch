using System;
using UnityEngine;

namespace CardMatching.GamePlay
{
    public class ScoreManager
    {
        private static ScoreManager instance;
        public static ScoreManager GetInstance => instance ?? (instance = new ScoreManager());

        public int MatchCount { get; private set; }
        public int TurnCount { get; private set; }
        public int Score { get; private set; }

        private int comboCount;

        private ScoreManager() { Reset(); }

        public void Reset()
        {
            MatchCount = 0;
            TurnCount = 0;
            Score = 0;
            comboCount = 0;
        }

        public void UpdateMatchCount()
        {
            MatchCount++;
            comboCount++;
            Score += 100 + (comboCount - 1) * 20; // base 100 + combo bonus
        }

        public void UpdateTurnCount()
        {
            TurnCount++;
            // reset combo when a turn happens that isn't a match — handled externally
        }

        public void ResetCombo()
        {
            comboCount = 0;
        }
    }
}
