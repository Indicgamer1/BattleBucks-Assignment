using System;
using BattleBucks.Core;
using BattleBucks.Player;
using BattleBucks.Systems;
using UnityEngine;

namespace BattleBucks.Match
{
    // Pure C# match orchestrator — owns TimerSystem and ScoreSystem, handles respawns.
    public sealed class MatchController
    {
        public bool IsMatchActive { get; private set; }

        private readonly TimerSystem    _timer;
        private readonly ScoreSystem    _scoreSystem;
        private readonly PlayerRegistry _registry;
        private readonly MatchConfig    _config;

        private readonly PlayerData[]   _respawnQueue; // pre-allocated — no per-kill alloc
        private int                     _respawnCount;
        private readonly System.Random  _rng;

        public MatchController(PlayerRegistry registry, MatchConfig config)
        {
            _registry     = registry;
            _config       = config;
            _timer        = new TimerSystem();
            _scoreSystem  = new ScoreSystem();
            _respawnQueue = new PlayerData[config.PlayerCount];
            _rng          = new System.Random();

            WireInternalEvents();
        }

        private void WireInternalEvents()
        {
            _scoreSystem.OnScoreChanged     += p => GameEvents.OnScoreChanged?.Invoke(p);
            _scoreSystem.OnKillLimitReached += EndMatch;
            _timer.OnTimerTick    += t => GameEvents.OnTimerTick?.Invoke(t);
            _timer.OnTimerExpired += OnTimerExpired;
            GameEvents.OnKill     += HandleKill;
        }

        // ─── Public API ──────────────────────────────────────────────────

        public void StartMatch()
        {
            if (IsMatchActive) return;

            IsMatchActive  = true;
            _respawnCount  = 0;

            _registry.Initialise(_config.PlayerCount);
            _scoreSystem.Initialise(_config.KillLimit);
            _timer.Initialise(_config.MatchDuration);
            _timer.Start();

            GameEvents.OnMatchStarted?.Invoke();
        }

        /// <summary>Called every frame by MatchBootstrapper.Update().</summary>
        public void Tick(float deltaTime, float currentTime)
        {
            if (!IsMatchActive) return;

            _timer.Tick(deltaTime);
            ProcessRespawnQueue(currentTime);
        }

        // ─── Kill handling ───────────────────────────────────────────────

        private void HandleKill(KillEventData data)
        {
            if (!IsMatchActive) return;

            // Award score — may trigger OnKillLimitReached → EndMatch
            _scoreSystem.RegisterKill(data.Killer);

            // Schedule victim respawn
            data.Victim.IsAlive   = false;
            data.Victim.RespawnAt = GetCurrentTime() + _config.RespawnDelay;

            if (_respawnCount < _respawnQueue.Length)
                _respawnQueue[_respawnCount++] = data.Victim;
        }

        private void ProcessRespawnQueue(float currentTime)
        {
            for (int i = _respawnCount - 1; i >= 0; i--)
            {
                PlayerData p = _respawnQueue[i];
                if (currentTime >= p.RespawnAt)
                {
                    p.IsAlive   = true;
                    p.RespawnAt = 0f;
                    _respawnQueue[i] = _respawnQueue[--_respawnCount]; // swap-with-last O(1) remove
                    GameEvents.OnPlayerRespawned?.Invoke(p);
                }
            }
        }

        private void OnTimerExpired()
        {
            if (!IsMatchActive) return;
            EndMatch(GetLeader()); // null leader = tie
        }

        private void EndMatch(PlayerData winner)
        {
            if (!IsMatchActive) return;
            IsMatchActive = false;
            _timer.Stop();
            GameEvents.OnKill -= HandleKill;
            GameEvents.OnMatchEnded?.Invoke(winner);
        }

        private PlayerData GetLeader()
        {
            int count;
            PlayerData[] sorted = _registry.GetSortedSnapshot(out count);
            return count > 0 ? sorted[0] : null;
        }

        // Seam for unit testing — inject a fake clock via SetTimeSource().
        private Func<float> _timeFn;
        private float GetCurrentTime() => _timeFn != null ? _timeFn() : UnityEngine.Time.time;
        public void SetTimeSource(Func<float> fn) => _timeFn = fn;
    }
}
