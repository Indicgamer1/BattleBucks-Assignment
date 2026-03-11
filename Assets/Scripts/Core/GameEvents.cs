using System;
using BattleBucks.Player;

namespace BattleBucks.Core
{
    // Central event bus — all systems talk through here, never directly to each other.
    public static class GameEvents
    {
        public static Action OnMatchStarted;
        public static Action<PlayerData> OnMatchEnded;   // null = time-expired (no single winner)
        public static Action<KillEventData> OnKill;
        public static Action<PlayerData> OnPlayerRespawned;
        public static Action<PlayerData> OnScoreChanged;
        public static Action<float> OnTimerTick;         // fires once per second, not every frame

        // Call before each Play session to prevent stale subscriptions carrying over.
        public static void Clear()
        {
            OnMatchStarted      = null;
            OnMatchEnded        = null;
            OnKill              = null;
            OnPlayerRespawned   = null;
            OnScoreChanged      = null;
            OnTimerTick         = null;
        }
    }

    // Struct — no heap allocation when fired through events.
    public readonly struct KillEventData
    {
        public readonly PlayerData Killer;
        public readonly PlayerData Victim;

        public KillEventData(PlayerData killer, PlayerData victim)
        {
            Killer = killer;
            Victim = victim;
        }
    }
}
