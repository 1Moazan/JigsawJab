using System;
using UnityEngine;

namespace Networking
{
    [CreateAssetMenu(menuName = "Data/Shared Connection Data" , fileName = "Shared Connection Data")]
    public class SharedConnectionData : ScriptableObject
    {
        private ConnectionData _connectionData;
        [SerializeField] private ConnectionData localConnectionData;
        
        
        public bool isLocal;
        public ConnectionData GetConnectionData()
        {
            return _connectionData;
        }

        public void SetRemoteConnectionData(ConnectionData connectionData)
        {
            _connectionData = connectionData;
        }
    }

    [Serializable]
    public class ConnectionData
    {
        public string IPAddress;
        public int PortNo;
        public bool isServer;

        public ConnectionData(string ipAddress, int portNo, bool isServer)
        {
            this.IPAddress = ipAddress;
            this.PortNo = portNo;
            this.isServer = isServer;
        }
    }
}
