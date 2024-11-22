using kcp2k;
using UnityEngine;

namespace Networking
{
    public class ConnectionConfiguration : MonoBehaviour
    {
        [SerializeField] private SharedConnectionData sharedConnectionData;
        [SerializeField] private PuzzleNetworkManager networkManager;
        [SerializeField] private KcpTransport transport;

        private void Start()
        {
            SetConnectionData();
        }

        private void SetConnectionData()
        {
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
