using UnityEngine;
using TMPro;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }
    public int Pinballs { get; private set; } = 0;

    [Header("UI")]
    public TextMeshProUGUI pinballText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        UpdatePinballText();
    }

    public void AddPinballs(int amount)
    {
        Pinballs += amount;
        Debug.Log($"[CurrencyManager] Pinballs increased by {amount}. Total: {Pinballs}");
        UpdatePinballText();
    }

    public bool SpendPinballs(int amount)
    {
        if (Pinballs >= amount)
        {
            Pinballs -= amount;
            Debug.Log($"[CurrencyManager] Pinballs spent: {amount}. Remaining: {Pinballs}");
            UpdatePinballText();
            return true;
        }
        Debug.LogWarning($"[CurrencyManager] Not enough pinballs to spend: {amount}. Current: {Pinballs}");
        return false;
    }

    private void Update()
    {
        // Optional: keep UI in sync if value changes elsewhere
        UpdatePinballText();
    }

    private void UpdatePinballText()
    {
        if (pinballText != null)
            pinballText.text = $"{Pinballs}";
    }
}
