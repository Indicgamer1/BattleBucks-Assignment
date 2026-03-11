using BattleBucks.Player;
using UnityEngine;

namespace BattleBucks.UI
{
    /// <summary>
    /// Wires UI components to the PlayerRegistry.
    /// All references are set in the Inspector — no FindObjectOfType calls.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private LeaderboardUI leaderboard;

        public void Initialise(PlayerRegistry registry)
        {
            leaderboard.Initialise(registry);
        }
    }
}
