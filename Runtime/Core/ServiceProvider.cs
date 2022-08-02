using GameWarriors.DependencyInjection.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GameWarriors.DependencyInjection.Core
{
    public class ServiceProvider : IServiceProvider
    {
        private readonly ServiceLocator _singletonLocator;
        private Dictionary<Type, TransientServiceItem> _transienTable;

        public IDictionary<Type, TransientServiceItem> TransientTable => _transienTable;

        public ServiceProvider(int size = 25)
        {
            _singletonLocator = new ServiceLocator(size);
            _transienTable = new Dictionary<Type, TransientServiceItem>(size);
        }

        public void SetSingletonService(Type injectType, object serviceObject)
        {
            _singletonLocator.Register(injectType, serviceObject);
        }

        public void SetTransientService(Type injectType,Type mainType)
        {
            _transienTable.Add(injectType, new TransientServiceItem(mainType));
        }

        public object GetService(Type serviceType)
        {
            object service = _singletonLocator.Resolve(serviceType);
            if (service == null)
            {
                if (_transienTable.TryGetValue(serviceType, out var item))
                {
                    service = item.CreateInstance(this);
                    item.SetProperties(service, this);
                }

            }
            return service;
        }
    }
}