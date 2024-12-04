using System;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

namespace Networking.Gameplay
{
    public class PuzzleGameplayManager : NetworkBehaviour
    {

        [SerializeField] private PuzzlePieceGameplay piecePrefab;
        [SerializeField] private Transform piecePlacepoint;
        [SerializeField] private GameObject piecesScroller;
        [SerializeField] PuzzlePiecesContainer pieceContainer;
        [SerializeField] List<PieceTrigger> pieceTriggers;

        public int maxPlayerCount;
        private PuzzlePieceGameplay _lastPiece;
        
        private List<PuzzlePlayer> _puzzlePlayers;
        private int _currentPlayerIndex;

        private void OnEnable()
        {
            PuzzleNetworkManager.OnPlayerConnected += OnPlayerConnected;
        }

        private void OnDisable()
        {
            PuzzleNetworkManager.OnPlayerConnected -= OnPlayerConnected;
        }

        [TargetRpc]
        private void TargetSetScroller(NetworkConnectionToClient target , bool active)
        {
            piecesScroller.SetActive(active);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            SetTriggerIndices();
        }

        [Client]
        private void SetTriggerIndices()
        {
            for (int i = 0; i < pieceTriggers.Count; i++)
            {
                pieceTriggers[i].pieceIndex = i;
            }
        }

        [Client]
        public void GetPuzzleSprites(Action<List<Sprite>> onSpritesLoaded)
        {
            onSpritesLoaded?.Invoke(pieceContainer.sharedPieces);
            pieceContainer.pieceTriggers.Clear();
            pieceContainer.pieceTriggers = pieceTriggers;
        }

        [Command(requiresAuthority = false)]
        public void CmdSpawnPuzzleAtSpawnPoint(int index)
        {
            ServerDestroyLastPiece();
            PuzzlePieceGameplay piece = Instantiate(piecePrefab, piecePlacepoint.transform.position, Quaternion.identity);
            NetworkServer.Spawn(piece.gameObject);
            _lastPiece = piece;
            piece.ServerPiecePlaced += OnPlayerPiecePlaced;
            piece.RpcSetSprite(index);
            piece.netIdentity.AssignClientAuthority(GetCurrentPlayer().connectionToClient);
        }

        [Server]
        private void ServerDestroyLastPiece()
        {
            if (_lastPiece && !_lastPiece.IsPlaced) NetworkServer.Destroy(_lastPiece.gameObject);
        }

        [Server]
        private void OnPlayerPiecePlaced(bool isCorrect)
        {
            GetCurrentPlayer().ServerUpdatePlayerOnPiecePlaced(isCorrect);
        }

        [Server]
        private PuzzlePlayer GetCurrentPlayer()
        {
            return _puzzlePlayers[_currentPlayerIndex % _puzzlePlayers.Count];
        }
        
        [Server]
        private void ServerStartPlayerTurn()
        {
            TargetSetScroller(GetCurrentPlayer().connectionToClient, true);
        }
        
        [Server]
        private void OnPlayerConnected(PuzzleNetworkManager.PlayerConnectionInfo obj)
        {
            _puzzlePlayers ??= new List<PuzzlePlayer>();

            if (obj.conn.identity.TryGetComponent(out PuzzlePlayer p))
            {
                _puzzlePlayers.Add(p);
                p.TurnEnded += ServerPlayerTurnEnded;
            }

            if (_puzzlePlayers.Count >= maxPlayerCount)
            {
                ServerStartPlayerTurn();
            }
        }
        
        [Server]
        private void ServerPlayerTurnEnded()
        {
            ServerDestroyLastPiece();
            TargetSetScroller(GetCurrentPlayer().connectionToClient, false);
            _currentPlayerIndex++;
            ServerStartPlayerTurn();
        }

    }
}
