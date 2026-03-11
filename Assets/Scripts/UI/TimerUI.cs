using BattleBucks.Core;
using TMPro;
using UnityEngine;

namespace BattleBucks.UI
{
    // Updates once per second via OnTimerTick — no Update() polling.
    public sealed class TimerUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Color warningColor = new Color(1f, 0.60f, 0f, 1f);
        [SerializeField] private Color normalColor  = new Color(0.91f, 0.22f, 0.16f, 1f);
        [SerializeField] private float warningThreshold = 30f;

        private readonly char[] _timeBuffer = new char[5]; // "MM:SS" — reused, no string alloc

        private void OnEnable()
        {
            GameEvents.OnTimerTick  += UpdateDisplay;
            GameEvents.OnMatchEnded += OnMatchEnded;
        }

        private void OnDisable()
        {
            GameEvents.OnTimerTick  -= UpdateDisplay;
            GameEvents.OnMatchEnded -= OnMatchEnded;
        }

        private void OnMatchEnded(BattleBucks.Player.PlayerData _) => enabled = false;

        private void UpdateDisplay(float seconds)
        {
            int total   = Mathf.CeilToInt(seconds);
            int minutes = total / 60;
            int secs    = total % 60;

            _timeBuffer[0] = (char)('0' + minutes / 10);
            _timeBuffer[1] = (char)('0' + minutes % 10);
            _timeBuffer[2] = ':';
            _timeBuffer[3] = (char)('0' + secs / 10);
            _timeBuffer[4] = (char)('0' + secs % 10);

            timerText.SetCharArray(_timeBuffer);
            timerText.color = seconds <= warningThreshold ? warningColor : normalColor;
        }
    }
}
