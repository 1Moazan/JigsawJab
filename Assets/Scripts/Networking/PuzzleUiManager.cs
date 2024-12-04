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

        public override void OnStartClient()
        {
            base.OnStartClient();
            SetPuzzlePiecesInUi();
        }

        private void SetPuzzlePiecesInUi()
        {
            puzzleGameplayManager.GetPuzzleSprites((sprites) =>
            {
                for(int i = 0; i < sprites.Count; i++)
                {
                    PuzzlePieceUI uiPiece = Instantiate(pieceUiPrefab, pieceUiParent);
                    uiPiece.InitializeUiPiece(i, sprites[i]);
                    int i1 = i;
                    uiPiece.mainButton.onClick.AddListener(() =>
                    {
                        puzzleGameplayManager.CmdSpawnPuzzleAtSpawnPoint(i1);
                    });
                }
            });
        }
    }
}
