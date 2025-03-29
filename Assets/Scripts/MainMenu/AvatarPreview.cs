using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Client
{
    public class AvatarPreview : MonoBehaviour
    {
        [SerializeField] private GameObject childObject;
        [SerializeField] private GameObject selectedTick;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Button selectButton;

        public string AvatarKey { get; private set; }
        

        private void OnDisable()
        {
            selectedTick.transform.DOKill();
        }

        public void SetValues(AvatarData data, UnityAction selectEvent)
        {
            avatarImage.sprite = data.avatarSprite;
            AvatarKey = data.avatarKey;
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(selectEvent);
        }

        public void SetSelected(bool selected)
        {
            selectedTick.SetActive(selected);
            if (selected) selectedTick.transform.DOPunchScale(new Vector3(1f, 1f), 0.2f).SetEase(Ease.InOutQuad);
        }
        
        
    }
}
