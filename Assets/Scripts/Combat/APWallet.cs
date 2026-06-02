using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;

namespace Assets.Scripts.Combat
{
    public class APWallet : IService
    {
        public bool IsPersistance => false;

        private readonly uint MAX_AP = 0;
        private uint _currentAP = 0;

        public uint CurrentAP => _currentAP;
        public uint MaxAP => MAX_AP;

        public APWallet(APWalletConfiguration configuration)
        {
            MAX_AP = configuration._maxAP;
            _currentAP = configuration._startingAP;
        }

        public void ConsumeAP(uint price)
        {
            _currentAP -= price;
        }

        public void ResetAP()
        {
            _currentAP = MAX_AP;
        }
    }
}
