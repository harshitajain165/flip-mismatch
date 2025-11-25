using System.Collections.Generic;
using UnityEngine;

namespace CardMatching.GamePlay
{
    // Simple distributor that arranges Card prefabs in a rows x cols grid
    public class GridPrefabDistributor : IGridDistributor
    {
        private RectTransform parent;
        private int rows;
        private int cols;
        private List<Card> items;

        public GridPrefabDistributor(RectTransform parent, int rows, int cols, List<Card> items)
        {
            this.parent = parent;
            this.rows = Mathf.Max(1, rows);
            this.cols = Mathf.Max(1, cols);
            this.items = items ?? new List<Card>();
        }

        public void DistributePrefabs()
        {
            if (parent == null || items == null || items.Count == 0) return;

            // Use parent's rect to calculate cell sizes
            var rect = parent.rect;
            float cellW = rect.width / cols;
            float cellH = rect.height / rows;

            for (int i = 0; i < items.Count; i++)
            {
                var card = items[i];
                if (card == null) continue;

                var rt = card.GetComponent<RectTransform>();
                if (rt == null) continue;

                int r = i / cols;
                int c = i % cols;

                // Calculate anchored position relative to center anchor (0.5,0.5)
                float x = -rect.width * 0.5f + cellW * c + cellW * 0.5f;
                float y = rect.height * 0.5f - cellH * r - cellH * 0.5f;

                rt.SetParent(parent, false);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, y);
                rt.sizeDelta = new Vector2(cellW, cellH);
            }
        }
    }
}
