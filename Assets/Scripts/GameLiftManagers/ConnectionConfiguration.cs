using kcp2k;
using Networking;
using UnityEngine;

namespace GameLiftManagers
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
            ConnectionData data = sharedConnectionData.GetConnectionData();
            networkManager.networkAddress = data.IPAddress;
            transport.Port = (ushort)data.PortNo;
            if (data.isServer)
            {
                Debug.Log("Starting Server");
                networkManager.StartServer();
            }
            else
            {
                Debug.Log("Starting Client");
                networkManager.StartClient();
            }
        }
    }
}
