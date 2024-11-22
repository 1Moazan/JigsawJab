using UnityEngine;

namespace Networking
{
    [CreateAssetMenu(menuName = "Data/Shared Connection Data" , fileName = "Shared Connection Data")]
    public class SharedConnectionData : ScriptableObject
    {
        public ConnectionData ConnectionData;
    }

    
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
