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
            avatarSprite.flipX = false;
        }
    }
}