using System;

namespace GameWrriors.DependencyInjection.Core
{
    public class ServiceProvider : IServiceProvider
    {
        public void SetService(Type injectType, object serviceObject)
        {
            ServiceLocator.Register(injectType, serviceObject);
        }

        public object GetService(Type serviceType)
        {
            return ServiceLocator.Resolve(serviceType);
        }
    }
}