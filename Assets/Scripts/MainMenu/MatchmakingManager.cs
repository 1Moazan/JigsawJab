using System;
using System.Collections;
using Networking;
using PlayFab;
using PlayFab.MultiplayerModels;
using UnityEngine;

namespace MainMenu
{
    public class MatchmakingManager : MonoBehaviour
    {
        [SerializeField] SharedConnectionData sharedConnectionData;
        private const string QueueName = "QuickMatchQueue";

        public void StartMatchmaking(Action<ConnectionData> onSuccess, Action<string> onFailure)
        {
            if (sharedConnectionData.CurrentConnectionType == ConnectionType.LocalGsdk)
            {
                onSuccess?.Invoke(null);
                return;
            }

            var request = new CreateMatchmakingTicketRequest
            {
                QueueName = QueueName,
                Creator = new MatchmakingPlayer
                {
                    Entity = new EntityKey
                    {
                        Id = PlayFabSettings.staticPlayer.EntityId,
                        Type = PlayFabSettings.staticPlayer.EntityType
                    }
                },
                GiveUpAfterSeconds = 30
            };

            PlayFabMultiplayerAPI.CreateMatchmakingTicket(request,
                result => OnMatchmakingTicketCreated(result, onSuccess, onFailure),
                error => onFailure?.Invoke(error.GenerateErrorReport()));
        }

        private void OnMatchmakingTicketCreated(CreateMatchmakingTicketResult result, Action<ConnectionData> onSuccess,
            Action<string> onFailure)
        {
            Debug.Log("Matchmaking ticket created: " + result.TicketId);
            StartCoroutine(PollMatchmakingStatus(result.TicketId, onSuccess, onFailure));
        }

        private IEnumerator PollMatchmakingStatus(string ticketId, Action<ConnectionData> onSuccess,
            Action<string> onFailure)
        {
            while (true)
            {
                var request = new GetMatchmakingTicketRequest
                {
                    TicketId = ticketId,
                    QueueName = QueueName
                };

                PlayFabMultiplayerAPI.GetMatchmakingTicket(request, result =>
                {
                    if (result.Status == "Matched")
                    {
                        GetMatch(result.MatchId, onSuccess, onFailure);
                        StopAllCoroutines();
                    }
                    else
                    {
                        Debug.Log("Matchmaking in progress: " + result.Status);
                    }
                }, error => onFailure?.Invoke(error.GenerateErrorReport()));

                yield return new WaitForSeconds(5f);
            }
        }

        private void GetMatch(string matchId, Action<ConnectionData> onSuccess, Action<string> onFailure)
        {
            var request = new GetMatchRequest
            {
                MatchId = matchId,
                QueueName = QueueName
            };

            PlayFabMultiplayerAPI.GetMatch(request, result =>
            {
                if (result != null && result.ServerDetails != null)
                {
                    var connectionData = new ConnectionData(result.ServerDetails.IPV4Address,
                        result.ServerDetails.Ports[0].Num);
                    Debug.Log("Match found! Server IP: " + result.ServerDetails.IPV4Address);
                    onSuccess?.Invoke(connectionData);
                }
                else
                {
                    onFailure?.Invoke("Failed to retrieve match details.");
                }
            }, error => onFailure?.Invoke(error.GenerateErrorReport()));
        }
    }
}