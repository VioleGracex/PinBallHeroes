using UnityEngine;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Cards/CardData")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Sprite frontSprite;
    public Sprite backSprite;
    public Sprite effectIcon;
    public int costInPinballs;
    public PlayerStats.StatType statAffected;
    public EffectType effectType;
    public int amount;
    public string description;
    [HideInInspector]
    public string id = System.Guid.NewGuid().ToString();

    public enum EffectType
    {
        FlatIncrease,
        Multiplier,
        Percentage
    }

#if UNITY_EDITOR
    [Button("Set Asset Name")]
    private void SetAssetName()
    {
        string newName = !string.IsNullOrEmpty(cardName) ? cardName : $"Card_{id}";
        string assetPath = AssetDatabase.GetAssetPath(this);
        AssetDatabase.RenameAsset(assetPath, newName);
        AssetDatabase.SaveAssets();
        Debug.Log($"CardData asset renamed to: {newName}");
    }
#endif
}