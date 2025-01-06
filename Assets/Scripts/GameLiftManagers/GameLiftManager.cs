using System;
using kcp2k;
using Networking;
using UnityEngine;

namespace GameLiftManagers
{
    public class GameLiftManager : MonoBehaviour
    {
        [SerializeField] private GameLiftClientManager clientManager;
        [SerializeField] private GameLiftServerManager serverManager;

        [SerializeField] private NetworkSettings networkSettings;
        [SerializeField] private GameLiftBuildType buildType;
        private void Start()
        {
            switch (buildType)
            {
                case GameLiftBuildType.Server:
                    serverManager.gameObject.SetActive(true);
                    serverManager.InitializeAndStart(networkSettings);
                    break;
                case GameLiftBuildType.Client:
                    clientManager.gameObject.SetActive(true);
                    clientManager.InitializeAndStart(networkSettings);
                    break;
            }
        }
    }

    [Serializable]
    public class NetworkSettings
    {
        public SharedConnectionData connectionData;
        public PuzzleNetworkManager networkManager;
        public KcpTransport transport;
    }

    public enum GameLiftBuildType
    {
        Server,
        Client
    }
}
