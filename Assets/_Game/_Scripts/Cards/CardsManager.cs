using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

using System;

public class CardsManager : MonoBehaviour
{
#region Fields
    [Header("Card Window UI")]
    public GameObject cardWindow; // Assign in inspector
    private Player player;

    private List<CardData> freeCards = new List<CardData>();
    private List<CardData> nonFreeCards = new List<CardData>();


#endregion

#region Unity Methods
    private void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<Player>();

        UpdateCardTypeLists();
    }
#endregion


#region Currency
    public void SpendPinballs(int amount)
    {
        CurrencyManager.Instance.SpendPinballs(amount);
    }
#endregion


    public GameObject cardPrefab;
    public Transform cardParent; // Parent with HorizontalLayoutGroup
    public List<CardData> cardPool; // Assign possible cards in inspector

    private List<CardUI> spawnedCards = new List<CardUI>();
    private int selectedCardIndex = -1;

    public event Action OnCardSelectionEnded;


#region Card Spawning
    public void SpawnCards()
    {
        selectedCardIndex = -1;
        if (cardWindow != null)
        {
            cardWindow.SetActive(true);
            // Animate card window scale from 0.7 to 1 with easeOutBack
            cardWindow.transform.localScale = Vector3.one * 0.7f;
            cardWindow.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }
        List<CardData> chosen = new List<CardData>();
        System.Random rng = new System.Random();
        // Shuffle freeCards
        for (int i = freeCards.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var temp = freeCards[i];
            freeCards[i] = freeCards[j];
            freeCards[j] = temp;
        }
        // Shuffle nonFreeCards
        for (int i = nonFreeCards.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var temp = nonFreeCards[i];
            nonFreeCards[i] = nonFreeCards[j];
            nonFreeCards[j] = temp;
        }
        // Add 2 free cards (or as many as available)
        int freeToAdd = Mathf.Min(2, freeCards.Count);
        for (int i = 0; i < freeToAdd; i++) chosen.Add(freeCards[i]);
        // Add 1 non-free card (or fill with free if not enough non-free)
        if (nonFreeCards.Count > 0)
            chosen.Add(nonFreeCards[0]);
        else if (freeCards.Count > 2)
            chosen.Add(freeCards[2]);
        // If still less than 3, fill with random from cardPool
        while (chosen.Count < 3)
        {
            chosen.Add(cardPool[UnityEngine.Random.Range(0, cardPool.Count)]);
        }
        // Shuffle chosen cards for random order
        for (int i = chosen.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            var temp = chosen[i];
            chosen[i] = chosen[j];
            chosen[j] = temp;
        }

        for (int i = 0; i < 3; i++)
        {
            CardUI card;
            if (i < spawnedCards.Count && spawnedCards[i] != null)
            {
                card = spawnedCards[i];
                card.transform.SetSiblingIndex(i);
                card.gameObject.SetActive(true);
                DOTween.Kill(card.transform); // Kill any running tweens
            }
            else
            {
                card = Instantiate(cardPrefab, cardParent).GetComponent<CardUI>();
                spawnedCards.Add(card);
            }
            var data = chosen[i];
            if (data.frontSprite != null)
                card.SetFront(data.frontSprite);
            if (data.backSprite != null)
                card.SetBack(data.backSprite);
            if (data.effectIcon != null)
                card.SetEffectIcon(data.effectIcon);
            card.ShowFront(false); // start face-down
            card.SetCardInfo(data.name, data.description, data.costInPinballs);
            float delay = 0.5f + 0.3f * i;
            DOVirtual.DelayedCall(delay, card.FlipCard);

            // Remove previous listeners
            card.button?.onClick.RemoveAllListeners();
            int cardIdx = i;
            card.button?.onClick.AddListener(() => OnCardClicked(cardIdx, data));

            // Disable button if not enough pinballs
            if (card.button != null)
            {
                bool canAfford = data.costInPinballs == 0 || (CurrencyManager.Instance != null && CurrencyManager.Instance.Pinballs >= data.costInPinballs);
                card.button.interactable = canAfford;
            }
        }
        // Hide any extra cards
        for (int i = 3; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
                spawnedCards[i].gameObject.SetActive(false);
        }
    }
#endregion


#region Card Pool Helpers
    // Call this to update free/non-free lists if cardPool changes
    private void UpdateCardTypeLists()
    {
        freeCards.Clear();
        nonFreeCards.Clear();
        foreach (var c in cardPool)
        {
            if (c.costInPinballs == 0)
                freeCards.Add(c);
            else
                nonFreeCards.Add(c);
        }
    }
#endregion


#region Card Selection
    private void OnCardClicked(int cardIdx, CardData data)
    {
        if (selectedCardIndex != -1) return; // Only allow one selection
        selectedCardIndex = cardIdx;
        ApplyCardEffect(data);
        // Optionally: visually highlight the selected card, disable others, etc.
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            var btn = spawnedCards[i].GetComponent<UnityEngine.UI.Button>();
            if (i != cardIdx && btn != null)
                btn.interactable = false;
        }
        Debug.Log($"Card {cardIdx} selected: {data.cardName}");
        // Notify listeners that card selection has ended
        OnCardSelectionEnded?.Invoke();
        // Hide card window after selection
        if (cardWindow != null)
            cardWindow.SetActive(false);
    }
#endregion


#region Card Effects
    public void ApplyCardEffect(CardData card)
    {
        if (player == null || card == null) return;
        int amount = card.amount;
        switch (card.effectType)
        {
            case CardData.EffectType.FlatIncrease:
                AddPlayerStat(card.statAffected, amount);
                break;
            case CardData.EffectType.Multiplier:
                MultiplyPlayerStat(card.statAffected, amount);
                break;
            case CardData.EffectType.Percentage:
                AddPlayerStat(card.statAffected, Mathf.RoundToInt(GetPlayerStat(card.statAffected) * (amount / 100f)));
                break;
        }
    }

#region Stat Helpers
    private void AddPlayerStat(PlayerStats.StatType stat, int amount)
    {
        switch (stat)
        {
            case PlayerStats.StatType.HP:
                player.MaxHP += amount;
                player.CurrentHP += amount;
                player.UpdateHealthBar();
                break;
            case PlayerStats.StatType.Attack:
                player.AttackDamage += amount;
                break;
            case PlayerStats.StatType.AttackSpeed:
                player.AttackSpeed += amount;
                break;
            case PlayerStats.StatType.Defense:
                player.Armor += amount;
                break;
            case PlayerStats.StatType.CritChance:
                // Add crit chance logic if present
                break;
            case PlayerStats.StatType.CurrencyDropChance:
                player.currencyDropChance += amount / 100f;
                break;
        }
    }

    private void MultiplyPlayerStat(PlayerStats.StatType stat, int multiplier)
    {
        switch (stat)
        {
            case PlayerStats.StatType.HP:
                player.MaxHP *= multiplier;
                player.CurrentHP *= multiplier;
                break;
            case PlayerStats.StatType.Attack:
                player.AttackDamage *= multiplier;
                break;
            case PlayerStats.StatType.AttackSpeed:
                player.AttackSpeed *= multiplier;
                break;
            case PlayerStats.StatType.Defense:
                player.Armor *= multiplier;
                break;
            case PlayerStats.StatType.CritChance:
                // Add crit chance logic if present
                break;
            case PlayerStats.StatType.CurrencyDropChance:
                player.currencyDropChance *= multiplier;
                break;
        }
    }

    private float GetPlayerStat(PlayerStats.StatType stat)
    {
        switch (stat)
        {
            case PlayerStats.StatType.HP:
                return player.MaxHP;
            case PlayerStats.StatType.Attack:
                return player.AttackDamage;
            case PlayerStats.StatType.AttackSpeed:
                return player.AttackSpeed;
            case PlayerStats.StatType.Defense:
                return player.Armor;
            case PlayerStats.StatType.CritChance:
                // Add crit chance logic if present
                return 0f;
            case PlayerStats.StatType.CurrencyDropChance:
                return player.currencyDropChance;
            default:
                return 0f;
        }
    }
#endregion
#endregion
}
