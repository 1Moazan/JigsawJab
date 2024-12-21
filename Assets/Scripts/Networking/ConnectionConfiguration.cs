using kcp2k;
using UnityEngine;

namespace Networking
{
    public class ConnectionConfiguration : MonoBehaviour
    {
        [SerializeField] private SharedConnectionData sharedConnectionData;
        [SerializeField] private PuzzleNetworkManager networkManager;
        [SerializeField] private KcpTransport transport;
        
        public bool forceLocal;
        public bool isLocalServer;
        public string localIP;
        public ushort localPort;
        private void Start()
        {
            SetConnectionData();
        }

        private void SetConnectionData()
        {
            if (forceLocal)
            {
                networkManager.networkAddress = localIP;
                transport.Port = localPort;
                if (isLocalServer)
                {
                    networkManager.StartServer();
                }
                else
                {
                    networkManager.StartClient();
                }
                return;
            }
            if(sharedConnectionData.ConnectionData == null) return;
            networkManager.networkAddress = sharedConnectionData.ConnectionData.IPAddress;
            transport.Port = (ushort)sharedConnectionData.ConnectionData.PortNo;
            if (sharedConnectionData.ConnectionData.isServer)
            {
                networkManager.StartServer();
            }
            else
            {
                networkManager.StartClient();
            }
        }
    }
}
