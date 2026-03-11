using TMPro;
using UnityEngine;

namespace BattleBucks.UI
{
    // Single leaderboard row — animates Y on rank change, flashes green/red.
    public sealed class LeaderboardRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI          rankText;
        [SerializeField] private TextMeshProUGUI          nameText;
        [SerializeField] private TextMeshProUGUI          scoreText;
        [SerializeField] private UnityEngine.UI.Image     rowBackground;

        [Header("Colors")]
        [SerializeField] private Color leaderColor   = new Color(1.00f, 0.75f, 0.10f, 0.30f); // gold tint
        [SerializeField] private Color defaultColor  = new Color(1.00f, 1.00f, 1.00f, 0.05f);
        [SerializeField] private Color rankUpColor   = new Color(0.20f, 0.90f, 0.30f, 0.55f); // green
        [SerializeField] private Color rankDownColor = new Color(0.90f, 0.20f, 0.20f, 0.55f); // red

        [Header("Animation")]
        [SerializeField] private float moveSpeed  = 10f;  // lerp speed for Y slide
        [SerializeField] private float flashDecay =  3f;  // how fast flash color fades

        private RectTransform _rt;
        private float _targetY;
        private bool  _isLeader;

        private Color _flashColor;
        private float _flashT;  // 0..1, lerped to 0 each frame

        private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder(4); // reused — no alloc

        /// <summary>Last rank assigned to this row (-1 = not yet assigned).</summary>
        public int CurrentRank { get; private set; } = -1;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
        }

        private void Update()
        {
            Vector2 pos = _rt.anchoredPosition;
            float diff = _targetY - pos.y;
            if (Mathf.Abs(diff) > 0.1f)
            {
                pos.y = Mathf.Lerp(pos.y, _targetY, Time.deltaTime * moveSpeed);
                _rt.anchoredPosition = pos;
            }
            else if (pos.y != _targetY)
            {
                pos.y = _targetY;
                _rt.anchoredPosition = pos;
            }

            if (_flashT > 0.001f && rowBackground != null) // fade flash back to base color
            {
                _flashT = Mathf.Lerp(_flashT, 0f, Time.deltaTime * flashDecay);
                rowBackground.color = Color.Lerp(_isLeader ? leaderColor : defaultColor,
                                                  _flashColor, _flashT);
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Move this row to the correct Y for <paramref name="rank"/> (0-based).</summary>
        public void AnimateTo(int rank, float rowStep)
        {
            CurrentRank = rank;
            _targetY    = -(rank * rowStep);
        }

        public void SnapTo(int rank, float rowStep)
        {
            CurrentRank = rank;
            _targetY    = -(rank * rowStep);
            Vector2 pos = _rt.anchoredPosition;
            pos.y = _targetY;
            _rt.anchoredPosition = pos;
        }

        public void FlashRankChange(bool movedUp)
        {
            _flashColor = movedUp ? rankUpColor : rankDownColor;
            _flashT     = 1f;
        }

        public void Refresh(int rank, string playerName, int score, bool isLeader)
        {
            _isLeader = isLeader;

            _sb.Clear();
            _sb.Append('#');
            AppendInt(_sb, rank);
            rankText.SetText(_sb);

            nameText.SetText(playerName);

            _sb.Clear();
            AppendInt(_sb, score);
            scoreText.SetText(_sb);

            // Only update base color if not mid-flash
            if (_flashT <= 0.001f && rowBackground != null)
                rowBackground.color = isLeader ? leaderColor : defaultColor;
        }

        // int → chars without string.Format or int.ToString() — zero alloc
        private static void AppendInt(System.Text.StringBuilder sb, int value)
        {
            if (value == 0) { sb.Append('0'); return; }
            if (value < 0)  { sb.Append('-'); value = -value; }

            int start = sb.Length;
            while (value > 0) { sb.Append((char)('0' + value % 10)); value /= 10; }

            int end = sb.Length - 1;
            while (start < end)
            {
                char tmp = sb[start]; sb[start++] = sb[end]; sb[end--] = tmp;
            }
        }
    }
}
