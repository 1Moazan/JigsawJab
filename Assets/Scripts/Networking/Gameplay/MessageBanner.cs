using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Networking.Gameplay
{
    public class MessageBanner : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI bannerText;
        [SerializeField] private GameObject bannerObject;
        [SerializeField] private RectTransform bannerTransform;

        public float startHeight;
        public float showDelay;
        public float moveDuration;

        public void Show(string message)
        {
            bannerTransform.DOKill();
            bannerTransform.anchoredPosition = new Vector2(0, startHeight);
            bannerObject.SetActive(true);
            bannerText.text = message;
            
            Sequence seq = DOTween.Sequence();

            seq.Append(bannerTransform.DOAnchorPosY(0f, moveDuration).SetEase(Ease.OutQuad));
            seq.Append(bannerTransform.DOAnchorPosY(-startHeight, moveDuration).SetEase(Ease.InQuad).SetDelay(showDelay));
            seq.Play();
        }
    }
}
