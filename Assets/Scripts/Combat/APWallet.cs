using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using UnityEngine;

namespace Assets.Scripts.Combat
{
    public class APWallet : IService, IDisposable
    {
        EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();
        public bool IsPersistance => false;

        private readonly int MAX_AP = 0;
        private int _currentAP = 0;

        public int CurrentAP => _currentAP;
        public int MaxAP => MAX_AP;

        public APWallet(APWalletConfiguration configuration)
        {
            MAX_AP = configuration._maxAP;
            _currentAP = configuration._startingAP;
        }

        public void Init()
        {
            EventBus.Subscribe<APConsumeRequestAceptedEvent>(OnAPConsume);
        }

        private void OnAPConsume(in APConsumeRequestAceptedEvent apConsumeRequestAceptedEvent)
        {
            _currentAP -= apConsumeRequestAceptedEvent._amountConsume;
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<APConsumeRequestAceptedEvent>(OnAPConsume);
        }
    }

    [CreateAssetMenu(fileName = "APWalletConfiguration", menuName = "ScriptableObjects/APWalletConfiguration")]
    public class APWalletConfiguration : ScriptableObject
    {
        public int _maxAP = 0;
        public int _startingAP = 0;
    }
}
