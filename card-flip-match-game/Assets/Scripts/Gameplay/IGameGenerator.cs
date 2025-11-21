using System.Collections.Generic;
using UnityEngine;

namespace CardMatching.GamePlay
{
    public interface IGameGenerator
    {
        List<int> GenerateCardMatchGame(int totalCards, int spriteCount);
    }
}
