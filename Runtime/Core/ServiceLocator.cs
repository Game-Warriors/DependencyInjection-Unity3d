using System;
using System.Collections.Generic;

namespace GameWarriors.DependencyInjection
{
    public class ServiceLocator
    {
        private static Dictionary<Type, object> _serviceTable;

        public ServiceLocator(int size)
        {
            _serviceTable = new Dictionary<Type, object>(size);
        }

        public bool Register<T>(T input)
        {
            _serviceTable.Add(typeof(T), input);
            return true;
        }

        public void Register(Type injectType, object serviceObject)
        {
            if (_serviceTable.ContainsKey(injectType))
            {
                //UnityEngine.Debug.Log(injectType);
                return;
            }
            _serviceTable.Add(injectType, serviceObject);
        }

        public object Resolve(Type serviceType)
        {
            if (_serviceTable.TryGetValue(serviceType, out var service))
            {
                return service;
            }
            return null;
        }


        public T Resolve<T>()
        {
            if (_serviceTable.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }

            return default;
        }
    }
}
