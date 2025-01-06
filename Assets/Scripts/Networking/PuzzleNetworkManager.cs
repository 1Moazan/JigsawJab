using System;
using System.Collections;
using System.Collections.Generic;
using Aws.GameLift.Server;
using Mirror;
using UnityEngine;

namespace Networking
{
    public class PuzzleNetworkManager : NetworkManager
    {
        public static List<PlayerConnectionInfo> Connections = new List<PlayerConnectionInfo>();
        
        public static Action<PlayerConnectionInfo> OnPlayerConnected;
        public static Action ManagerShutdown;
        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("Server Started");
        }

        private void StopGameLiftServer()
        {
            var outcome = GameLiftServerAPI.ProcessEnding();

            if (outcome.Success)
            {
                GameLiftServerAPI.Destroy();
                Debug.Log("Server Terminated");
            }
            else
            {
                Debug.LogError("Server Termination Failed + " + outcome.Error.ErrorMessage);
            }
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

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);

            var find = Connections.Find(c => c.connectionId == conn.connectionId.ToString());

            if (find != null)
            {
                Connections.Remove(find);
            }

            if (Connections.Count < 1)
            {
                StartCoroutine(ShutdownServer());
            }

        }

        private IEnumerator ShutdownServer()
        {
            NetworkServer.Shutdown();
            yield return new WaitUntil(() => !NetworkServer.active);
            ManagerShutdown?.Invoke();
        }

        [Serializable]
        public class PlayerConnectionInfo
        {
            public string connectionId;
            public NetworkConnection conn;
        }
    }
}
