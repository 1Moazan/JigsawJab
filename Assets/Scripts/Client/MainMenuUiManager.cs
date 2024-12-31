using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class MainMenuUiManager : MonoBehaviour
    {
        [SerializeField] private TMP_InputField usernameInputField;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private TextMeshProUGUI avatarErrorText;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private Button saveUserNameButton;
        [SerializeField] private Button avatarClosebutton;
        [SerializeField] private GameObject userNamePanel;
        [SerializeField] private GameObject avatarSelectionPanel;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private RectTransform punchPanel;
        [SerializeField] private RectTransform avatarPunchPanel;
        [SerializeField] private RectTransform errorPunchPanel;
        [SerializeField] private AvatarPreview previewPrefab;
        [SerializeField] private Transform previewParent;


        [SerializeField] private ProfileManager profileManager;
        [SerializeField] private AvatarDataContainer dataContainer;

        private List<AvatarPreview> _previewObjects;
        private string _selectedAvatarKey;
        private Sequence _avatarSeq;

        private void Start()
        {
            loadingText.gameObject.SetActive(true);
            loadingText.transform.DOScale(new Vector3(1.1f, 1.1f), 1f).SetLoops(-1, LoopType.Yoyo);
            SetAvatars();
        }

        private void OnEnable()
        {
            saveUserNameButton.onClick.AddListener(SaveUserNameClicked);
            avatarClosebutton.onClick.AddListener(CloseAvatarClicked);
            profileManager.UserNameUpdated += UserNameUpdated;
            profileManager.AvatarUpdated += AvatarUpdated;
            profileManager.GuestLoginSuccess += GuestLoginSuccess;
        }

        private void OnDisable()
        {
            saveUserNameButton.onClick.RemoveListener(SaveUserNameClicked);
            avatarClosebutton.onClick.RemoveListener(CloseAvatarClicked);
            profileManager.UserNameUpdated -= UserNameUpdated;
            profileManager.GuestLoginSuccess -= GuestLoginSuccess;
        }
        
        private void UserNameUpdated()
        {
            SetPanel(userNamePanel, punchPanel);

            _avatarSeq?.Play();
            SetPanel(avatarSelectionPanel, avatarPunchPanel, true);
        }
        
        private void AvatarUpdated()
        {
            avatarClosebutton.interactable = true;
            SetPanel(avatarSelectionPanel, avatarPunchPanel);
        }
        
        private void GuestLoginSuccess()
        {
            loadingText.gameObject.SetActive(false);
            loadingText.transform.DOKill();
            if (profileManager.IsFirstLogin)
            {
                SetPanel(userNamePanel, punchPanel , true);
            }
        }

        private void SaveUserNameClicked()
        {
            if (string.IsNullOrEmpty(usernameInputField.text))
            {
                SetError("Username Can Not Be Empty.");
                return;
            }

            saveUserNameButton.interactable = false;
            profileManager.UpdateUserName(usernameInputField.text);
        }

        private void SetPanel(GameObject panel ,RectTransform childPanel, bool active = false)
        {
            mainPanel.SetActive(active);
            panel.SetActive(active);
            if (active) DefaultMove(childPanel);
            else childPanel.DOKill();
        }
        private void SetError(string message)
        {
            errorText.text = message;
            SetPanel(errorPanel, errorPunchPanel, true);
        }
        
        private void DefaultMove(RectTransform moveTransform)
        {
            moveTransform.anchoredPosition = new Vector2(-1500, moveTransform.anchoredPosition.y);
            moveTransform.DOAnchorPosX(0, 0.5f).SetEase(Ease.OutElastic);
        }

        private void SetAvatars()
        {
            _previewObjects ??= new List<AvatarPreview>();
            ClearContainer();
            _avatarSeq = DOTween.Sequence();
            foreach (AvatarData avatarData in dataContainer.avatarDataList)
            {
                AvatarPreview avatarPreview = Instantiate(previewPrefab, previewParent);

                _avatarSeq.Append(avatarPreview.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.1f).SetDelay(0.3f));
                avatarPreview.SetValues(avatarData, () =>
                {
                    _selectedAvatarKey = avatarPreview.AvatarKey;
                    avatarPreview.SetSelected(true);
                });
            }
        }

        private void CloseAvatarClicked()
        {
            if (!string.IsNullOrEmpty(_selectedAvatarKey))
            {
                avatarClosebutton.interactable = false;
                profileManager.UpdateUserAvatar(_selectedAvatarKey);
            }
            else
            {
                SetError("Please Select An Avatar.");
            }
        }

        private void ClearContainer()
        {
            if(_previewObjects == null || _previewObjects.Count < 1) return;
            foreach (AvatarPreview preview in _previewObjects)
            {
                Destroy(preview.gameObject);
            }
            _previewObjects.Clear();
        }
    }
}
