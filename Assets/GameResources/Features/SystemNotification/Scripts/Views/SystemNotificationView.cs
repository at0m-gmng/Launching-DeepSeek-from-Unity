namespace GameResources.Features.SystemNotification.Scripts.Views
{
    using System.Collections.Generic;
    using Interfaces;
    using UnityEngine;
    using Zenject;

    public abstract class SystemNotificationView : MonoBehaviour
    {
        [Inject]
        public virtual void Construct(SystemMessageService _messageManager)
        {
            messageManager = _messageManager;
        }

        protected SystemMessageService messageManager = default;
        protected HashSet<ISystemNotification> subscribedMessages = new HashSet<ISystemNotification>();
        
        protected virtual void Start()
        {
            messageManager.onMessageAdded += OnMessageAdded;
            messageManager.onMessageRemoved += OnMessageRemoved;

            Debug.LogError($"messageManager.Messages {messageManager.Messages.Count}");
            foreach (ISystemNotification message in messageManager.Messages)
            {
                SubscribeToMessage(message);
            }
        }

        protected virtual void OnDestroy()
        {
            messageManager.onMessageAdded -= OnMessageAdded;
            messageManager.onMessageRemoved -= OnMessageRemoved;

            foreach (ISystemNotification message in subscribedMessages)
            {
                UnsubscribeFromMessage(message);
            }
            subscribedMessages.Clear();
        }

        protected virtual void OnMessageAdded(ISystemNotification message) => SubscribeToMessage(message);

        protected virtual void OnMessageRemoved(ISystemNotification message)
        {
            UnsubscribeFromMessage(message);
            subscribedMessages.Remove(message);
        }

        protected virtual void SubscribeToMessage(ISystemNotification message)
        {
            if (!subscribedMessages.Contains(message))
            {
                message.onMessage += OnMessageUpdate;
                subscribedMessages.Add(message);
            }
        }

        protected virtual void UnsubscribeFromMessage(ISystemNotification message)
        {
            if (subscribedMessages.Contains(message))
            {
                message.onMessage -= OnMessageUpdate;   
            }
        }

        protected abstract void OnMessageUpdate(string newText);
    }
}