using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Mirror;
using UnityEngine;

namespace Networking.Gameplay
{
    public class PuzzlePieceGameplay : NetworkBehaviour
    {
        [SerializeField] private SpriteRenderer pieceRenderer;
        [SerializeField] private PuzzlePiecesContainer piecesContainer;
        [SerializeField] private GameObject tickSprite;
        [SerializeField] private GameObject crossSprite;
        
        private List<PieceTrigger> _nearTriggers;
        private Vector3 _initialPosition;
        private Coroutine _lastRoutine;
        
        public float yOffset;
        public float minDistance;

        private bool _isDragging;
        private Camera _camera;
        private PieceTrigger _lastTrigger;
        private int _myIndex;
        
        [SyncVar]
        private bool _isPlaced;

        private bool _allowDrag = true;
        public Action<bool, int> ServerPiecePlaced;

        public bool IsPlaced => _isPlaced;

        private void Start()
        {
            _camera = Camera.main;
            
        }

        [ClientRpc]
        public void RpcSetSprite(int index)
        {
            Vector2 newPos = transform.position;
            newPos.y += 1f;
            transform.DOMoveY(newPos.y, 1f).SetEase(Ease.OutElastic);
            _initialPosition = newPos;
            pieceRenderer.sprite = piecesContainer.sharedPieces[index];
            _myIndex = index;
        }
        
        void Update()
        {
            if (_isPlaced || !isOwned) return;
            if (Input.GetMouseButtonDown(0) && _allowDrag)
            {
                RaycastHit2D hit = Physics2D.Raycast(_camera.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    _isDragging = true;
                    if (_lastRoutine != null)
                    {
                        StopCoroutine(_lastRoutine);
                        _lastRoutine = null;
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
                _allowDrag = false;

                if (_lastTrigger != null && PieceInRange())
                {
                    if (IsCorrectPiece())
                    {
                        CmdSetIsPlaced(true, _lastTrigger.pieceIndex);
                        _lastRoutine = StartCoroutine(SmoothMoveTo(_lastTrigger.transform.position, 10f));
                    }
                    else
                    {
                        CmdWrongPiecePlaced(_lastTrigger.pieceIndex);
                        _lastRoutine = StartCoroutine(SmoothMoveTo(_initialPosition, 5f));
                    }
                }
                else
                {
                    _lastRoutine = StartCoroutine(SmoothMoveTo(_initialPosition, 5f));
                }
            }
            if (_isDragging)
            {
                MovePieceOnDrag();
                CheckPieceInRange();
            }

        }

        private void CheckPieceInRange()
        {
            PieceTrigger closestTrigger = null;
            float closestDistance = float.MaxValue;

            foreach (PieceTrigger pieceTrigger in piecesContainer.pieceTriggers)
            {
                float distance = Vector2.Distance(pieceTrigger.transform.position, transform.position);

                if (distance < minDistance && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTrigger = pieceTrigger;
                }
            }

            foreach (PieceTrigger pieceTrigger in piecesContainer.pieceTriggers)
            {
                pieceTrigger.SetInRange(pieceTrigger == closestTrigger);
            }

            _lastTrigger = closestTrigger;
        }


        private void MovePieceOnDrag()
        {
            Vector3 mousePosition = _camera.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.y += yOffset;
            mousePosition.z = transform.position.z;
            transform.position = mousePosition;
        }

        [Command]
        private void CmdSetIsPlaced(bool isPlaced , int index)
        {
            _isPlaced = isPlaced;
            ServerPiecePlaced?.Invoke(true , index);
            RpcPlaySuccessEffect();
        }

        [Command]
        private void CmdWrongPiecePlaced(int index)
        {
            ServerPiecePlaced?.Invoke(false , index);
            RpcPlayFailEffect();
        }

        private bool PieceInRange()
        {
            return _lastTrigger != null && _lastTrigger.inRange;
        }

        private bool IsCorrectPiece()
        {
            return _lastTrigger != null && _lastTrigger.pieceIndex == _myIndex;
        }

        private IEnumerator SmoothMoveTo(Vector3 targetPosition , float moveSpeed , float delay = 0f)
        {
            yield return new WaitForSeconds(delay);
            float elapsedTime = 0f;
            Vector3 startPosition = transform.position;

            float duration = 1.0f / moveSpeed;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            transform.position = targetPosition;
            _allowDrag = true;
        }

        [ClientRpc]
        private void RpcPlaySuccessEffect()
        {
            if (piecesContainer.sharedVfx != null)
            {
                piecesContainer.sharedVfx.transform.position = transform.position;
                piecesContainer.sharedVfx.Play();
            }

            tickSprite.SetActive(true);
            Sequence sequence = DOTween.Sequence();

            sequence.Append(tickSprite.transform.DOScale(new Vector3(2.4f, 2.4f, 2.4f), 1f).SetEase(Ease.OutElastic));
            sequence.Append(tickSprite.transform.DOScale(new Vector3(0.1f, 0.1f, 0.1f), 1f).SetEase(Ease.InElastic));

            sequence.Play().OnComplete(() =>
            {
                tickSprite.SetActive(false);
            });
            pieceRenderer.sortingOrder = 2;
        }

        [ClientRpc]
        private void RpcPlayFailEffect()
        {
            crossSprite.SetActive(true);
            Sequence sequence = DOTween.Sequence();

            sequence.Append(crossSprite.transform.DOScale(new Vector3(2.4f, 2.4f, 2.4f), 1f).SetEase(Ease.OutElastic));
            sequence.Append(crossSprite.transform.DOScale(new Vector3(0.1f, 0.1f, 0.1f), 1f).SetEase(Ease.InElastic));
            
            sequence.Play().OnComplete(() =>
            {
                crossSprite.SetActive(false);
            });
        }

        private void OnDisable()
        {
            tickSprite.transform.DOKill();
            crossSprite.transform.DOKill();
            transform.DOKill();
        }
    }
}
