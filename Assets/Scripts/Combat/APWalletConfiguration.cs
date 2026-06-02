using UnityEngine;

namespace Assets.Scripts.Combat
{
    [CreateAssetMenu(fileName = "APWalletConfiguration", menuName = "ScriptableObjects/APWalletConfiguration")]
    public class APWalletConfiguration : ScriptableObject
    {
        public uint _maxAP = 0;
        public uint _APPerTurn = 0;
        public uint _startingAP = 0;
    }
}
