using BattleBucks.Player;
using BattleBucks.Systems;
using BattleBucks.UI;
using UnityEngine;

namespace BattleBucks.Match
{
    // Single MonoBehaviour entry point. Wires systems and drives the match tick.
    // -100 ensures Awake runs before all other scripts so GameEvents.Clear fires first.
    [DefaultExecutionOrder(-100)]
    public sealed class MatchBootstrapper : MonoBehaviour
    {
        [SerializeField] private MatchConfig   config;
        [SerializeField] private KillSimulator killSimulator;
        [SerializeField] private UIManager     uiManager;

        private MatchController _matchController;
        private PlayerRegistry  _playerRegistry;

        private void Awake()
        {
            // Clear stale subscriptions from previous sessions
            Core.GameEvents.Clear();

            _playerRegistry  = new PlayerRegistry(config.PlayerCount);
            _matchController = new MatchController(_playerRegistry, config);

            killSimulator.Initialise(_playerRegistry, config);
            uiManager.Initialise(_playerRegistry);
        }

        private void Start()
        {
            _matchController.StartMatch();
        }

        private void Update()
        {
            _matchController.Tick(Time.deltaTime, Time.time);
        }

        private void OnDestroy()
        {
            Core.GameEvents.Clear();
        }
    }
}
