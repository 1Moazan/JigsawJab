using System;
using System.Collections.Generic;
using Client;
using DG.Tweening;
using Mirror;
using Networking.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Networking
{
    public class PuzzleUiManager : NetworkBehaviour
    {
        [SerializeField] private PuzzlePieceUI pieceUiPrefab;
        [SerializeField] private Transform pieceUiParent;
        [SerializeField] PuzzleGameplayManager puzzleGameplayManager;

        private List<PuzzlePieceUI> _currPieces;
        private int _index;
        private List<AvatarPreview> _previewObjects;
        private Sequence _avatarSeq;
        private AvatarPreview _lastAvatarPreview;
        private string _selectedPuzzleKey;
        [SerializeField] private AvatarPreview previewPrefab;
        [SerializeField] private GameObject selectPuzzlePanel;
        [SerializeField] private GameObject waitForPuzzlePanel;
        [SerializeField] private RectTransform punchPanel;
        [SerializeField] private RectTransform waitPunchPanel;
        [SerializeField] private Transform previewParent;
        [SerializeField] private AvatarDataContainer dataContainer;
        [SerializeField] private Button selectPuzzlebutton;

        public override void OnStartClient()
        {
            base.OnStartClient();
            SetPuzzles();
            selectPuzzlebutton.onClick.AddListener(SelectPuzzle);
            puzzleGameplayManager.ClientPuzzleSelected += PuzzleSelected;
            puzzleGameplayManager.PuzzleSelectionStarted += SetSelectionPanelActive;
        }

        private void OnDisable()
        {
            puzzleGameplayManager.ClientPuzzleSelected -= PuzzleSelected;
            puzzleGameplayManager.PuzzleSelectionStarted -= SetSelectionPanelActive;
            selectPuzzlebutton.onClick.RemoveListener(SelectPuzzle);
        }

        private void PuzzleSelected(string obj)
        {
            SetPanel(selectPuzzlePanel, punchPanel);
            SetPanel(waitForPuzzlePanel, waitPunchPanel);

            SetPuzzlePiecesInUi(obj);
        }

        private void SelectPuzzle()
        {
            selectPuzzlebutton.interactable = false;
            puzzleGameplayManager.CmdSelectPuzzle(_selectedPuzzleKey);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            puzzleGameplayManager.CorrectPiecePlaced += RpcDisableCorrectPieceInUi;
            puzzleGameplayManager.GameReset += GameReset;
        }

        [Server]
        private void GameReset()
        {
            RpcResetUI();
        }

        [Client]
        private void SetSelectionPanelActive(bool isSelecting)
        {
            SetSelectionUI(isSelecting);
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
        private void SetPuzzlePiecesInUi(string key)
        {
            _currPieces ??= new List<PuzzlePieceUI>();
            puzzleGameplayManager.PieceAnimationCompleted += SetNextPieceActive;
            
            var sprite = dataContainer.puzzlesList.Find(p => p.avatarKey == key);
            puzzleGameplayManager.GetPuzzleSprites(sprite.avatarSprite ,(sprites) =>
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

        [ClientRpc]
        private void RpcResetUI()
        {
            foreach (PuzzlePieceUI pieceUI in _currPieces)
            {
                Destroy(pieceUI.gameObject);
            }
            _currPieces.Clear();
            _index = 0;
            puzzleGameplayManager.PieceAnimationCompleted -= SetNextPieceActive;
        }
        
        private void SetPuzzles()
        {
            _previewObjects ??= new List<AvatarPreview>();
            ClearContainer();
            _avatarSeq = DOTween.Sequence();
            foreach (AvatarData avatarData in dataContainer.puzzlesList)
            {
                AvatarPreview avatarPreview = Instantiate(previewPrefab, previewParent);
                
                _avatarSeq.Append(avatarPreview.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.1f).SetDelay(0.3f));
                avatarPreview.SetValues(avatarData, () =>
                {
                    if(_lastAvatarPreview != null) _lastAvatarPreview.SetSelected(false);
                    _selectedPuzzleKey = avatarPreview.AvatarKey;
                    avatarPreview.SetSelected(true);
                    _lastAvatarPreview = avatarPreview;
                });
            }
        }
        
        private void ClearContainer()
        {
            if(_previewObjects == null || _previewObjects.Count < 1) return;
            foreach (AvatarPreview preview in _previewObjects)
            {
                Destroy(preview.gameObject);
            }
            _previewObjects.Clear();
        }
        
        private void SetPanel(GameObject panel, RectTransform childPanel, bool active = false)
        {
            panel.SetActive(active);
            if (active) DefaultMove(childPanel);
            else childPanel.DOKill();
        }
        
        private void DefaultMove(RectTransform moveTransform)
        {
            moveTransform.anchoredPosition = new Vector2(-1500, moveTransform.anchoredPosition.y);
            moveTransform.DOAnchorPosX(0, 0.5f).SetEase(Ease.OutElastic);
        }

        private void SetSelectionUI(bool isSelecting)
        {
            SetPanel(selectPuzzlePanel, punchPanel);
            SetPanel(waitForPuzzlePanel, waitPunchPanel);

            if (isSelecting)
            {
                SetPanel(selectPuzzlePanel, punchPanel, true);
                selectPuzzlebutton.interactable = true;
            }
            else
            {
                SetPanel(waitForPuzzlePanel, waitPunchPanel, true);
            }

        }

    }
}
