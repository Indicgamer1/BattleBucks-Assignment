using System.Collections.Generic;

namespace BattleBucks.Player
{
    // Pure C# — owns all PlayerData. No LINQ, pre-allocated arrays throughout.
    public sealed class PlayerRegistry
    {
        private readonly List<PlayerData>  _all;
        private readonly PlayerData[]      _sortedCache;   // pre-allocated sort buffer
        private readonly List<PlayerData>  _aliveScratch;  // scratch list for random pick

        // Reusable comparer — avoids closure allocation on every sort call
        private static readonly ScoreComparer _comparer = new ScoreComparer();

        public int Count => _all.Count;

        public PlayerRegistry(int capacity)
        {
            _all         = new List<PlayerData>(capacity);
            _sortedCache = new PlayerData[capacity];
            _aliveScratch = new List<PlayerData>(capacity);
        }

        public void Initialise(int count)
        {
            _all.Clear();
            for (int i = 0; i < count; i++)
                _all.Add(new PlayerData(i, $"Player {i + 1}"));
        }

        public PlayerData Get(int index) => _all[index];

        /// <summary>Sorted snapshot, highest score first. Do NOT cache — reused buffer.</summary>
        public PlayerData[] GetSortedSnapshot(out int count)
        {
            count = _all.Count;
            for (int i = 0; i < count; i++)
                _sortedCache[i] = _all[i];

            System.Array.Sort(_sortedCache, 0, count, _comparer); // reusable comparer — no closure alloc
            return _sortedCache;
        }

        /// <summary>Random alive player excluding <paramref name="exclude"/>. Returns null if none.</summary>
        public PlayerData GetRandomAlive(PlayerData exclude, System.Random rng)
        {
            _aliveScratch.Clear();
            for (int i = 0; i < _all.Count; i++)
            {
                var p = _all[i];
                if (p.IsAlive && p != exclude)
                    _aliveScratch.Add(p);
            }
            if (_aliveScratch.Count == 0) return null;
            return _aliveScratch[rng.Next(_aliveScratch.Count)];
        }

        /// <summary>Random alive player. Returns null if none.</summary>
        public PlayerData GetRandomAlive(System.Random rng)
        {
            _aliveScratch.Clear();
            for (int i = 0; i < _all.Count; i++)
            {
                var p = _all[i];
                if (p.IsAlive)
                    _aliveScratch.Add(p);
            }
            if (_aliveScratch.Count == 0) return null;
            return _aliveScratch[rng.Next(_aliveScratch.Count)];
        }

        private sealed class ScoreComparer : System.Collections.Generic.IComparer<PlayerData>
        {
            public int Compare(PlayerData x, PlayerData y) => y.Score.CompareTo(x.Score);
        }
    }
}
