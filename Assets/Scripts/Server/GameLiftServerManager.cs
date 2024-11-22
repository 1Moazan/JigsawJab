using System;
using System.Collections;
using System.Collections.Generic;
using Aws.GameLift;
using Aws.GameLift.Server;
using Constants;
using Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Server
{
    public class GameLiftServerManager : MonoBehaviour
    {

        private const string socketURl = "wss://us-east-2.api.amazongamelift.com";
        private const string authToken = "06098aa4-de26-44b2-a1a5-661b834b6c7a";
        
        [SerializeField] private SharedConnectionData connectionData;
        private bool _sessionCreated;

        public bool isLocal;

        void Start()
        {
            StartCoroutine(ShiftToGameplayDelayed());

            GenericOutcome outcome;
            string pID = Guid.NewGuid().ToString();
            if (isLocal)
            {
                outcome = GameLiftServerAPI.InitSDK(new ServerParameters
                {
                    WebSocketUrl = socketURl,
                    ProcessId = pID,
                    HostId = GameLiftConstants.ComputeName,
                    FleetId = GameLiftConstants.FleetId,
                    AuthToken = authToken
                });
            }
            else
            {
                outcome = GameLiftServerAPI.InitSDK();
            }

            if (outcome.Success)
            {
                Debug.Log("Server Initialized PID : " + pID);
            }
            else
            {
                Debug.LogError("Server Initialization Failed + " + outcome.Error.ErrorMessage);
            }
       

            GameLiftServerAPI.ProcessReady(new ProcessParameters
            {
                OnStartGameSession = (session) =>
                {
                   
                    var gameSession = GameLiftServerAPI.ActivateGameSession();
                    if (gameSession.Success)
                    {
                        Debug.Log("Session Started");
                        Debug.Log("Session Ip : " + session.IpAddress);
                        Debug.Log("Session Port : " + session.Port);

                        connectionData.ConnectionData = new ConnectionData(session.IpAddress, session.Port, true);
                        _sessionCreated = true;
                    }
                    else
                    {
                        Debug.LogError("Game Session Activation Failed + " + gameSession.Error.ErrorMessage);
                    }
                },
                OnUpdateGameSession = (updateCheck) =>
                {

                },
                OnProcessTerminate = () =>
                {
                
                },
                OnHealthCheck = () =>
                {
                    Debug.Log("Server Health Check");
                    return true;
                },
                Port = 7777,
                LogParameters = new LogParameters(new List<string>()
                {
                    "/local/game/logs/server.log"
                })
            });
        }

        private IEnumerator ShiftToGameplayDelayed()
        {
            yield return new WaitUntil(() => _sessionCreated);
            SceneManager.LoadScene("Gameplay");
        }

        private void OnDisable()
        {
            //StopGameLiftServer();
        }

        [ContextMenu("Stop Server")]
        private void StopGameLiftServer()
        {
            var outcome = GameLiftServerAPI.ProcessEnding();

            if (outcome.Success)
            {
                GameLiftServerAPI.Destroy();
                Debug.Log("Server Terminated");
            }
            else
            {
                Debug.LogError("Server Termination Failed + " + outcome.Error.ErrorMessage);
            }
        }
    }
}
