using System;

namespace BattleBucks.Systems
{
    /// <summary>
    /// Pure C# timer. Zero MonoBehaviour dependency.
    /// Fires OnTimerTick once per second (not per frame) to avoid per-frame allocations.
    /// MatchController drives this via Tick().
    /// </summary>
    public sealed class TimerSystem
    {
        public float TimeRemaining { get; private set; }
        public bool  IsRunning     { get; private set; }

        // Fired once per elapsed second — subscribers update UI here
        public Action<float> OnTimerTick;
        public Action        OnTimerExpired;

        private float _duration;
        private float _secondAccumulator;

        public void Initialise(float duration)
        {
            _duration      = duration;
            TimeRemaining  = duration;
            IsRunning      = false;
            _secondAccumulator = 0f;
        }

        public void Start()
        {
            IsRunning = true;
            // Fire immediately so UI shows correct time on first frame
            OnTimerTick?.Invoke(TimeRemaining);
        }

        public void Stop() => IsRunning = false;

        /// <summary>Called by MatchController each Update.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsRunning || TimeRemaining <= 0f) return;

            TimeRemaining      -= deltaTime;
            _secondAccumulator += deltaTime;

            // Fire once per elapsed second — not every frame
            if (_secondAccumulator >= 1f)
            {
                _secondAccumulator -= 1f;
                float clamped = TimeRemaining < 0f ? 0f : TimeRemaining;
                OnTimerTick?.Invoke(clamped);
            }

            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                IsRunning     = false;
                OnTimerExpired?.Invoke();
            }
        }
    }
}
