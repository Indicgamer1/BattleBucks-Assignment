using BattleBucks.Core;
using TMPro;
using UnityEngine;

namespace BattleBucks.UI
{
    // Parented to lbPanel (not RowContainer) — SpawnRows destroys all RowContainer children.
    public sealed class ScorePopupUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI popupTemplate;
        [SerializeField] private LeaderboardUI   leaderboard;

        [Header("Pool")]
        [SerializeField] private int poolSize = 6;

        [Header("Animation")]
        [SerializeField] private float lifetime  = 0.85f;
        [SerializeField] private float riseSpeed = 90f;   // pixels per second
        [SerializeField] private float xOffset   = 130f;  // pixels right of row centre

        private TextMeshProUGUI[] _pool;      // pre-allocated — no Instantiate after Awake
        private float[]           _timeLeft;
        private Vector2[]         _pos;
        private int               _poolHead;

        private float _rowContainerOffsetY; // converts row-local Y → lbPanel-local Y

        private static readonly string PlusOne = "+1";

        private void Awake()
        {
            _pool     = new TextMeshProUGUI[poolSize];
            _timeLeft = new float[poolSize];
            _pos      = new Vector2[poolSize];

            RectTransform rowContainer = leaderboard != null ? leaderboard.RowContainer : null;
            Transform popupParent = rowContainer != null
                ? rowContainer.parent
                : popupTemplate.transform.parent;

            if (rowContainer != null)
                _rowContainerOffsetY = rowContainer.anchoredPosition.y;

            for (int i = 0; i < poolSize; i++)
            {
                var popup = Instantiate(popupTemplate, popupParent);
                popup.SetText(PlusOne);
                Color c = popup.color; c.a = 0f; popup.color = c; // start invisible
                _pool[i] = popup;
            }
        }

        private void OnEnable()  => GameEvents.OnKill += OnKill;
        private void OnDisable() => GameEvents.OnKill -= OnKill;

        private void OnKill(KillEventData data)
        {
            if (leaderboard == null) return;
            SpawnPopup(_rowContainerOffsetY + leaderboard.GetRowTargetY(data.Killer.Id));
        }

        private void SpawnPopup(float startY)
        {
            int idx = _poolHead % poolSize;
            _poolHead++;

            if (_pool[idx] == null) return;   // safety guard

            _timeLeft[idx] = lifetime;
            _pos[idx]      = new Vector2(xOffset, startY);

            _pool[idx].rectTransform.anchoredPosition = _pos[idx];

            Color c = _pool[idx].color; c.a = 1f; _pool[idx].color = c;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            for (int i = 0; i < poolSize; i++)
            {
                if (_timeLeft[i] <= 0f || _pool[i] == null) continue;

                _timeLeft[i] -= dt;

                if (_timeLeft[i] <= 0f)
                {
                    _timeLeft[i] = 0f;
                    Color c = _pool[i].color; c.a = 0f; _pool[i].color = c;
                    continue;
                }

                _pos[i].y += riseSpeed * dt;
                _pool[i].rectTransform.anchoredPosition = _pos[i];

                float alpha = _timeLeft[i] / lifetime; // fade out as lifetime runs out
                Color col   = _pool[i].color; col.a = alpha; _pool[i].color = col;
            }
        }
    }
}
