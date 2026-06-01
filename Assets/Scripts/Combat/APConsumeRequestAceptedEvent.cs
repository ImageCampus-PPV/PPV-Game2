using ImageCampus.ToolBox.Events;

namespace Assets.Scripts.Combat
{
    public struct APConsumeRequestAceptedEvent : IEvent
    {
        public int _amountConsume;

        public void Assign(params object[] parameters)
        {
            _amountConsume = (int)parameters[0];
        }

        public void Reset()
        {
            _amountConsume = default(int);
        }
    }
}