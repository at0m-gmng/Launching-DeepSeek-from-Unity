namespace GameResources.Features.SystemNotification.Scripts.Interfaces
{
    using System;

    public interface IProgressSystemNotification : ISystemNotification
    {
        public event Action<string, float> onMessageProgress;
    }
}