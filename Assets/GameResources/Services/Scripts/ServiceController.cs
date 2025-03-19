namespace GameResources.Services.Scripts
{
    using System.Collections.Generic;
    using UnityEngine;
    using Zenject;

    public class ServiceController : MonoBehaviour
    {
        [Inject]
        public virtual void Construct(List<IService> _services)
        {
            services.AddRange(_services);
        }

        protected List<IService> services = new List<IService>();

        protected virtual async void Start()
        {
            for (int i = 0; i < services.Count; i++)
            {
                if (!await services[i].TryRegister())
                {
                    Debug.LogError($"Service Not Register {services[i].GetType()}");
                }
            }
        }
    }
}