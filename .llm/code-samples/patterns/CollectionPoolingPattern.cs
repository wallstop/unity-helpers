// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// Collection pooling pattern - zero allocation in steady state
// Use Buffers<T> for temporary collections

namespace WallstopStudios.UnityHelpers.Examples
{
    using System.Collections.Generic;
    using System.Text;
    using WallstopStudios.UnityHelpers.Core.Helper;

    public sealed class CollectionPoolingExamples
    {
        // List pooling - returns collection to pool when lease is disposed
        public void ProcessWithPooledList()
        {
            using var listLease = Buffers<string>.List.Get(out List<string> buffer);
            // Use buffer...
            buffer.Add("item1");
            buffer.Add("item2");
            // Buffer returned to pool when 'using' scope ends
        }

        // HashSet pooling
        public void ProcessWithPooledHashSet()
        {
            using var setLease = Buffers<int>.HashSet.Get(out HashSet<int> buffer);
            buffer.Add(1);
            buffer.Add(2);
            // Buffer returned to pool when 'using' scope ends
        }

        // StringBuilder pooling
        public string BuildStringWithPool()
        {
            using var sbLease = Buffers.StringBuilder.Get(out StringBuilder sb);
            sb.Append("Hello");
            sb.Append(" ");
            sb.Append("World");
            return sb.ToString();
        }

        // Caller-provides-buffer pattern - zero allocation
        public void GetActiveEnemies(List<Enemy> result)
        {
            result.Clear();
            for (int i = 0; i < _enemies.Count; i++)
            {
                Enemy enemy = _enemies[i];
                if (enemy.IsActive)
                {
                    result.Add(enemy);
                }
            }
        }

        // Usage with pooling
        public void ProcessActiveEnemies()
        {
            using var lease = Buffers<Enemy>.List.Get(out List<Enemy> activeEnemies);
            GetActiveEnemies(activeEnemies);
            // Process activeEnemies...
        }

        private readonly List<Enemy> _enemies = new List<Enemy>();

        // Dummy class for example
        public sealed class Enemy
        {
            public bool IsActive { get; set; }
        }
    }
}
