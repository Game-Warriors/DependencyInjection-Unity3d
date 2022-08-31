using GameWarriors.DependencyInjection.Abstraction;
using System;
using System.Collections.Generic;

namespace GameWarriors.DependencyInjection.Core
{
    public class ServiceProvider : IServiceProvider
    {
        private readonly ServiceLocator _singletonLocator;
        private Dictionary<Type, IObjectFactory> _transienTable;

        public IDictionary<Type, IObjectFactory> TransientTable => _transienTable;

        public ServiceProvider(int size = 25)
        {
            _singletonLocator = new ServiceLocator(size);
            _transienTable = new Dictionary<Type, IObjectFactory>(size);
        }

        public void SetSingletonService(Type injectType, object serviceObject)
        {
            _singletonLocator.Register(injectType, serviceObject);
        }

        public ITransientServiceItem SetTransientService(Type injectType, Type mainType)
        {
            TransientServiceItem item = new TransientServiceItem(mainType);
            _transienTable.Add(injectType, item);
            return item;
        }

        public void SetTransientService(Type injectType, IObjectFactory ServiceItem)
        {
            _transienTable.Add(injectType, ServiceItem);
        }

        public object GetService(Type serviceType)
        {
            object service = _singletonLocator.Resolve(serviceType);
            if (service == null)
            {
                if (_transienTable.TryGetValue(serviceType, out var item))
                {
                    service = item.CreateObject(this);
                }
            }
            return service;
        }
    }
}