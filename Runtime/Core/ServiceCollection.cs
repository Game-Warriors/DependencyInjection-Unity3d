using GameWarriors.DependencyInjection.Attributes;
using GameWarriors.DependencyInjection.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace GameWarriors.DependencyInjection.Core
{
    public class ServiceCollection
    {
        private Dictionary<Type, ServiceItem> _mainTypeTable;
        private Dictionary<Type, ServiceItem> _transientTable;
        private Dictionary<Type, Type> _abstractionToMainTable;
        private ServiceProvider _serviceProvider;
        private Task[] _loadingTasks;
        private int _loadingCount;


        //private int _singletonCounter;

        public ServiceCollection()
        {
            _mainTypeTable = new Dictionary<Type, ServiceItem>();
            _transientTable = new Dictionary<Type, ServiceItem>();
            _abstractionToMainTable = new Dictionary<Type, Type>();
            _serviceProvider = new ServiceProvider();
            AddSingleton<IServiceProvider, ServiceProvider>(_serviceProvider);
        }

        public void AddSingleton<T>() where T : class
        {
            Type mainType = typeof(T);
            AddItem(mainType, new ServiceItem());
            _abstractionToMainTable.Add(mainType, mainType);
        }

        public void AddSingleton<T>(T instance) where T : class
        {
            Type mainType = typeof(T);
            AddItem(mainType, new ServiceItem() { Instance = instance });
            _abstractionToMainTable.Add(mainType, mainType);
        }

        public void AddSingleton<I, T>() where T : class, I
        {
            Type mainType = typeof(T);
            var item = new ServiceItem();
            AddItem(mainType, item);
            _abstractionToMainTable.Add(typeof(I), mainType);
        }

        public void AddSingleton<I, T>(T instance) where T : class, I
        {
            Type mainType = typeof(T);
            Type injectType = typeof(I);
            AddItem(mainType, new ServiceItem() { Instance = instance });
            _abstractionToMainTable.Add(injectType, mainType);
        }

        public void AddTransient<I, T>() where T : class, I where I : IDisposable
        {
            var item = new ServiceItem();
            _transientTable.Add(typeof(T), item);
            //TODO need Implementation
        }

        public async Task Build()
        {
            _loadingCount = 0;
            await Task.WhenAll(Task.Run(() => Parallel.ForEach(_mainTypeTable, FindConstructorParams)), Task.Run(() => Parallel.ForEach(_transientTable, FindConstructorParams)));
            _loadingTasks = new Task[_loadingCount];
            InitializeSingleton();
            Task transientTask = Task.Run(InitializeTransient);
            Task propertyTask = Task.Run(() => Parallel.ForEach(_mainTypeTable.Values, SetSingletonProperties));
            await Task.WhenAll(_loadingTasks);
            await propertyTask;
        }

        private void InitializeSingleton()
        {
            foreach (var item in _abstractionToMainTable)
            {
                Type mainType = item.Value;
                Type injectType = item.Key;
                ServiceItem serviceItem = _mainTypeTable[mainType];
                if (!IsCreated(serviceItem))
                {
                    CreateServiceItem(mainType, injectType, serviceItem);
                }
                else
                {
                    _serviceProvider.SetService(injectType, serviceItem.Instance);
                }
            }
        }

        private void InitializeTransient()
        {
            //TODO need Implementation
        }

        private void AddItem(Type injectType, ServiceItem item)
        {
            if (!_mainTypeTable.ContainsKey(injectType))
                _mainTypeTable.Add(injectType, item);
        }

        private void FindConstructorParams(KeyValuePair<Type, ServiceItem> input)
        {
            Type mainType = input.Key;
            ServiceItem item = input.Value;
            if (!mainType.IsSubclassOf(typeof(MonoBehaviour)))
            {
                ConstructorInfo[] constructors = mainType.GetConstructors();
                ConstructorInfo firstConstructors = constructors[0];
                ParameterInfo[] constructorParams = firstConstructors.GetParameters();
                item.ParamsArray = constructorParams;
            }

            item.Properties = mainType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty);
            item.LoadingMethod = mainType.GetMethod("WaitForLoading");
            if (item.LoadingMethod != null)
            {
                lock (_mainTypeTable)
                {
                    ++_loadingCount;
                }
            }
        }

        private object CreateServiceItem(Type mainType, Type injectType, ServiceItem item)
        {
            ParameterInfo[] constructorParams = item.ParamsArray;
            object serviceObject;
            if (constructorParams == null && mainType.IsSubclassOf(typeof(MonoBehaviour)))
            {
                serviceObject = new GameObject(mainType.Name, mainType).GetComponent(mainType);
            }
            else
            {
                int length = constructorParams?.Length ?? 0;
                if (length > 0)
                {
                    item.IsChainDepend = true;
                    object[] tmp = new object[length];
                    for (int i = 0; i < length; ++i)
                    {
                        Type argType = constructorParams[i].ParameterType;
                        if (_mainTypeTable.TryGetValue(argType, out var serviceItem) && serviceItem.IsChainDepend)
                            throw new Exception($"There are circle dependency reference between type {mainType} & {argType}");
                        tmp[i] = ResolveSingletonService(argType);
                    }

                    item.IsChainDepend = false;
                    serviceObject = Activator.CreateInstance(mainType, tmp);
                }
                else
                {
                    serviceObject = Activator.CreateInstance(mainType);
                }
            }
            item.Instance = serviceObject;
            _serviceProvider.SetService(injectType, serviceObject);
            if (item.LoadingMethod != null)
            {
                --_loadingCount;
                _loadingTasks[_loadingCount] = InvokeLoading(item.LoadingMethod, serviceObject);
            }
            return serviceObject;
        }

        private void SetSingletonProperties(ServiceItem targetType)
        {
            PropertyInfo[] properties = targetType.Properties;
            int length = properties?.Length ?? 0;
            for (int i = 0; i < length; ++i)
            {
                InjectAttribute attribute = properties[i].GetCustomAttribute<InjectAttribute>();
                Type abstractionType = properties[i].PropertyType;
                if (attribute != null && properties[i].CanWrite && _abstractionToMainTable.TryGetValue(abstractionType, out var mainType) && _mainTypeTable.TryGetValue(mainType, out var item))
                {
                    properties[i].SetValue(targetType.Instance, item.Instance);
                }
            }
        }

        private Task InvokeLoading(MethodInfo methodInfo, object obj)
        {
            var tmp = methodInfo.Invoke(obj, null) as Task;
            if (tmp == null)
                Debug.LogError(obj);
            return tmp;
        }

        private object ResolveSingletonService(Type serviceType)
        {
            if (_abstractionToMainTable.TryGetValue(serviceType, out var mainType) && _mainTypeTable.TryGetValue(mainType, out ServiceItem item))
            {
                if (item.Instance != null)
                {
                    //Debug.Log(item.Instance);
                    return item.Instance;
                }
                else
                {
                    return CreateServiceItem(mainType, serviceType, item);
                }
            }
            return null;
        }

        private object ResolveTransientService(Type serviceType)
        {
            //TODO Implemenet here
            return null;
        }

        private bool IsCreated(ServiceItem serviceItem)
        {
            return serviceItem.Instance != null;
        }
    }
}