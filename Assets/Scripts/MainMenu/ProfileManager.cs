using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace MainMenu
{
    public class ProfileManager : MonoBehaviour
    {
        private const string UserNameKey = "UserName";
        private const string AvatarKey = "UserAvatar";
        public Action UserNameUpdated;
        public Action AvatarUpdated;
        public Action GuestLoginSuccess;

        public static PlayerItems LocalPlayerItems { get; private set; }
        public bool IsFirstLogin { get; private set; }

        void Start()
        {
            LocalPlayerItems = new PlayerItems();
            InitializeGuestLogin();
        }

        private void InitializeGuestLogin()
        {
            var request = new LoginWithCustomIDRequest
            {
                CustomId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true
            };

            PlayFabClientAPI.LoginWithCustomID(request, result =>
            {
                Debug.Log("PlayFab Login Success: " + result.PlayFabId);
                IsFirstLogin = result.NewlyCreated;
                UpdatePlayerItems();
                GuestLoginSuccess?.Invoke();
            }, error =>
            {
                Debug.LogError("PlayFab Login Failed: " + error.ErrorMessage);
            });
        }

        private void UpdateItemByKey(string key, string value)
        {
            var request = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> { { key, value } }
            };

            PlayFabClientAPI.UpdateUserData(request, result =>
            {
                Debug.Log("Updated Key: " + key);
            }, error =>
            {
                Debug.LogError("Failed to update key: " + key + " Error: " + error.ErrorMessage);
            });
        }

        private void UpdatePlayerItems()
        {
            PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
            {
                if (result.Data != null && result.Data.ContainsKey(UserNameKey))
                {
                    LocalPlayerItems.userName = result.Data[UserNameKey].Value;
                }
                if (result.Data != null && result.Data.ContainsKey(AvatarKey))
                {
                    LocalPlayerItems.selectedAvatar = result.Data[AvatarKey].Value;
                }
            }, error =>
            {
                Debug.LogError("Failed to get user data: " + error.ErrorMessage);
            });
        }

        public void UpdateUserName(string userName)
        {
            UpdateItemByKey(UserNameKey, userName);
            LocalPlayerItems.userName = userName;
            UserNameUpdated?.Invoke();
        }

        public void UpdateUserAvatar(string avatarId)
        {
            UpdateItemByKey(AvatarKey, avatarId);
            LocalPlayerItems.selectedAvatar = avatarId;
            AvatarUpdated?.Invoke();
        }

        [Serializable]
        public class PlayerItems
        {
            public string userName = "";
            public string selectedAvatar = "";
        }
    }
}
