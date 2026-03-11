using BattleBucks.Core;
using BattleBucks.Player;
using UnityEngine;

namespace BattleBucks.UI
{
    /// <summary>
    /// Leaderboard panel.
    /// Each player owns a dedicated row indexed by player ID.
    /// Rows smoothly animate to their new Y position when rank changes.
    /// Requires no VerticalLayoutGroup — positioning is fully manual.
    /// </summary>
    public sealed class LeaderboardUI : MonoBehaviour
    {
        [Tooltip("Hidden template row — cloned at match start.")]
        [SerializeField] private LeaderboardRowUI rowTemplate;

        [Tooltip("Parent RectTransform that holds all rows.")]
        [SerializeField] private RectTransform rowContainer;

        [Tooltip("Vertical gap between rows in pixels.")]
        [SerializeField] private float rowSpacing = 4f;

        private PlayerRegistry    _registry;
        private LeaderboardRowUI[] _playerRows;   // indexed by player ID
        private float             _rowStep;        // rowHeight + rowSpacing
        private bool              _dirty;

        // ── Initialisation ────────────────────────────────────────────────

        public void Initialise(PlayerRegistry registry)
        {
            _registry = registry;
            GameEvents.OnMatchStarted += OnMatchStarted;
            GameEvents.OnScoreChanged += OnScoreChanged;
            GameEvents.OnMatchEnded   += OnMatchEnded;
        }

        private void OnDestroy()
        {
            GameEvents.OnMatchStarted -= OnMatchStarted;
            GameEvents.OnScoreChanged -= OnScoreChanged;
            GameEvents.OnMatchEnded   -= OnMatchEnded;
        }

        private void OnMatchEnded(PlayerData _) => enabled = false;
        private void OnScoreChanged(PlayerData _) => _dirty = true;

        private void OnMatchStarted()
        {
            SpawnRows(_registry.Count);
            enabled = true;
            _dirty  = true;
        }

        // ── Row instantiation ─────────────────────────────────────────────

        private void SpawnRows(int count)
        {
            if (rowTemplate == null) { Debug.LogError("[LeaderboardUI] rowTemplate is not assigned."); return; }
            if (rowContainer == null) { Debug.LogError("[LeaderboardUI] rowContainer is not assigned."); return; }

            // Disable VerticalLayoutGroup — we handle positioning manually
            var vlg = rowContainer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (vlg != null) vlg.enabled = false;

            // Row height from template
            float rowHeight = rowTemplate.GetComponent<RectTransform>().sizeDelta.y;
            _rowStep = rowHeight + rowSpacing;

            // Destroy old rows
            for (int i = rowContainer.childCount - 1; i >= 0; i--)
                Destroy(rowContainer.GetChild(i).gameObject);

            _playerRows = new LeaderboardRowUI[count];

            for (int i = 0; i < count; i++)
            {
                LeaderboardRowUI row = Instantiate(rowTemplate, rowContainer);
                UIVisibility.Show(row.gameObject);

                // Set anchor to top-stretch so manual Y works correctly
                RectTransform rt = row.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0f, 1f);
                rt.anchorMax        = new Vector2(1f, 1f);
                rt.pivot            = new Vector2(0.5f, 1f);
                rt.sizeDelta        = new Vector2(0f, rowHeight);

                // Snap to initial position (no animation on spawn)
                row.SnapTo(i, _rowStep);

                // _playerRows is indexed by player ID.
                // At match start all scores are 0; initial sorted order = ID order.
                _playerRows[i] = row;
            }
        }

        // ── Dirty-flag rebuild ────────────────────────────────────────────

        private void LateUpdate()
        {
            if (!_dirty) return;
            _dirty = false;
            RebuildLeaderboard();
        }

        private void RebuildLeaderboard()
        {
            if (_playerRows == null) return;

            int count;
            PlayerData[] sorted = _registry.GetSortedSnapshot(out count);

            for (int rank = 0; rank < count; rank++)
            {
                PlayerData p = sorted[rank];
                if (p.Id < 0 || p.Id >= _playerRows.Length || _playerRows[p.Id] == null)
                    continue;

                LeaderboardRowUI row     = _playerRows[p.Id];
                int              prevRank = row.CurrentRank;

                row.AnimateTo(rank, _rowStep);
                row.Refresh(rank + 1, p.Name, p.Score, rank == 0);

                if (prevRank >= 0 && prevRank != rank)
                    row.FlashRankChange(rank < prevRank);  // true = moved up
            }
        }

        // ── Public helpers ────────────────────────────────────────────────

        /// <summary>
        /// Returns the target anchoredPosition.y for a player's row.
        /// Used by ScorePopupUI to position floating "+1" labels.
        /// </summary>
        public float GetRowTargetY(int playerId)
        {
            if (_playerRows == null || playerId < 0 || playerId >= _playerRows.Length
                || _playerRows[playerId] == null)
                return 0f;

            return -(_playerRows[playerId].CurrentRank * _rowStep);
        }

        /// <summary>Exposes the row container transform for child popup placement.</summary>
        public RectTransform RowContainer => rowContainer;
    }
}
