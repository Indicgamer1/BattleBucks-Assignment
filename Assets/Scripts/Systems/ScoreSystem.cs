using System;
using BattleBucks.Player;

namespace BattleBucks.Systems
{
    /// <summary>
    /// Pure C# score tracker. Decoupled from any MonoBehaviour.
    /// Mutates PlayerData and raises events consumed by UI and MatchController.
    /// </summary>
    public sealed class ScoreSystem
    {
        private int _killLimit;

        // Fired when any player's score changes
        public Action<PlayerData> OnScoreChanged;
        // Fired when a player hits the kill limit
        public Action<PlayerData> OnKillLimitReached;

        public void Initialise(int killLimit)
        {
            _killLimit = killLimit;
        }

        /// <summary>
        /// Awards +1 kill to <paramref name="killer"/> and fires events.
        /// </summary>
        public void RegisterKill(PlayerData killer)
        {
            killer.Score++;
            OnScoreChanged?.Invoke(killer);

            if (killer.Score >= _killLimit)
                OnKillLimitReached?.Invoke(killer);
        }
    }
}
