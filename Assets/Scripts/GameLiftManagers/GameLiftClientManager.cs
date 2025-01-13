using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using kcp2k;
using Networking;
using UnityEngine;

namespace GameLiftManagers
{
    public class GameLiftClientManager : MonoBehaviour
    {
        public static PlayerSession CurrentPlayerSession;
        private AmazonGameLiftClient _client;
        private PlayerSession _currentPlayerSession;
        private string _playerId;
        private bool _sessionActive;
        private string _currFleetId;
        
        private NetworkSettings _networkSettings;

        public void InitializeAndStart(NetworkSettings networkSettings)
        {
            _networkSettings = networkSettings;
            AmazonGameLiftConfig config = new AmazonGameLiftConfig();

            if (!_networkSettings.connectionData.isLocal)
            {
                config.RegionEndpoint = GameLiftConstants.Region;
            }

            _client = new AmazonGameLiftClient(GameLiftConstants.AccessKey, GameLiftConstants.SecretKey, config);
            _playerId = Guid.NewGuid().ToString();
            FindOrCreateGame();
        }

        private async void FindOrCreateGame()
        {
            try
            {
                if (!_networkSettings.connectionData.isLocal)
                {
                    var fleets = await GetFleets();
                    if (!fleets.Any())
                    {
                        Debug.LogError("No active fleets found.");
                        return;
                    }

                    Debug.Log($"Found {fleets.Count} active fleets.");
                    var fleet = fleets.First();

                    if (string.IsNullOrEmpty(fleet))
                    {
                        Debug.LogError("No active fleet found.");
                    }
                    else
                    {
                        _currFleetId = fleet;
                    }
                }
                else
                {
                    _currFleetId = GameLiftConstants.FleetId;
                }

                var sessions = await GetAvailableGameSessionsAsync();
                Debug.Log($"Found {sessions.Count} game sessions.");

                if (sessions.Any())
                {
                    var activeSession = sessions.FirstOrDefault(s =>
                        s.Status == GameSessionStatus.ACTIVE && s.CurrentPlayerSessionCount < 2);

                    if (activeSession != null)
                    {
                        Debug.Log($"Joining existing game session: {activeSession.GameSessionId}");
                        await JoinGame(activeSession);
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
            await JoinGame(gameSession);
        }

        private async Task JoinGame(GameSession gameSession)
        {
            while (!_sessionActive)
            {
                await Task.Delay(2000); // Check status every 2 seconds
                var activeGameSession = await GetActiveGameSession(gameSession.GameSessionId);

                Debug.Log("Session State : " + activeGameSession.Status);
                if (activeGameSession.Status == GameSessionStatus.ACTIVE)
                {
                    var playerSession = await CreatePlayerSessionAsync(gameSession.GameSessionId);
                    CurrentPlayerSession = playerSession;
                    Debug.Log($"Joining game at {activeGameSession.IpAddress}:{activeGameSession.Port}");
                    _networkSettings.networkManager.networkAddress = activeGameSession.IpAddress;
                    _networkSettings.transport.port = (ushort)activeGameSession.Port;
                    _networkSettings.networkManager.StartClient();
                    _sessionActive = true;
                }
            }
        }

        private async Task<List<string>> GetFleets(CancellationToken token = default)
        {
            var response = await _client.ListFleetsAsync(new ListFleetsRequest(), token);
            return response.FleetIds;
        }

        private async Task<List<GameSession>> GetAvailableGameSessionsAsync(CancellationToken token = default)
        {
            var response = await _client.DescribeGameSessionsAsync(new DescribeGameSessionsRequest
            {
                FleetId = _currFleetId,
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
            if (_networkSettings.connectionData.isLocal)
            {
                var response = await _client.CreateGameSessionAsync(new CreateGameSessionRequest
                {
                    FleetId = _currFleetId,
                    Location = GameLiftConstants.LocationName,
                    MaximumPlayerSessionCount = 2,
                    Name = "QuickSession"
                }, token);
                return response.GameSession;
            }
            else
            {
                var response = await _client.CreateGameSessionAsync(new CreateGameSessionRequest
                {
                    FleetId = _currFleetId,
                    MaximumPlayerSessionCount = 2,
                    Name = "QuickSession"
                }, token);
                return response.GameSession;
            }
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
