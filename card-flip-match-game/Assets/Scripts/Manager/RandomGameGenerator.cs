using System.Collections.Generic;
using UnityEngine;

namespace CardMatching.GamePlay
{
    public class RandomGameGenerator : IGameGenerator
    {
        public List<int> GenerateCardMatchGame(int totalCards, int spriteCount)
        {
            // totalCards must be even
            if (totalCards % 2 != 0) totalCards--;

            int neededPairs = totalCards / 2;
            var ids = new List<int>();

            // Simple selection: take sprite ids 0..spriteCount-1 cyclically and duplicate
            int spriteIndex = 0;
            for (int i = 0; i < neededPairs; i++)
            {
                ids.Add(spriteIndex);
                ids.Add(spriteIndex);
                spriteIndex = (spriteIndex + 1) % spriteCount;
            }

            // Shuffle
            for (int i = 0; i < ids.Count; i++)
            {
                int r = Random.Range(i, ids.Count);
                int tmp = ids[i];
                ids[i] = ids[r];
                ids[r] = tmp;
            }

            return ids;
        }
    }
}
