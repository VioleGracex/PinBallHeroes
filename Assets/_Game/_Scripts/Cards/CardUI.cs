using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class CardUI : MonoBehaviour
{
    public Image frontImage;
    public Image backImage;
    public Image effectIconImage;
    public Button button;

    public float flipDuration = 0.5f;

    private bool isFront = true;

    [Header("Card Info UI")]
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardDescriptionText;
    public TextMeshProUGUI costText;

    public void SetFront(Sprite sprite) { if (sprite != null) frontImage.sprite = sprite; }
    public void SetBack(Sprite sprite) { if (sprite != null) backImage.sprite = sprite; }
    public void SetEffectIcon(Sprite icon) { if (icon != null) effectIconImage.sprite = icon; }

    public void SetCardInfo(string name, string description, int cost)
    {
        if (cardNameText != null) cardNameText.text = name;
        if (cardDescriptionText != null) cardDescriptionText.text = description;
        if (costText != null) costText.text = (cost == 0) ? "Free" : cost.ToString();
    }

    public void FlipCard()
    {
        // Animate rotation Y 0 -> 90, swap, 90 -> 0
        float half = flipDuration / 2f;
        transform.DORotate(new Vector3(0, 90, 0), half)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                isFront = !isFront;
                frontImage.gameObject.SetActive(isFront);
                backImage.gameObject.SetActive(!isFront);
                transform.DORotate(new Vector3(0, 0, 0), half).SetEase(Ease.OutQuad);
            });
    }

    // Optionally, for instant flip without anim
    public void ShowFront(bool front)
    {
        isFront = front;
        frontImage.gameObject.SetActive(front);
        backImage.gameObject.SetActive(!front);
        transform.localRotation = Quaternion.identity;
    }
}