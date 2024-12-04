using UnityEngine;

namespace Networking.Gameplay
{
    public class PieceTrigger : MonoBehaviour
    {
       [SerializeField] private GameObject indicatorObject;
       public bool inRange;
       public int pieceIndex;
       public void SetInRange(bool isInRange)
       {
           indicatorObject.SetActive(isInRange);
           inRange = isInRange;
       }
    }
}
