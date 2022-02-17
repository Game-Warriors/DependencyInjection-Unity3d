using System;
using System.Collections.Generic;

namespace GameWarriors.DependencyInjection
{
    public static class ServiceLocator
    {
        private static Dictionary<Type, object> _serviceTable;

        static ServiceLocator()
        {
            _serviceTable = new Dictionary<Type, object>(25);
        }

        public static bool Register<T>(T input)
        {
            _serviceTable.Add(typeof(T), input);
            return true;
        }

        public static void Register(Type injectType, object serviceObject)
        {
            if (_serviceTable.ContainsKey(injectType))
            {
                //UnityEngine.Debug.Log(injectType);
                return;
            }
            _serviceTable.Add(injectType, serviceObject);
        }

        public static object Resolve(Type serviceType)
        {
            if (_serviceTable.TryGetValue(serviceType, out var service))
            {
                return service;
            }
            return null;
        }


        public static T Resolve<T>()
        {
            if (_serviceTable.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }

            return default;
        }


    }
}
