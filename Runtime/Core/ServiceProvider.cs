using System;

namespace GameWarriors.DependencyInjection.Core
{
    public class ServiceProvider : IServiceProvider
    {
        private readonly ServiceLocator _serviceLocator;

        public ServiceProvider(int size = 25)
        {
            _serviceLocator = new ServiceLocator(size);
        }

        public void SetService(Type injectType, object serviceObject)
        {
            _serviceLocator.Register(injectType, serviceObject);
        }

        public object GetService(Type serviceType)
        {
            return _serviceLocator.Resolve(serviceType);
        }
    }
}