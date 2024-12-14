using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Networking.Gameplay
{
    public class PuzzleGameplayManager : NetworkBehaviour
    {

        [SerializeField] private PuzzlePieceGameplay piecePrefab;
        [SerializeField] private SpriteRenderer animationPrefab;
        [SerializeField] private Transform piecePlacepoint;
        [SerializeField] private RectTransform piecesPanel;
        [SerializeField] private GameObject blockerImage;
        [SerializeField] PuzzlePiecesContainer pieceContainer;
        [SerializeField] List<PieceTrigger> pieceTriggers;
        [SerializeField] private ParticleSystem correctVfx;
        [SerializeField] Sprite demoTexture;

        public int maxPlayerCount;
        public Action PieceAnimationCompleted;
        public Action<int> CorrectPiecePlaced;

        
        private PuzzlePieceGameplay _lastPiece;
        private List<PuzzlePlayer> _puzzlePlayers;
        private int _currentPlayerIndex;
        private bool _firstTurn = true;
        private int _animationEndedCount;

        private void OnEnable()
        {
            PuzzleNetworkManager.OnPlayerConnected += OnPlayerConnected;
        }

        private void OnDisable()
        {
            PuzzleNetworkManager.OnPlayerConnected -= OnPlayerConnected;
        }
        

        [TargetRpc]
        private void TargetSetTurnUI(NetworkConnectionToClient target, bool active)
        {
            blockerImage.SetActive(!active);
            if (active)
            {
                piecesPanel.DOAnchorPosY(176f, 1f).SetEase(Ease.InElastic);
            }
            else
            {
                piecesPanel.DOAnchorPosY(-500f, 1f).SetEase(Ease.InElastic);
            }
        }

        [ClientRpc]
        private void RpcPlayShuffleAnimation()
        {
            StartCoroutine(PlayPieceAnimation());
        }

        private IEnumerator PlayPieceAnimation()
        {
            List<SpriteRenderer> animatedPieces = new List<SpriteRenderer>();

            for (int i = 0; i < pieceContainer.sharedPieces.Count; i++)
            {
                var piece = pieceContainer.sharedPieces[i];
                var animatedPiece = Instantiate(animationPrefab, pieceTriggers[i].transform.position,
                    pieceTriggers[i].transform.rotation);
                animatedPiece.sprite = piece;
                animatedPieces.Add(animatedPiece);
            }

            yield return new WaitForSeconds(2f);

            Sequence sequence = DOTween.Sequence();

            foreach (SpriteRenderer animatedPiece in animatedPieces)
            {
                Vector2 pos = animatedPiece.transform.position;
                pos += Random.insideUnitCircle;
                
                sequence.Append(animatedPiece.transform.DOMove(pos, 0.1f));
                sequence.Append(animatedPiece.transform.DOShakeScale(0.1f, 0.5f));
            }
            
            foreach (SpriteRenderer animatedPiece in animatedPieces)
            {
                sequence.Append(
                    animatedPiece.transform.DOMove(Camera.main.ScreenToWorldPoint(piecesPanel.transform.position),
                        0.1f).OnComplete(() =>
                    {
                        PieceAnimationCompleted?.Invoke();
                    }));
            }
            
            sequence.Play().OnComplete(()=>
            {
                foreach (SpriteRenderer animatedPiece in animatedPieces)
                {
                    Destroy(animatedPiece.gameObject);
                }
                CmdAnimationEnded();
            });

        }


        public override void OnStartClient()
        {
            base.OnStartClient();
            pieceContainer.sharedVfx = correctVfx;
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
            pieceContainer.sharedPieces?.Clear();
            pieceContainer.sharedPieces = SpriteDividerHelper.DivideSprite(demoTexture);
            onSpritesLoaded?.Invoke(pieceContainer.sharedPieces);
            pieceContainer.pieceTriggers.Clear();
            pieceContainer.pieceTriggers = pieceTriggers;
        }

        [Command(requiresAuthority = false)]
        public void CmdSpawnPuzzleAtSpawnPoint(int index , Vector2 spawnPosition)
        {
            ServerDestroyLastPiece();
            PuzzlePieceGameplay piece = Instantiate(piecePrefab, spawnPosition, Quaternion.identity);
            NetworkServer.Spawn(piece.gameObject);
            _lastPiece = piece;
            piece.ServerPiecePlaced += OnPlayerPiecePlaced;
            piece.RpcSetSprite(index);
            piece.netIdentity.AssignClientAuthority(GetCurrentTurnPlayer().connectionToClient);
        }

        [Server]
        private void ServerDestroyLastPiece()
        {
            if (_lastPiece && !_lastPiece.IsPlaced) NetworkServer.Destroy(_lastPiece.gameObject);
        }

        [Server]
        private void OnPlayerPiecePlaced(bool isCorrect, int pieceIndex)
        {
            if (isCorrect) CorrectPiecePlaced?.Invoke(pieceIndex);
            GetCurrentTurnPlayer().ServerUpdatePlayerOnPiecePlaced(isCorrect);
        }

        [Server]
        private PuzzlePlayer GetCurrentTurnPlayer()
        {
            return _puzzlePlayers[_currentPlayerIndex % _puzzlePlayers.Count];
        }
        
        
        [Server]
        private void ServerStartPlayerTurn()
        {
            if (_firstTurn)
            {
                RpcPlayShuffleAnimation();
                for (int i = 0; i < _puzzlePlayers.Count; i++)
                {
                    if (i == _currentPlayerIndex) continue;
                    _puzzlePlayers[i].RpcSetRightPlayer();
                }
                _firstTurn = false;
            }
            else
            {
                GetCurrentTurnPlayer().RpcSetPlayerReady();
                TargetSetTurnUI(GetCurrentTurnPlayer().connectionToClient, true);

                for (int i = 0; i < _puzzlePlayers.Count; i++)
                {
                    if (_puzzlePlayers[i] != GetCurrentTurnPlayer())
                    {
                        _puzzlePlayers[i].RpcSetPlayerIdle();
                        TargetSetTurnUI(_puzzlePlayers[i].connectionToClient, false);
                    }
                }
                
            }
        }
        
        [Server]
        private void OnPlayerConnected(PuzzleNetworkManager.PlayerConnectionInfo obj)
        {
            _puzzlePlayers ??= new List<PuzzlePlayer>();

            if (obj.conn.identity.TryGetComponent(out PuzzlePlayer p))
            {
                _puzzlePlayers.Add(p);
                p.ServerTurnEnded += ServerPlayerTurnEnded;
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
            TargetSetTurnUI(GetCurrentTurnPlayer().connectionToClient, false);
            _currentPlayerIndex++;
            ServerStartPlayerTurn();
        }

        [Command(requiresAuthority = false)]
        private void CmdAnimationEnded()
        {
            _animationEndedCount++;
            if (_animationEndedCount >= maxPlayerCount)
            {
                ServerStartPlayerTurn();
            }
        }

    }
}
