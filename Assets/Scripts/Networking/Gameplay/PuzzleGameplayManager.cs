using System;
using System.Collections;
using System.Collections.Generic;
using Client;
using DG.Tweening;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Networking.Gameplay
{
    public class PuzzleGameplayManager : NetworkBehaviour
    {

        [SerializeField] private PuzzlePieceGameplay piecePrefab;
        [SerializeField] private SpriteRenderer animationPrefab;
        [SerializeField] private RectTransform piecesPanel;
        [SerializeField] private GameObject blockerImage;
        [SerializeField] PuzzlePiecesContainer pieceContainer;
        [SerializeField] AvatarDataContainer dataContainer;
        [SerializeField] List<PieceTrigger> pieceTriggers;
        [SerializeField] private ParticleSystem correctVfx;
        [SerializeField] MessageBanner messageBanner;

        public int maxPlayerCount;
        public int totalRounds;
        public int totalCorrectPieces;
        public Action PieceAnimationCompleted;
        public Action<int> CorrectPiecePlaced;
        public Action GameReset;
        public Action<bool> PuzzleSelectionStarted;
        public Action<string> ClientPuzzleSelected;

        
        private PuzzlePieceGameplay _lastPiece;
        private List<PuzzlePlayer> _puzzlePlayers;
        private int _currentPlayerIndex;
        private bool _firstTurn = true;
        private int _animationEndedCount;
        [SyncVar]
        private int _currRoundNo = 1;
        private int _noOfCorrectPieces;
        private List<PuzzlePieceGameplay> _currPieces;
        private bool _isPuzzleSelected;
        private Coroutine _lastTurnRoutine;
        private int _lastPuzzleSelectorIndex;
        private int _currentPuzzleSelectorIndex;

        private void OnEnable()
        {
            PuzzleNetworkManager.OnPlayerConnected += OnPlayerConnected;
        }

        private void OnDisable()
        {
            PuzzleNetworkManager.OnPlayerConnected -= OnPlayerConnected;
        }


        [TargetRpc]
        private void TargetSetTurnUI(NetworkConnectionToClient target, bool active , bool showRoundNumber)
        {
            if (showRoundNumber)
            {
                messageBanner.Show("Round : " + _currRoundNo);
                return;
            }
            blockerImage.SetActive(!active);
            if (active)
            {
                messageBanner.Show("YOUR TURN");
                piecesPanel.DOAnchorPosY(176f, 1f).SetEase(Ease.InElastic);
            }
            else
            {
                messageBanner.Show("OPPONENT'S TURN");
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
                animatedPieces.Clear();
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
        public void GetPuzzleSprites(Sprite selectedSprite,Action<List<Sprite>> onSpritesLoaded)
        {
            pieceContainer.sharedPieces?.Clear();
            pieceContainer.sharedPieces = SpriteDividerHelper.DivideSprite(selectedSprite);
            onSpritesLoaded?.Invoke(pieceContainer.sharedPieces);
            pieceContainer.pieceTriggers = pieceTriggers;
        }
        
        [ClientRpc]
        private void RpcSelectPuzzle(string key)
        {
            ClientPuzzleSelected?.Invoke(key);
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
            if (isCorrect)
            {
                _noOfCorrectPieces++;
                CorrectPiecePlaced?.Invoke(pieceIndex);
                _currPieces ??= new List<PuzzlePieceGameplay>();
                _currPieces.Add(_lastPiece);
                bool allPiecesPlaced = _noOfCorrectPieces >= totalCorrectPieces;
                if (allPiecesPlaced)
                {
                    ServerResetGame();
                    return;
                }
            }
            GetCurrentTurnPlayer().ServerUpdatePlayerOnPiecePlaced(isCorrect);
        }

        [Server]
        private void ServerResetGame()
        {
            _currRoundNo++;
            _currentPuzzleSelectorIndex++;
            foreach (PuzzlePieceGameplay pieceGameplay in _currPieces)
            {
                NetworkServer.Destroy(pieceGameplay.gameObject);
            }

            foreach (var player in _puzzlePlayers)
            {
                player.ServerResetPlayer();
            }
            _currPieces.Clear();
            _noOfCorrectPieces = 0;
            _currentPlayerIndex = 0;
            _isPuzzleSelected = false;
            _firstTurn = true;
            _animationEndedCount = 0;
            if (_lastTurnRoutine != null) StopCoroutine(_lastTurnRoutine);
            GameReset?.Invoke();
            
            StartCoroutine(SetRoundNoDelayed());
        }

        [Server]
        private IEnumerator SetRoundNoDelayed()
        {
            foreach (PuzzlePlayer player in _puzzlePlayers)
            {
                TargetSetTurnUI(player.connectionToClient , true , true);
            }

            yield return new WaitForSeconds(2f);
            ServerStartTurn();
        }

        [Server]
        private PuzzlePlayer GetCurrentTurnPlayer()
        {
            int index = _currentPlayerIndex % _puzzlePlayers.Count;
            
            return _puzzlePlayers[index];
        }
        
        [Server]
        private PuzzlePlayer GetCurrentPuzzleSelector()
        {
            int index = _currentPuzzleSelectorIndex % _puzzlePlayers.Count;
            return _puzzlePlayers[index];
        }
        
        [Server]
        private void ServerStartTurn()
        {
            _lastTurnRoutine = StartCoroutine(ServerStartPlayerTurn());

            if (!_isPuzzleSelected)
            {
                foreach (PuzzlePlayer puzzlePlayer in _puzzlePlayers)
                {
                    TargetSetPuzzleSelection(puzzlePlayer.connectionToClient,
                        puzzlePlayer == GetCurrentPuzzleSelector());
                }
            }
        }

        [TargetRpc]
        private void TargetSetPuzzleSelection(NetworkConnectionToClient target , bool isSelecting)
        {
            PuzzleSelectionStarted?.Invoke(isSelecting);
        }

        [Command(requiresAuthority = false)]
        public void CmdSelectPuzzle(string key)
        {
            _isPuzzleSelected = true;
            RpcSelectPuzzle(key);
        }
        
        
        [Server]
        private IEnumerator ServerStartPlayerTurn()
        {
            yield return new WaitUntil(() => _isPuzzleSelected);
            if (_firstTurn)
            {
                RpcPlayShuffleAnimation();
                for (int i = 0; i < _puzzlePlayers.Count; i++)
                {
                    _puzzlePlayers[i].RpcSetPlayerDirection();
                    _puzzlePlayers[i].ServerUpdateScoreText();
                    _puzzlePlayers[i].ServerUpdateTurnsText();
                }
                _firstTurn = false;
            }
            else
            {
                GetCurrentTurnPlayer().RpcSetPlayerReady();
                GetCurrentTurnPlayer().ServerStartTimer();
                GetCurrentTurnPlayer().ServerUpdateTurnsText();
                TargetSetTurnUI(GetCurrentTurnPlayer().connectionToClient, true , false);

                for (int i = 0; i < _puzzlePlayers.Count; i++)
                {
                    if (_puzzlePlayers[i] != GetCurrentTurnPlayer())
                    {
                        _puzzlePlayers[i].RpcSetPlayerIdle();
                        TargetSetTurnUI(_puzzlePlayers[i].connectionToClient, false , false);
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
                ServerStartTurn();
            }
        }
        
        [Server]
        private void ServerPlayerTurnEnded()
        {
            _currentPlayerIndex++;
            ServerDestroyLastPiece();
            ServerStartTurn();
        }

        [Command(requiresAuthority = false)]
        private void CmdAnimationEnded()
        {
            _animationEndedCount++;
            if (_animationEndedCount >= maxPlayerCount)
            {
                ServerStartTurn();
            }
        }

    }
    
}
