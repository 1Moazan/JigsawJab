using Mirror;
using UnityEngine;

namespace Networking
{
    public class PuzzleNetworkManager : NetworkManager
    {
        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("Server Started");
        }
        

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);
            Debug.Log("Player Added : " + conn.connectionId);
        }
    }
}
