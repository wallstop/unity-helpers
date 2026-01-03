// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// String building best practices - avoid allocations in hot paths

namespace WallstopStudios.UnityHelpers.Examples
{
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using UnityEngine.UI;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public sealed class StringBuilderExamples : MonoBehaviour
    {
        [SerializeField]
        private Text _label;

        private int _health;
        private int _lastHealth = -1;

        // BAD: Concatenation in loop - O(n^2) allocations
        public string BadStringBuilding(List<Item> items)
        {
            string result = "";
            for (int i = 0; i < items.Count; i++)
            {
                result += items[i].Name; // New string each iteration!
            }
            return result;
        }

        // GOOD: StringBuilder with pooling - zero allocation in steady state
        public string GoodStringBuilding(List<Item> items)
        {
            using var lease = Buffers.StringBuilder.Get(out StringBuilder sb);
            for (int i = 0; i < items.Count; i++)
            {
                sb.Append(items[i].Name);
            }
            return sb.ToString();
        }

        // GOOD: Complex formatting with StringBuilder
        public string BuildResultString(List<Item> items)
        {
            using var lease = Buffers.StringBuilder.Get(out StringBuilder sb);
            sb.Append("Results: ");
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(items[i].Name);
                sb.Append(" (");
                sb.Append(items[i].Score);
                sb.Append(")");
            }
            return sb.ToString();
        }

        // HOT PATH RULE: Cache and only update when changed
        private void Update()
        {
            // BAD: Allocates every frame
            // _label.text = $"Health: {_health}";

            // GOOD: Only update when changed
            if (_health != _lastHealth)
            {
                _lastHealth = _health;
                using var lease = Buffers.StringBuilder.Get(out StringBuilder sb);
                sb.Append("Health: ");
                sb.Append(_health);
                _label.text = sb.ToString();
            }
        }

        // Dummy class for example
        public sealed class Item
        {
            public string Name { get; set; }
            public int Score { get; set; }
        }
    }

    // Decision Guide:
    // | Context                   | Recommended Approach        |
    // | Hot paths (Update, loops) | StringBuilder via pooling   |
    // | Two strings               | Direct + is fine            |
    // | 3+ parts, non-hot path    | String interpolation        |
    // | Building in loops         | ALWAYS StringBuilder        |
}
