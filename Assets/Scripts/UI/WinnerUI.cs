using BattleBucks.Core;
using BattleBucks.Player;
using TMPro;
using UnityEngine;

namespace BattleBucks.UI
{
    public sealed class WinnerUI : MonoBehaviour
    {
        [SerializeField] private GameObject       panel;
        [SerializeField] private TextMeshProUGUI  winnerNameText;
        [SerializeField] private TextMeshProUGUI  winnerScoreText;
        [SerializeField] private TextMeshProUGUI  subTitleText;

        private void Awake()
        {
            UIVisibility.Hide(panel);
            GameEvents.OnMatchEnded += ShowWinner;
        }

        private void OnDestroy() => GameEvents.OnMatchEnded -= ShowWinner;

        private void ShowWinner(PlayerData winner)
        {
            UIVisibility.Show(panel);

            if (winner != null)
            {
                winnerNameText.SetText(winner.Name);
                winnerScoreText.SetText("{0} kills", winner.Score);
                subTitleText.SetText("WINNER");
            }
            else
            {
                winnerNameText.SetText("TIE!");
                winnerScoreText.SetText(string.Empty);
                subTitleText.SetText("TIME EXPIRED");
            }
        }
    }
}
