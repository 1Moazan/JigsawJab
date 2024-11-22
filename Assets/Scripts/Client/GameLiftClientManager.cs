using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using Constants;
using Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Client
{
    public class GameLiftClientManager : MonoBehaviour
    {
        public string PlayerSessionId => _currentPlayerSession?.PlayerSessionId;
        public string PlayerId => _playerId;

        [SerializeField] private SharedConnectionData connectionData;

        private AmazonGameLiftClient _client;
        private PlayerSession _currentPlayerSession;
        private string _playerId;
        private bool _sessionActive;

        void Start()
        {
            var config = new AmazonGameLiftConfig();
            _client = new AmazonGameLiftClient(GameLiftConstants.AccessKey, GameLiftConstants.SecretKey, config);
            _playerId = Guid.NewGuid().ToString();

            StartCoroutine(ShiftToGameplay());
            FindOrCreateGame();
        }

        private IEnumerator ShiftToGameplay()
        {
            yield return new WaitUntil(() => _sessionActive);
            Debug.Log("Shifting to Gameplay");
            SceneManager.LoadScene("Gameplay");
        }

        private async void FindOrCreateGame()
        {
            try
            {
                var fleets = await GetFleets();
                if (!fleets.Any())
                {
                    Debug.LogError("No active fleets found.");
                    return;
                }

                Debug.Log($"Found {fleets.Count} active fleets.");
                var sessions = await GetAvailableGameSessionsAsync(fleets.First());
                Debug.Log($"Found {sessions.Count} game sessions.");

                if (sessions.Any())
                {
                    var activeSession = sessions.FirstOrDefault(s => s.Status == GameSessionStatus.ACTIVE && s.CurrentPlayerSessionCount < 2);

                    if (activeSession != null && activeSession.CurrentPlayerSessionCount < 1)
                    {
                        Debug.Log($"Joining existing game session: {activeSession.GameSessionId}");
                        var playerSession = await CreatePlayerSessionAsync(activeSession.GameSessionId);
                        await JoinGame(playerSession);
                        return;
                    }
                }

                Debug.Log("Creating a new game session...");
                await CreateAndJoinNewSession();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error finding or creating a game: {e.Message}");
            }
        }

        private async Task CreateAndJoinNewSession()
        {
            var gameSession = await CreateGameSessionAsync();
            await Task.Delay(2000); // Give some time for the session to initialize
            var playerSession = await CreatePlayerSessionAsync(gameSession.GameSessionId);
            await JoinGame(playerSession);
        }

        private async Task JoinGame(PlayerSession playerSession)
        {
            while (!_sessionActive)
            {
                await Task.Delay(2000); // Check status every 2 seconds
                var activeGameSession = await GetActiveGameSession(playerSession.GameSessionId);

                if (activeGameSession.Status == GameSessionStatus.ACTIVE)
                {
                    Debug.Log($"Joining game at {activeGameSession.IpAddress}:{activeGameSession.Port}");
                    connectionData.ConnectionData = new ConnectionData(activeGameSession.IpAddress, activeGameSession.Port, false);
                    _sessionActive = true;
                }
            }
        }

        private async Task<List<string>> GetFleets(CancellationToken token = default)
        {
            var response = await _client.ListFleetsAsync(new ListFleetsRequest(), token);
            return response.FleetIds;
        }

        private async Task<List<GameSession>> GetAvailableGameSessionsAsync(string fleetId, CancellationToken token = default)
        {
            var response = await _client.DescribeGameSessionsAsync(new DescribeGameSessionsRequest
            {
                FleetId = fleetId,
                StatusFilter = GameSessionStatus.ACTIVE.ToString()
            }, token);
            return response.GameSessions;
        }

        private async Task<PlayerSession> CreatePlayerSessionAsync(string gameSessionId, CancellationToken token = default)
        {
            var response = await _client.CreatePlayerSessionAsync(gameSessionId, _playerId, token);
            return response.PlayerSession;
        }

        private async Task<GameSession> CreateGameSessionAsync(CancellationToken token = default)
        {
            var response = await _client.CreateGameSessionAsync(new CreateGameSessionRequest
            {
                FleetId = GameLiftConstants.FleetId,
                Location = GameLiftConstants.LocationName,
                MaximumPlayerSessionCount = 2,
                Name = "QuickSession"
            }, token);
            return response.GameSession;
        }

        private async Task<GameSession> GetActiveGameSession(string gameSessionId, CancellationToken token = default)
        {
            var response = await _client.DescribeGameSessionDetailsAsync(new DescribeGameSessionDetailsRequest
            {
                GameSessionId = gameSessionId
            }, token);

            return response.GameSessionDetails.First().GameSession;
        }
    }
}
