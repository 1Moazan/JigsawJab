using UnityEngine;
using UnityEngine.UI;

namespace Networking.Gameplay
{
    public class PuzzlePieceUI : MonoBehaviour
    {
        public Button mainButton;
        [SerializeField] private Image pieceImage;
        private int _myIndex;
        
        public int PiceIndex => _myIndex;

        public void InitializeUiPiece(int index , Sprite pieceSprite)
        {
            _myIndex = index;
            pieceImage.sprite = pieceSprite;
        }

       
    }
}
