using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Client
{
    public class ProfileManager : MonoBehaviour
    {
        private const string IdentityPoolId = "us-east-1:3c9345bb-60de-4433-88c3-0384b359168a";
        private const string TableName = "arn:aws:dynamodb:us-east-1:339713110717:table/UserInfo";
        private const string UserIdKey = "UserId";
        private AmazonDynamoDBClient _client;
        private string _userId;
        private bool _userIdSaved;

        void Start()
        {
            InitializeGuestLogin();
        }

        private async void InitializeGuestLogin()
        {
            CognitoAWSCredentials credentials = new CognitoAWSCredentials(IdentityPoolId, RegionEndpoint.USEast1);
            _client = new AmazonDynamoDBClient(credentials, RegionEndpoint.USEast1);
            _userId = SystemInfo.deviceUniqueIdentifier;
            await SaveUserId();
        }

        private async Task SaveUserId()
        {
            if (_userIdSaved) return;
            try
            {
                var request = new PutItemRequest
                {
                    TableName = TableName,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { UserIdKey, new AttributeValue { S = _userId }}
                    }
                };
                await _client.PutItemAsync(request);
                _userIdSaved = true;
            }
            catch (AmazonDynamoDBException e)
            {
                Debug.LogError("Error updating name: " + e.Message);
            }
        }

        private async Task UpdateUserName(string userName)
        {
            try
            {
                await SaveUserId();
                var request = new UpdateItemRequest
                {
                    TableName = TableName,
                    Key = new Dictionary<string, AttributeValue> { { UserIdKey, new AttributeValue { S = _userId } } },
                    AttributeUpdates = new Dictionary<string, AttributeValueUpdate>
                    {
                        {
                            "UserName", new AttributeValueUpdate
                            {
                                Action = AttributeAction.PUT,
                                Value = new AttributeValue
                                {
                                    S = userName,
                                }
                            }
                        }
                    }
                };
                var response = _client.UpdateItemAsync(request);

                await response;
                if (response.Result.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    Debug.Log("Updated user name: " + userName);
                }
            }
            catch (AmazonDynamoDBException e)
            {
                Debug.LogError(e.Message);
            }
        }
    }
}
