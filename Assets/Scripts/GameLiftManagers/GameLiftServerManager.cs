#if UNITY_SERVER || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Aws.GameLift;
using Aws.GameLift.Server;
using Networking;
using UnityEngine;

namespace GameLiftManagers
{
    public class GameLiftServerManager : MonoBehaviour
    {

        public string socketUrl = "wss://us-east-2.api.amazongamelift.com";
        public string authToken = "fc86f519-5a44-400c-b0f6-e1708bc64ed8";
        private NetworkSettings _networkSettings;

        private void OnEnable()
        {
            PuzzleNetworkManager.ManagerShutdown += StopGameLiftServer;
        }

        private void OnDisable()
        {
            PuzzleNetworkManager.ManagerShutdown -= StopGameLiftServer;
        }

        public void InitializeAndStart(NetworkSettings networkSettings)
        {
            _networkSettings = networkSettings;

            GenericOutcome outcome;
            string pID = Guid.NewGuid().ToString();
            if (_networkSettings.connectionData.isLocal)
            {
                outcome = GameLiftServerAPI.InitSDK(new ServerParameters
                {
                    WebSocketUrl = socketUrl,
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
                StopGameLiftServer();
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
                        StartCoroutine(StartServerDelayed());
                    }
                    else
                    {
                        Debug.LogError("Game Session Activation Failed + " + gameSession.Error.ErrorMessage);
                    }
                },
                OnUpdateGameSession = (updateCheck) => { },
                OnProcessTerminate = () => { StopGameLiftServer(); },
                OnHealthCheck = () =>
                {
                    Debug.Log("Server Health Check");
                    return true;
                },
                Port = 7777,
                LogParameters = new LogParameters(new List<string>()
                {
                    "/local/game/server.log"
                })
            });
        }

        private IEnumerator StartServerDelayed()
        {
            yield return null;
            _networkSettings.networkManager.StartServer();
        }
        
        [ContextMenu("Stop Server")]
        private void StopGameLiftServer()
        {
            try
            {
                GenericOutcome outcome = GameLiftServerAPI.ProcessEnding();

                if (outcome.Success)
                {
                    Debug.Log(":) PROCESSENDING");
                }
                else
                {
                    Debug.Log(":( PROCESSENDING FAILED. ProcessEnding() returned " + outcome.Error.ToString());
                }
            }
            catch (Exception e)
            {
                Debug.Log(":( PROCESSENDING FAILED. ProcessEnding() exception " + Environment.NewLine + e.Message);
            }
            finally
            {
                GameLiftServerAPI.Destroy();
                Application.Quit();
            }
        }

        private void OnApplicationQuit()
        {
            StopGameLiftServer();
        }
    }
}

#endif
