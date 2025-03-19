namespace GameResources.Features.SystemNotification.Scripts.Views
{
    using System.Collections;
    using DG.Tweening;
    using Interfaces;
    using UI.Scripts;
    using UnityEngine;
    using UnityEngine.UI;

    public class ProgressSystemNotificationView : SystemNotificationView
    {
        [SerializeField] 
        protected Slider progressSlider = default;
        
        [SerializeField] 
        protected Text progressText = default;

        [SerializeField]
        protected float timeShowingText = 1f;
        
        [SerializeField]
        protected float timeDelay = 1f;

        protected SimpleTextSequenceAnimator simpleTextSequenceAnimator = default;

        protected override void Start()
        {
            simpleTextSequenceAnimator = new SimpleTextSequenceAnimator(progressText, _timeShowingText: timeShowingText, _delayText: timeDelay);
            base.Start();
        }

        protected override void SubscribeToMessage(ISystemNotification message)
        {
            if (!subscribedMessages.Contains(message))
            {
                if (message is IProgressSystemNotification messageProgress)
                {
                    messageProgress.onMessageProgress += OnMessageUpdate;
                }
                else
                {
                    message.onMessage += OnMessageUpdate;
                }
                subscribedMessages.Add(message);
            }
        }

        protected override void UnsubscribeFromMessage(ISystemNotification message)
        {
            if (subscribedMessages.Contains(message))
            {
                if (message is IProgressSystemNotification messageProgress)
                {
                    messageProgress.onMessageProgress -= OnMessageUpdate;
                }
                else
                {
                    message.onMessage -= OnMessageUpdate;
                }
            }
        }

        protected virtual void OnMessageUpdate(string newText, float progress)
        {
            OnMessageUpdate(newText);
            StartCoroutine(WaitWhileEndAnimation(progress));
        }
        
        protected virtual IEnumerator WaitWhileEndAnimation(float progress)
        {
            yield return null;
            UpdateSliderValue(progress);
        }

        protected virtual void UpdateSliderValue(float progress)
        {
            if (progressSlider != null)
            {
                if (progress > progressSlider.value)
                {
                    progressSlider.DOValue(progress, 1f);
                }
                else
                {
                    progressSlider.value = progress;
                }
            }
        }

        protected override void OnMessageUpdate(string newText)
        {
            if (!simpleTextSequenceAnimator.IsContains(newText))
            {
                simpleTextSequenceAnimator.IsFullCleaningField = true;
                simpleTextSequenceAnimator.EnqueueTextAnimation(newText);
            }
        }
    }
}