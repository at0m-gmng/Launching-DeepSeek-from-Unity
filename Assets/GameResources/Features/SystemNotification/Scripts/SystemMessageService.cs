namespace GameResources.Features.SystemNotification.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Interfaces;
    using Services.Scripts;
    using UnityEngine;
    using Zenject;
    
    public class SystemMessageService : MonoInstaller, IService
    {
        public event Action<ISystemNotification> onMessageAdded = delegate {};
        public event Action<ISystemNotification> onMessageRemoved = delegate {};

        public IReadOnlyList<ISystemNotification> Messages => messages;

        protected List<ISystemNotification> messages = new List<ISystemNotification>();
        
        public override void InstallBindings()
        {
            Container.Bind<IService>().To<SystemMessageService>().FromComponentOn(gameObject).AsTransient();
            Container.Bind<SystemMessageService>().FromInstance(this);
        }

        public void RegisterMessage(ISystemNotification message)
        {
            if (!messages.Contains(message))
            {
                messages.Add(message);
                onMessageAdded?.Invoke(message);
                // Debug.LogError($"Register {message.GetType()}");
            }
        }

        public void UnregisterMessage(ISystemNotification message)
        {
            if (messages.Remove(message))
            {
                onMessageRemoved?.Invoke(message);
                // Debug.LogError($"Removed {message.GetType()}");
            }
        }

        public async Task<bool> TryRegister() => true;
    }
}