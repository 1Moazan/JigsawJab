using System;
using System.Collections.Generic;
using Mirror;
using Networking.Gameplay;
using UnityEngine;

namespace Networking
{
    public class PuzzleUiManager : NetworkBehaviour
    {
        [SerializeField] private PuzzlePieceUI pieceUiPrefab;
        [SerializeField] private Transform pieceUiParent;
        [SerializeField] PuzzleGameplayManager puzzleGameplayManager;

        private List<PuzzlePieceUI> _currPieces;
        private int _index;

        public override void OnStartClient()
        {
            base.OnStartClient();
            SetPuzzlePiecesInUi();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            puzzleGameplayManager.CorrectPiecePlaced += RpcDisableCorrectPieceInUi;
        }

        [ClientRpc]
        private void RpcDisableCorrectPieceInUi(int pieceIndex)
        {
            if (pieceIndex >= _currPieces.Count) return;

            var p = _currPieces.Find(p => p.PiceIndex == pieceIndex);
            if (p != null)
            {
                p.gameObject.SetActive(false);
            }
        }

        [Client]
        private void SetPuzzlePiecesInUi()
        {
            _currPieces ??= new List<PuzzlePieceUI>();
            puzzleGameplayManager.PieceAnimationCompleted += SetNextPieceActive;
            puzzleGameplayManager.GetPuzzleSprites((sprites) =>
            {
                for(int i = 0; i < sprites.Count; i++)
                {
                    PuzzlePieceUI uiPiece = Instantiate(pieceUiPrefab, pieceUiParent);
                    uiPiece.InitializeUiPiece(i, sprites[i]);
                    uiPiece.gameObject.SetActive(false);
                    _currPieces.Add(uiPiece);
                    uiPiece.mainButton.onClick.AddListener(() =>
                    {
                        Vector3 pos = Camera.main.ScreenToWorldPoint(uiPiece.mainButton.transform.position);
                        puzzleGameplayManager.CmdSpawnPuzzleAtSpawnPoint(uiPiece.PiceIndex, (Vector2)pos);
                    });
                }
            });
        }

        [Client]
        private void SetNextPieceActive()
        {
            if (_currPieces == null || _currPieces.Count < 1) return;
            _currPieces[_index].gameObject.SetActive(true);
            _index++;
        }
        
    }
}
