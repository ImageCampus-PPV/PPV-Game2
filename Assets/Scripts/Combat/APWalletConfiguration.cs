using UnityEngine;

namespace Assets.Scripts.Combat
{
    [CreateAssetMenu(fileName = "APWalletConfiguration", menuName = "ScriptableObjects/APWalletConfiguration")]
    public class APWalletConfiguration : ScriptableObject
    {
        public int _maxAP = 0;
        public int _APPerTurn = 0;
        public int _startingAP = 0;
    }
}
