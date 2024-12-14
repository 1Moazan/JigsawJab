using System;
using Mirror;
using TMPro;
using UnityEngine;

namespace Networking.Gameplay
{
    public class PuzzlePlayer : NetworkBehaviour
    {
        public int totalTurns;
        
        private int _currTurnCount;
        private int _currScore;
        
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private SpriteRenderer avatarSprite;
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private ParticleSystem[] hitEffects;

        public Action ServerTurnEnded;
        private PuzzlePieceGameplay _currSelectedPiece;
        public override void OnStartServer()
        {
            base.OnStartServer();
            _currTurnCount = totalTurns;
            RpcUpdateTurnsText(_currTurnCount);
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
            RpcUpdateTurnsText(_currTurnCount);

            if (_currTurnCount <= 0)
            {
                _currTurnCount = totalTurns;
                ServerEndPlayerTurn();
            }
        }

        [Server]
        private void ServerEndPlayerTurn()
        {
            ServerTurnEnded?.Invoke();
        }

        [Server]
        private void ServerUpdateScore()
        {
            _currScore += 20;
            RpcUpdateScoreText(_currScore);
            RpcSetPlayerPunch();
        }

        [ClientRpc]
        private void RpcUpdateTurnsText(int currTurnCount)
        {
            turnText.text = "Turns Left : " + currTurnCount;
        }
        
        [ClientRpc]
        private void RpcUpdateScoreText(int score)
        {
            scoreText.text = "Score : " + score;
        }

        [ClientRpc]
        public void RpcSetRightPlayer()
        {
            Vector3 scale = transform.localScale;
            scale.x = -1;
            transform.localScale = scale;
        }
        
        [ClientRpc]
        public void RpcSetPlayerIdle()
        {
            playerAnimator.SetTrigger("BoxingIdle");
        }

        [ClientRpc]
        public void RpcSetPlayerReady()
        {
            playerAnimator.ResetTrigger("BoxingHit");
            playerAnimator.SetTrigger("BoxingIdleReady");
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
            foreach (ParticleSystem effect in hitEffects)
            {
                effect.Play();
            }
            CameraShake.instance.Shake();
        }
    }
}