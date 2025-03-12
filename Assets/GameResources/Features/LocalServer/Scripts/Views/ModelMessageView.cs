namespace GameResources.Features.LocalServer.Scripts.Views
{
    using UnityEngine;
    using UnityEngine.UI;
    using Zenject;

    public class ModelMessageView : MonoBehaviour
    {
        [Inject]
        protected virtual void Construct(LocalModelClient _localModelClient)
        {
            localModelClient = _localModelClient;

            Subscribe();
        }
        
        [SerializeField]
        protected Text text = default;

        protected LocalModelClient localModelClient = default;

        protected virtual void Subscribe() => localModelClient.onAnswerReceived += UpdateView;

        protected virtual void UpdateView(string message) => text.text = message;

        protected virtual void OnDestroy() => localModelClient.onAnswerReceived -= UpdateView;
    }
}
