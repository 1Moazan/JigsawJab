using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentity.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using UnityEngine;
using Credentials = Amazon.SecurityToken.Model.Credentials;

namespace Client
{
    public class ProfileManager : MonoBehaviour
    {
        private const string IdentityPoolId = "us-east-1:3c9345bb-60de-4433-88c3-0384b359168a";
        private const string TableName = "arn:aws:dynamodb:us-east-1:339713110717:table/UserInfo";
        private const string UserIdKey = "UserId";
        private const string UserNameKey = "UserName";
        private const string AvatarKey = "UserAvatar";
        private const string SaveIdKey = "PrefsIdKey";
        private AmazonDynamoDBClient _client;
        private string _userId;
        private bool _userIdSavedOrRetrieved;

        public Action UserNameUpdated;
        public Action AvatarUpdated;
        public Action GuestLoginSuccess;

        public static PlayerItems LocalPlayerItems { get; private set; }
        public bool IsFirstLogin { get; private set; }

        async void Start()
        {
            LocalPlayerItems = new PlayerItems();
            await InitializeGuestLogin();
        }

        private async Task InitializeGuestLogin()
        {
            AWSCredentials credentials = new AnonymousAWSCredentials();
            AmazonCognitoIdentityClient identityClient =
                new AmazonCognitoIdentityClient(credentials, RegionEndpoint.USEast1);

            string identityId = "";
            if (string.IsNullOrEmpty(PlayerPrefs.GetString(SaveIdKey, "")))
            {
                var client = await identityClient.GetIdAsync(new GetIdRequest
                {
                    IdentityPoolId = IdentityPoolId
                });
                identityId = client.IdentityId;
                Debug.Log("New Id Found : " + identityId);
                PlayerPrefs.SetString(SaveIdKey, identityId);
            }
            else
            {
                identityId = PlayerPrefs.GetString(SaveIdKey);
                Debug.Log("Id Exists : " + identityId);
            }
            
            var identityCred = await identityClient.GetCredentialsForIdentityAsync(new GetCredentialsForIdentityRequest
            {
                IdentityId = identityId
            });

            if (identityCred == null || identityCred.HttpStatusCode != HttpStatusCode.OK)
            {
                Debug.Log("Failed to get identity credentials");
                return;
            }
            
            Debug.Log("Credentials found for : " + identityCred.IdentityId);
            _client = new AmazonDynamoDBClient(identityCred.Credentials, RegionEndpoint.USEast1);
            _userId = identityId;
            await GetOrSaveUserId();
        }

        private async Task GetOrSaveUserId()
        {
            if (_userIdSavedOrRetrieved)
            {
                return;
            }

            var existResult = await CheckKeyExists(UserIdKey, _userId);

            if (existResult.exists)
            {
                _userId = existResult.items[UserIdKey].S;
                _userIdSavedOrRetrieved = true;
                Debug.Log($"User ID: {_userId} already exists.");
                await UpdatePlayerItems();
                GuestLoginSuccess?.Invoke();
                IsFirstLogin = false;
                return;
            }
            try
            {
                var request = new PutItemRequest
                {
                    TableName = TableName,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { UserIdKey, new AttributeValue { S = _userId } }
                    }
                };
                await _client.PutItemAsync(request);
                Debug.Log($"New User ID: {_userId} has been saved.");
                _userIdSavedOrRetrieved = true;
            }
            catch (AmazonDynamoDBException e)
            {
                Debug.LogError("Error updating name: " + e.Message);
            }
            IsFirstLogin = true;
            GuestLoginSuccess?.Invoke();
        }
        private async Task UpdateItemByKey(string key , string value)
        {
            try
            {
                await GetOrSaveUserId();
                var request = new UpdateItemRequest
                {
                    TableName = TableName,
                    Key = new Dictionary<string, AttributeValue> { { UserIdKey, new AttributeValue { S = _userId } } },
                    AttributeUpdates = new Dictionary<string, AttributeValueUpdate>
                    {
                        {
                            key, new AttributeValueUpdate
                            {
                                Action = AttributeAction.PUT,
                                Value = new AttributeValue
                                {
                                    S = value
                                }
                            }
                        }
                    }
                };
                var response = await _client.UpdateItemAsync(request);
                
                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    Debug.Log("Updated Key: " + key);
                }
            }
            catch (AmazonDynamoDBException e)
            {
                Debug.LogError(e.Message);
            }
        }

        private async Task UpdatePlayerItems()
        {
            var getResult = await _client.GetItemAsync(new GetItemRequest
            {
                Key = new Dictionary<string, AttributeValue>()
                {
                    { UserIdKey, new AttributeValue { S = _userId } }
                },
                TableName = TableName
            });
            if (getResult.Item.Count > 0)
            {
                LocalPlayerItems.userName = getResult.Item[UserNameKey].S;
                LocalPlayerItems.selectedAvatar = getResult.Item[AvatarKey].S;
            }
        }
        

        private async Task<CheckResult> CheckKeyExists(string key , string value)
        {
            
            var getUserResult = await _client.GetItemAsync(TableName, new Dictionary<string, AttributeValue>()
            {
                { key, new AttributeValue { S = value } }
            });

            CheckResult result = new CheckResult
            {
                exists = false,
                items = null
            };
            if (getUserResult.HttpStatusCode == HttpStatusCode.OK)
            {
                if (getUserResult.Item != null && getUserResult.Item.Count > 0)
                {
                    result = new CheckResult
                    {
                        exists = true,
                        items = getUserResult.Item,

                    };
                }
            }

            return result;
        }
        
        public async void UpdateUserName(string userName)
        {
            await UpdateItemByKey(UserNameKey, userName);
            LocalPlayerItems.userName = userName;
            UserNameUpdated?.Invoke();
        }

        public async void UpdateUserAvatar(string avatarId)
        {
            await UpdateItemByKey(AvatarKey, avatarId);
            LocalPlayerItems.selectedAvatar = avatarId;
            AvatarUpdated?.Invoke();
        }

        [Serializable]
        public class PlayerItems
        {
            public string userName = "";
            public string selectedAvatar = "";
        }

        [Serializable]
        private class CheckResult
        {
            public bool exists;
            public Dictionary<string , AttributeValue> items;
        }
    }
}
