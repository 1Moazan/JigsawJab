using Mirror;
using Networking;
using UnityEngine;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;
using System.Collections;

namespace GameLiftManagers
{
    public class GameliftAuthenticator : NetworkAuthenticator
    {
        [SerializeField] private SharedConnectionData connectionData;
        public struct AuthRequestMessage : NetworkMessage 
        {
            public string PlayerSessionId;
            public string PlayerId;
        }

        public struct AuthResponseMessage : NetworkMessage 
        {
            public byte Code;
            public string Message;
        }

    /// <summary>
    /// Called on server from StartServer to initialize the Authenticator
    /// <para>Server message handlers should be registered in this method.</para>
    /// </summary>
    public override void OnStartServer()
    {
        // register a handler for the authentication request we expect from client
        NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);
    }

    /// <summary>
    /// Called on server when the client's AuthRequestMessage arrives
    /// </summary>
    /// <param name="conn">Connection to client.</param>
    /// <param name="msg">The message payload</param>
    public void OnAuthRequestMessage(NetworkConnection conn, AuthRequestMessage msg)
    {
        var playerSessions = GameLiftServerAPI.DescribePlayerSessions(new DescribePlayerSessionsRequest()
        {
            PlayerSessionId = msg.PlayerSessionId
        });

        if (playerSessions.Result.PlayerSessions.Count > 0)
        {
            conn.Send(new AuthResponseMessage() 
            { 
                Code = 100,
                Message = "Authentication successful"
            });

            GameLiftServerAPI.AcceptPlayerSession(msg.PlayerSessionId);

            // Accept the successful authentication
            ServerAccept(conn as NetworkConnectionToClient);
        }
        else
        {
            conn.Send(new AuthResponseMessage()
            {
                Code = 200,
                Message = $"Authentication failed. The server could not find any Player Sessions with the Player Session ID {msg.PlayerSessionId}"
            });
            conn.isAuthenticated = false;
            DelayedDisconnect(conn, 1);
        }
        
        ClientAccept();
    }

    Coroutine DelayedDisconnect(NetworkConnection conn, float waitTime)
    {
        return StartCoroutine(delayedDisconnectRoutine());
        IEnumerator delayedDisconnectRoutine()
        {
            yield return new WaitForSeconds(waitTime);

            // Reject the unsuccessful authentication
            ServerReject(conn as NetworkConnectionToClient);
        }
    }
    
    
        public override void OnClientAuthenticate()
        {
            if (GameLiftClientManager.CurrentPlayerSession == null) return;
            NetworkClient.Send(new AuthRequestMessage()
            {
                PlayerId = GameLiftClientManager.CurrentPlayerSession.PlayerId,
                PlayerSessionId = GameLiftClientManager.CurrentPlayerSession.PlayerSessionId
            });
        }

        /// <summary>
        /// Called on client from StartClient to initialize the Authenticator
        /// <para>Client message handlers should be registered in this method.</para>
        /// </summary>
        public override void OnStartClient()
        {
            // register a handler for the authentication response we expect from server
            NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
        }

        /// <summary>
        /// Called on client when the server's AuthResponseMessage arrives
        /// </summary>
        /// <param name="conn">Connection to client.</param>
        /// <param name="msg">The message payload</param>
        public void OnAuthResponseMessage(AuthResponseMessage msg)
        {
            if (msg.Code == 100)
            {
                Debug.Log(msg.Message);

                // Authentication has been accepted
                ClientAccept();
            }
            else
            {
                Debug.LogError(msg.Message, gameObject);
                ClientReject();
            }
        }
    }
}