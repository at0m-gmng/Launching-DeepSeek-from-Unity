namespace GameResources.Features.LocalServer.Scripts.Views
{
    using UnityEngine;
    using UnityEngine.UI;
    using Zenject;

    public class ModelRequestView : MonoBehaviour
    {
        [Inject]
        protected virtual void Construct(LocalModelClient _localModelClient)
        {
            localModelClient = _localModelClient;
        }
        
        [SerializeField]
        protected InputField inputField = default;

        protected LocalModelClient localModelClient = default;
        
        protected virtual void Start() => inputField.onEndEdit.AddListener(SendRequest);

        protected virtual void SendRequest(string value) => localModelClient.TrySendRequest(inputField.text);

        protected virtual void OnDestroy() => inputField.onEndEdit.RemoveListener(SendRequest);
    }
}
