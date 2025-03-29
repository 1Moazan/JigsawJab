using System;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    [CreateAssetMenu(menuName = "Data/Shared Connection Data" , fileName = "Shared Connection Data")]
    public class SharedConnectionData : ScriptableObject
    {
        [SerializeField] private List<ConnectionTypeData> connectionTypes;
        [SerializeField] private ConnectionType currentConnectionType;
        [SerializeField] private ClientType currentClientType;
        
        public ConnectionType CurrentConnectionType => currentConnectionType;
        public ClientType CurrentClientType => currentClientType;

        public ConnectionData GetConnectionData()
        {
            var connectionType = connectionTypes.Find(conn => conn.connectionType == currentConnectionType);
            return connectionType.connectionData;
        }

        public void SetConnectionData(ConnectionData setData)
        {
            if (currentConnectionType == ConnectionType.Remote)
            {
                var connectionType = connectionTypes.Find(conn => conn.connectionType == ConnectionType.Remote);
                connectionType.connectionData = setData;
            }
        }
    }

    [Serializable]
    public class ConnectionData
    {
        public string ipAddress;
        public int portNo;

        public ConnectionData(string ipAddress, int portNo)
        {
            this.ipAddress = ipAddress;
            this.portNo = portNo;
        }
    }

    public enum ConnectionType
    {
        LocalHost,
        LocalGsdk,
        Remote
    }

    public enum ClientType
    {
        Server,
        Client
    }

    [Serializable]
    public class ConnectionTypeData
    {
        public ConnectionType connectionType;
        public ConnectionData connectionData;
    }
}
