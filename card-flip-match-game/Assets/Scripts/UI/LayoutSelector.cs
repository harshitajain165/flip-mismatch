using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CardMatching.GamePlay;

namespace CardMatching.GamePlay
{
    // Simple UI helper to allow players to choose a board layout at runtime.
    // Attach to a GameObject and assign a UnityEngine.UI.Dropdown in the inspector.
    public class LayoutSelector : MonoBehaviour
    {
        [Tooltip("Dropdown UI used to select predefined layouts (TextMeshPro TMP_Dropdown)")]
        public TMP_Dropdown dropdown;

        [Tooltip("Presets in the format ROWxCOL, e.g. 3x4")]
        public List<string> presets = new List<string> { "2x2", "2x3", "3x4", "4x4", "4x5", "5x6", "6x6" };

        private void Awake()
        {
            if (dropdown == null)
                dropdown = GetComponent<TMP_Dropdown>();
        }

        private void Start()
        {
            if (dropdown == null) return;

            dropdown.ClearOptions();
            // Add a non-selectable placeholder at index 0 so the dropdown displays a label like "Layout"
            var options = new List<string>();
            options.Add("LAYOUT");
            options.AddRange(presets);
            dropdown.AddOptions(options);
            dropdown.onValueChanged.AddListener(OnSelectionChanged);
            // ensure placeholder is shown initially
            dropdown.value = 0;
            dropdown.RefreshShownValue();
        }

        private void OnDestroy()
        {
            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(OnSelectionChanged);
        }

        private void OnSelectionChanged(int idx)
        {
            // index 0 is the placeholder - ignore
            if (idx <= 0) return;
            int presetIndex = idx - 1; // adjust because presets start at index 1 in the dropdown
            if (presetIndex < 0 || presetIndex >= presets.Count) return;
            var s = presets[presetIndex];
            var parts = s.Split('x');
            if (parts.Length != 2) return;

            if (int.TryParse(parts[0], out int r) && int.TryParse(parts[1], out int c))
            {
                // Start the chosen layout via GamePlayManager singleton
                GamePlayManager.GetInstance.StartGame(r, c);
            }
        }
    }
}
