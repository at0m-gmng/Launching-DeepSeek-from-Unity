namespace GameResources.Features.UI.Scripts
{
    using System;
    using System.Collections.Generic;
    using DG.Tweening;
    using UnityEngine.UI;

    public class SimpleTextSequenceAnimator
    {
        public SimpleTextSequenceAnimator(Text _text, float _timeShowingText = 0f, float _delayText = 0f)
        {
            text = _text;
            timeShowingText = _timeShowingText;
            delayText = _delayText; 
        }
        
        public bool IsFullCleaningField = true;
        public Tween CurrentTween { get; protected set; } = null;

        protected Text text = default;
        protected float timeShowingText = default;
        protected float delayText = default;

        protected Queue<string> textQueue = new Queue<string>();
        protected Sequence sequence = null;
        protected string nextText = default;
        
        public virtual void EnqueueTextAnimation(string newText)
        {
            textQueue.Enqueue(newText);
            if (CurrentTween == null || !CurrentTween.IsActive())
            {
                PlayNext();
            }
        }

        protected virtual void PlayNext()
        {
            if (textQueue.Count != 0)
            {
                nextText = textQueue.Dequeue();
                
                sequence = DOTween.Sequence();
                sequence.Append(text.DOText(nextText, timeShowingText))
                    .AppendInterval(delayText)
                    .OnComplete(RePlayNext);

                CurrentTween = sequence;
            }
        }

        protected virtual void RePlayNext()
        {
            CurrentTween = null;
            if (IsFullCleaningField)
            {
                text.text = String.Empty;   
            }
            PlayNext();
        }
    }
}