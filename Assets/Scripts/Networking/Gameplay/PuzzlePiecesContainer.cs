using System.Collections.Generic;
using UnityEngine;

namespace Networking.Gameplay
{
    [CreateAssetMenu(menuName = "Gameplay/PuzzlePiecesContainer")]
    public class PuzzlePiecesContainer : ScriptableObject
    {
        public List<Sprite> sharedPieces;
        public List<PieceTrigger> pieceTriggers; 
    }
}
