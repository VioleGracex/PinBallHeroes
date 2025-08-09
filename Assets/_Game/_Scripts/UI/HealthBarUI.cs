using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class HealthBarUI : MonoBehaviour
{
    [Header("Assign a Slider for the HP bar")]
    public Image fillImage;
    [Header("Assign a TextMeshProUGUI for the HP value")]
    public TextMeshProUGUI hpText;
    [Header("Optional: Assign a pooled floating text object (TextMeshProUGUI)")]
    public TextMeshProUGUI pooledFloatingText;

    private Vector2 pooledTextInitialPos;
    private bool initialPosSet = false;

    public void SetHP(int current, int max)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = (float)current / max;
        }
        if (hpText != null)
        {
            hpText.text = $"{current}";
        }
    }

    public void ShowDamage(int amount)
    {
        if (pooledFloatingText != null)
        {
            if (!initialPosSet)
            {
                pooledTextInitialPos = pooledFloatingText.rectTransform.anchoredPosition;
                initialPosSet = true;
            }
            pooledFloatingText.gameObject.SetActive(false); // Reset
            pooledFloatingText.transform.DOKill(); // Kill any running tweens
            pooledFloatingText.text = $"-{amount}";
            pooledFloatingText.color = Color.red;
            pooledFloatingText.alpha = 1f;
            pooledFloatingText.rectTransform.anchoredPosition = pooledTextInitialPos;
            pooledFloatingText.gameObject.SetActive(true);
            // Animate: move up and fade out
            pooledFloatingText.rectTransform.DOAnchorPosY(3f, 1.5f).SetRelative(true);
            pooledFloatingText.DOFade(0f, 1.5f).SetDelay(0.2f).OnComplete(() =>
            {
                pooledFloatingText.gameObject.SetActive(false);
            });
        }
    }
}
