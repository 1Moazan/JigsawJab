using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Networking
{
    public class PuzzleNetworkManager : NetworkManager
    {
        public static List<PlayerConnectionInfo> Connections = new List<PlayerConnectionInfo>();
        
        public static Action<PlayerConnectionInfo> OnPlayerConnected;
        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("Server Started");
        }
        

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);
            Debug.Log("Player Added : " + conn.connectionId);
            
            PlayerConnectionInfo info = new PlayerConnectionInfo()
            {
                connectionId = conn.connectionId.ToString(),
                conn = conn
            }; 
            Connections.Add(info);
            
            OnPlayerConnected?.Invoke(info);
        }

        [Serializable]
        public class PlayerConnectionInfo
        {
            public string connectionId;
            public NetworkConnection conn;
        }
    }
}
