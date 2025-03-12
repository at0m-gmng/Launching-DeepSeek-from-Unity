namespace GameResources.Features.SystemNotification.Scripts.Interfaces
{
    using System;

    public interface ISystemNotification
    {
        public event Action<string> onMessage;
    }
}