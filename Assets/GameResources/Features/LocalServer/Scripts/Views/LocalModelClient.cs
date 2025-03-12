namespace GameResources.Features.LocalServer.Scripts.Views
{
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using GameResources.Services.Scripts;
    using SystemNotification.Scripts;
    using SystemNotification.Scripts.Interfaces;
    using UnityEngine;
    using UnityEngine.Networking;
    using Zenject;
    using Debug = UnityEngine.Debug;

    public class LocalModelClient : MonoInstaller, ISystemNotification, IService
    {
        [Inject]
        protected virtual void Construct(SystemMessageService _systemMessageService)
        {
            systemMessageService = _systemMessageService;
            systemMessageService.RegisterMessage(this);
        }
        
        protected const string SERVER_POST = "POST";
        protected const string SEND_ERROR = "The request is already being processed";

        public event Action<string> onAnswerReceived = delegate { };
        public event Action<string> onMessage = delegate { };
        
        [SerializeField]
        protected string serverURL = @"http://127.0.0.1:5000/generate";
        
        protected string HeaderName = "Content-Type";
        protected string HeaderParams = "application/json; charset=utf-8";

        protected SystemMessageService systemMessageService = default;
        protected Coroutine sendCoroutine = null;
        protected UnityWebRequest request = default;
        protected PromptData data = default;
        protected string jsonPayload = default;
        protected byte[] bodyRaw = default;

        public override void InstallBindings()
        {
            Container.Bind<IService>().To<LocalModelClient>().FromComponentOn(gameObject).AsTransient();
            Container.Bind<LocalModelClient>().FromInstance(this);
        }

        public virtual async Task<bool> TryRegister() => true;

        public virtual bool TrySendRequest(string prompt)
        {
            if (sendCoroutine == null)
            {
                sendCoroutine = StartCoroutine(SendRequest(prompt));
                return true;
            }
            else
            {
                onMessage(SEND_ERROR);
                return false;
            }
        }
        
        protected virtual IEnumerator SendRequest(string prompt)
        {
            data = new PromptData { Prompt = prompt };
            jsonPayload = JsonUtility.ToJson(data);
            bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

            request = new UnityWebRequest(serverURL, SERVER_POST);
            using (request)
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader(HeaderName, HeaderParams);

                yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
                if (request.isNetworkError || request.isHttpError)
#endif
                {
                    Debug.LogError("Ошибка запроса: " + request.error);
                }
                else
                {
                    Debug.Log("Ответ модели: " + request.downloadHandler.text);
                    onAnswerReceived(request.downloadHandler.text);
                }
            }

            sendCoroutine = null;
        }
    }
}