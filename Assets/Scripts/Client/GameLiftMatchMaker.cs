using System;
using System.Collections.Generic;
using Amazon;
using Amazon.GameLift;
using Amazon.GameLift.Model;
using Amazon.Internal;
using Constants;
using UnityEngine;

namespace Client
{
    public class GameLiftMatchMaker : MonoBehaviour
    {
        private AmazonGameLiftClient _client;
        private string _playerId;

        // Start is called before the first frame update
        void Start()
        {
            var config = new AmazonGameLiftConfig();
            
            config.RegionEndpoint = RegionEndpoint.USEast1;
            _client = new AmazonGameLiftClient(GameLiftConstants.AccessKey, GameLiftConstants.SecretKey, config);
            _playerId = Guid.NewGuid().ToString();

            MatchMakeAsync();
        }

        public async void MatchMakeAsync()
        {
            Player player = new Player();
            player.PlayerId = Guid.NewGuid().ToString();
            List<Player> players = new List<Player>(new[] { player }); 
            var response = await _client.StartMatchmakingAsync(new StartMatchmakingRequest
            {
                ConfigurationName = "arn:aws:gamelift:us-east-1:339713110717:matchmakingconfiguration/SampleMatchamker12",
                Players = players
            });
            
            Debug.Log(response.MatchmakingTicket.GameSessionConnectionInfo.IpAddress);
        }

        // [ContextMenu("Make Game Lift Config")]
        // public void CreateConfig()
        // {
        //     _client.CreateMatchmakingConfiguration(new CreateMatchmakingConfigurationRequest
        //     {
        //         Name = "QuickMatch"
        //     });
        // }
    }
}
