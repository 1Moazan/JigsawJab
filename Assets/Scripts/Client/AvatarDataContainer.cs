using System;
using System.Collections.Generic;
using UnityEngine;

namespace Client
{
    [CreateAssetMenu(fileName = "AvatarData", menuName = "Data/AvatarDataContainer", order = 0)]
    public class AvatarDataContainer : ScriptableObject
    {
        public List<AvatarData> avatarDataList;
    }

    [Serializable]
    public class AvatarData
    {
        public string avatarKey;
        public Sprite avatarSprite;
    }
}