using TMPro;
using UnityEngine;

public class TurnIndicatorUI : MonoBehaviour
{
    [Header("Assign a TextMeshProUGUI component")]
    public TextMeshProUGUI turnText;

    public void SetPlayerTurn()
    {
        if (turnText != null)
        {
            turnText.text = "Player Turn";
            turnText.color = Color.blue;
        }
    }

    public void SetEnemyTurn()
    {
        if (turnText != null)
        {
            turnText.text = "Enemy Turn";
            turnText.color = Color.red;
        }
    }

    public void Hide()
    {
        if (turnText != null)
            turnText.text = "";
    }
}
