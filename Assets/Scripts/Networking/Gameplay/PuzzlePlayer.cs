using System;
using System.Collections;
using System.Collections.Generic;
using Client;
using DG.Tweening;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Networking.Gameplay
{
    public class PuzzlePlayer : NetworkBehaviour
    {
        public int totalTurns;
        public int totalTurnTime;
        public int correctPlaceScore;
        public Action ServerTurnEnded;
        
        private int _currTurnCount;
        private int _currScore;
        private PuzzlePieceGameplay _currSelectedPiece;
        private PlayerSide _currDirection;
        private PlayerCanvas _currCanvas;

        [SerializeField] private SpriteRenderer avatarSprite;
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private List<PlayerCanvas> playerCanvases;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private NetworkTimer networkTimer;
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private AvatarDataContainer dataContainer;

        [SyncVar(hook = nameof(UpdateItems))]
        private ProfileManager.PlayerItems _syncedItems; 
        
        public int CurrScore => _currScore;

        public override void OnStartServer()
        {
            _currTurnCount = totalTurns;
            networkTimer.OnTimerTick += NetworkTimerOnOnTimerTick;
            networkTimer.OnTimerCompleted += ServerEndPlayerTurn;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        }

        public override void OnStopServer()
        {
            networkTimer.OnTimerTick -= NetworkTimerOnOnTimerTick;
            networkTimer.OnTimerCompleted -= ServerEndPlayerTurn;
        }

        [Command]
        private void CmdSetUserProperties(ProfileManager.PlayerItems items)
        {
            _syncedItems = items;
        }

        private void UpdateItems(ProfileManager.PlayerItems old, ProfileManager.PlayerItems newValue)
        {
            avatarSprite.sprite = dataContainer.avatarDataList.Find(a => a.avatarKey == newValue.selectedAvatar)
                .avatarSprite;
            _currCanvas.playerName.text = newValue.userName;
        }

        private void NetworkTimerOnOnTimerTick(double obj)
        {
            RpcUpdateTimer(obj);
        }

        [Server]
        public void ServerUpdatePlayerOnPiecePlaced(bool isCorrect)
        {
            ServerUpdateTurnCount();
            if (isCorrect)
            {
                ServerUpdateScore();
            }
        }

        [Server]
        private void ServerUpdateTurnCount()
        {
            _currTurnCount--;
            ServerUpdateTurnsText();
            ServerCheckTurnCount();
        }

        [Server]
        private void ServerCheckTurnCount()
        {
            if (_currTurnCount <= 0)
            {
                ServerEndPlayerTurn();
            }
        }

        [Server]
        private void ServerEndPlayerTurn()
        {
            _currTurnCount = totalTurns;
            networkTimer.ResetTimer();
            ServerTurnEnded?.Invoke();
        }

        [Server]
        private void ServerUpdateScore()
        {
            _currScore += correctPlaceScore;
            ServerUpdateScoreText();
            RpcSetPlayerPunch();
        }

        [Server]
        public void ServerUpdateScoreText()
        {
            RpcUpdateScoreText(_currScore);
        }
        
        [Server]
        public void ServerUpdateTurnsText()
        {
            RpcUpdateTurnsText(_currTurnCount);
        }

        [Server]
        public void ServerStartTimer()
        {
            networkTimer.StartTimer(totalTurnTime);
        }

        [ClientRpc]
        private void RpcUpdateTurnsText(int currTurnCount)
        {
            _currCanvas.turnText.text = "TURNS LEFT : " + currTurnCount;
        }
        
        [ClientRpc]
        private void RpcUpdateScoreText(int score)
        {
            _currCanvas.scoreText.text = "SCORE : " + score;
        }

        [ClientRpc]
        public void RpcSetPlayerDirection()
        {
            _currDirection = transform.position.x < 0 ? PlayerSide.Left : PlayerSide.Right;
            SetPlayerCanvas();
            CmdSetUserProperties(ProfileManager.LocalPlayerItems);
        }
        
        [ClientRpc]
        private void RpcUpdateTimer(double obj)
        {
            _currCanvas.SetTimerFillAmount((float)obj / totalTurnTime);
        }

        [ClientRpc]
        public void RpcSetParent(NetworkIdentity parentId)
        {
            transform.parent = parentId.transform;
        }
        
        private void SetPlayerCanvas()
        {
            _currCanvas = playerCanvases.Find(c => c.side == _currDirection);
            
            if(_currCanvas == null) return;
            _currCanvas.mainObject.SetActive(true);
            Vector3 scale = transform.localScale;
            scale.x = _currCanvas.localScaleX;
            transform.localScale = scale;
        }

        [ClientRpc]
        public void RpcSetPlayerIdle()
        {
            playerAnimator.SetTrigger("BoxingIdle");
            _currCanvas.MoveOut();
        }

        [ClientRpc]
        public void RpcSetPlayerReady()
        {
            playerAnimator.ResetTrigger("BoxingHit");
            playerAnimator.SetTrigger("BoxingIdleReady");
            _currCanvas.MoveIn();
        }

        [ClientRpc]
        private void RpcSetPlayerPunchLight()
        {
            playerAnimator.SetTrigger("BoxingHitLight");
        }

        [ClientRpc]
        private void RpcSetPlayerPunch()
        {
            playerAnimator.SetTrigger("BoxingHit");
        }

        public void PlayHitEffect()
        {
            hitEffect.Play();
            CameraShake.instance.Shake();
        }

        [Server]
        public void ServerResetPlayer()
        {
            networkTimer.ResetTimer();
            RpcResetPlayer();
        }

        [ClientRpc]
        private void RpcResetPlayer()
        {
            playerAnimator.ResetTrigger("BoxingHit");
            _currTurnCount = totalTurns;
            _currCanvas.MoveOut();
        }

        [ClientRpc]
        public void RpcPlaySuccessAnimation()
        {
            playerAnimator.Play("FinalSuccess");
        }

       

        [ClientRpc]
        public void RpcPlayFailAnimation()
        {
            playerAnimator.Play("FinalFail");
        }
        
        

        public enum PlayerSide
        {
            Left,
            Right
        }

        [Serializable]
        public class PlayerCanvas
        {
            public PlayerSide side;
            public GameObject mainObject;
            public RectTransform panelRect;
            public Image bgImage;
            public TextMeshProUGUI turnText;
            public TextMeshProUGUI scoreText;
            public TextMeshProUGUI playerName;
            public Image fillImage;
            public float localScaleX;
            public float moveInX;
            public float moveOutX;
            public float moveDelay;

            public void MoveIn()
            {
                StopAlert();
                panelRect.DOAnchorPosX(moveInX, moveDelay).SetEase(Ease.InOutQuad);
            }

            private void StopAlert()
            {
                panelRect.DOKill();
                panelRect.transform.localScale = Vector3.one;
                bgImage.color = Color.white;
            }

            public void MoveOut()
            {
                panelRect.DOAnchorPosX(moveOutX, moveDelay).SetEase(Ease.InOutQuad);
            }

            public void StartAlert()
            {
                panelRect.DOScale(new Vector2(1.1f, 1.1f), 1f).SetEase(Ease.InOutQuad).SetLoops(-1, LoopType.Yoyo);
                bgImage.DOColor(Color.red, 1f).SetLoops(-1, LoopType.Yoyo);
            }

            public void SetTimerFillAmount(float amount)
            {
                if (Mathf.Approximately(amount, 0.2f))
                {
                    StartAlert();
                }
                fillImage.fillAmount = amount;
            }
        }
    }
}