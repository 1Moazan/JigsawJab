using System;
using kcp2k;
using Networking;
using UnityEngine;

namespace GameLiftManagers
{
    public class GameLiftManager : MonoBehaviour
    {
        [SerializeField] private GameLiftClientManager clientManager;
        
#if UNITY_SERVER || UNITY_EDITOR
        [SerializeField] private GameLiftServerManager serverManager;
#endif

        [SerializeField] private NetworkSettings networkSettings;
        [SerializeField] private GameLiftBuildType buildType;
        private void Start()
        {
            switch (buildType)
            {
                case GameLiftBuildType.Server:
#if UNITY_SERVER || UNITY_EDITOR
                    serverManager.gameObject.SetActive(true);
                    serverManager.InitializeAndStart(networkSettings);
#endif
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
