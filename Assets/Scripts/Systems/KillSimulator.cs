using BattleBucks.Core;
using BattleBucks.Match;
using BattleBucks.Player;
using UnityEngine;

namespace BattleBucks.Systems
{
    // Drives simulated kills on a time schedule. Update() is a single float compare — no alloc.
    public sealed class KillSimulator : MonoBehaviour
    {
        private PlayerRegistry _registry;
        private MatchConfig    _config;
        private System.Random  _rng;

        private float _nextKillTime;
        private bool  _active;

        public void Initialise(PlayerRegistry registry, MatchConfig config)
        {
            _registry = registry;
            _config   = config;
            _rng      = new System.Random();

            GameEvents.OnMatchStarted += OnMatchStarted;
            GameEvents.OnMatchEnded   += OnMatchEnded;
        }

        private void OnDestroy()
        {
            GameEvents.OnMatchStarted -= OnMatchStarted;
            GameEvents.OnMatchEnded   -= OnMatchEnded;
        }

        private void OnMatchStarted()
        {
            _active = true;
            ScheduleNextKill();
        }

        private void OnMatchEnded(PlayerData _)
        {
            _active = false;
        }

        private void Update()
        {
            if (!_active || Time.time < _nextKillTime) return;
            SimulateKill();
            ScheduleNextKill();
        }

        private void SimulateKill()
        {
            PlayerData killer = _registry.GetRandomAlive(_rng);
            if (killer == null) return;

            PlayerData victim = _registry.GetRandomAlive(killer, _rng);
            if (victim == null) return;

            GameEvents.OnKill?.Invoke(new KillEventData(killer, victim));
        }

        private void ScheduleNextKill()
        {
            float interval = (float)(_rng.NextDouble() *
                             (_config.MaxKillInterval - _config.MinKillInterval) +
                             _config.MinKillInterval);
            _nextKillTime = Time.time + interval;
        }
    }
}
