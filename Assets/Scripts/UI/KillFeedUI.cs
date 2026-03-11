using BattleBucks.Core;
using TMPro;
using UnityEngine;

namespace BattleBucks.UI
{
    // Ring-buffer kill feed. Pre-allocated — no heap alloc in hot paths.
    public sealed class KillFeedUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI[] feedLines;
        [SerializeField] private float             lineDuration = 4f;
        [SerializeField] private float             slideSpeed   = 12f;
        [SerializeField] private float             slideStartX  = 320f;

        private string[] _messages; // ring buffer
        private float[]  _expiry;
        private int      _head;
        private int      _capacity;

        private float[] _slideX;  // slide-in X per display slot
        private bool    _newKill; // true when a fresh kill just arrived — drives slide-in

        private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder(64);

        private void Awake()
        {
            _capacity = feedLines.Length;
            _messages = new string[_capacity];
            _expiry   = new float[_capacity];
            _slideX   = new float[_capacity];
        }

        private void OnEnable()  => GameEvents.OnKill += OnKill;
        private void OnDisable() => GameEvents.OnKill -= OnKill;

        private void OnKill(KillEventData data)
        {
            _sb.Clear();
            _sb.Append(data.Killer.Name);
            _sb.Append("<color=#ffffff88>  killed  </color>");
            _sb.Append("<color=#E8372A>");
            _sb.Append(data.Victim.Name);
            _sb.Append("</color>");

            _messages[_head] = _sb.ToString(); // one alloc per kill at ~1-2 Hz — acceptable
            _expiry[_head]   = Time.time + lineDuration;
            _head            = (_head + 1) % _capacity;

            _newKill = true;
            RefreshDisplay();
        }

        private void Update()
        {
            bool anyExpired = false;
            float now = Time.time;

            for (int i = 0; i < _capacity; i++)
            {
                if (_messages[i] != null && now >= _expiry[i])
                {
                    _messages[i] = null;
                    anyExpired   = true;
                }
            }

            if (anyExpired) RefreshDisplay();

            for (int i = 0; i < feedLines.Length; i++) // slide X toward 0
            {
                if (_slideX[i] > 0.5f)
                {
                    _slideX[i] = Mathf.Lerp(_slideX[i], 0f, Time.deltaTime * slideSpeed);
                    var pos = feedLines[i].rectTransform.anchoredPosition;
                    pos.x = _slideX[i];
                    feedLines[i].rectTransform.anchoredPosition = pos;
                }
                else if (_slideX[i] != 0f)
                {
                    _slideX[i] = 0f;
                    var pos = feedLines[i].rectTransform.anchoredPosition;
                    pos.x = 0f;
                    feedLines[i].rectTransform.anchoredPosition = pos;
                }
            }
        }

        private void RefreshDisplay()
        {
            bool triggerSlide = _newKill;
            _newKill = false;

            float now = Time.time;
            int lineIndex = 0;

            for (int i = 0; i < _capacity && lineIndex < feedLines.Length; i++)
            {
                int idx = (_head - 1 - i + _capacity) % _capacity; // newest → oldest
                if (_messages[idx] == null) continue;

                feedLines[lineIndex].SetText(_messages[idx]);

                float timeLeft = _expiry[idx] - now;
                float alpha    = timeLeft < 1f ? timeLeft : 1f; // fade in final second
                Color c        = feedLines[lineIndex].color;
                c.a            = alpha;
                feedLines[lineIndex].color = c;

                UIVisibility.Show(feedLines[lineIndex].gameObject);

                if (lineIndex == 0 && triggerSlide)
                    _slideX[0] = slideStartX; // only newest entry slides in

                lineIndex++;
            }

            for (int i = lineIndex; i < feedLines.Length; i++)
            {
                UIVisibility.Hide(feedLines[i].gameObject);
                _slideX[i] = 0f;
            }
        }
    }
}
