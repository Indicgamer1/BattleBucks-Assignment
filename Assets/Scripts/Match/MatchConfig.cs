using UnityEngine;

namespace BattleBucks.Match
{
    /// <summary>
    /// ScriptableObject-driven match configuration.
    /// Satisfies the "Configurable match rules" extensibility requirement.
    /// Change values in the Inspector without recompiling.
    /// </summary>
    [CreateAssetMenu(fileName = "MatchConfig", menuName = "BattleBucks/Match Config")]
    public sealed class MatchConfig : ScriptableObject
    {
        [Header("Players")]
        [Tooltip("Total players that spawn at match start.")]
        [Range(2, 50)]
        public int PlayerCount = 10;

        [Header("Match Rules")]
        [Tooltip("Match ends when a player reaches this many kills.")]
        [Range(1, 50)]
        public int KillLimit = 10;

        [Tooltip("Match duration in seconds (0 = no time limit).")]
        [Min(0)]
        public float MatchDuration = 180f;

        [Header("Respawn")]
        [Tooltip("Seconds before a dead player respawns.")]
        [Min(0.1f)]
        public float RespawnDelay = 3f;

        [Header("Kill Simulation")]
        [Tooltip("Minimum seconds between simulated kills.")]
        [Min(0.1f)]
        public float MinKillInterval = 1f;

        [Tooltip("Maximum seconds between simulated kills.")]
        [Min(0.1f)]
        public float MaxKillInterval = 2f;
    }
}
