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
        private readonly string INIT_METHOD_NAME;
        private Dictionary<Type, ServiceItem> _mainTypeTable;
        private Dictionary<Type, Type> _abstractionToMainTable;
        private ServiceProvider _serviceProvider;
        private int _loadingCount;


        public ServiceCollection(ServiceProvider serviceProvider, string initMethodName = default)
        {
            INIT_METHOD_NAME = initMethodName;
            _mainTypeTable = new Dictionary<Type, ServiceItem>();
            _abstractionToMainTable = new Dictionary<Type, Type>();
            if (serviceProvider == null)
                throw new NullReferenceException("Ther service provider is null");
            AddSingleton<IServiceProvider, ServiceProvider>(_serviceProvider);
        }

        public ServiceCollection(string initMethodName = default)
        {
            INIT_METHOD_NAME = initMethodName;
            _mainTypeTable = new Dictionary<Type, ServiceItem>();
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

        public void AddTransient<I, T>() where T : class, I
        {
            _serviceProvider.SetTransientService(typeof(I), typeof(T));
        }

        internal bool IsChainDepend(Type argType)
        {
            if (_mainTypeTable.TryGetValue(argType, out var serviceItem) && serviceItem.IsChainDepend)
                return true;
            return false;
        }

        public async Task Build(Action onDone = null)
        {
            //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            //stopwatch.Start();
            _loadingCount = 0;
            await Task.WhenAll(
                Task.Run(() => Parallel.ForEach(_mainTypeTable, FindSingletonConstructorParams)), 
                Task.Run(() => Parallel.ForEach(_serviceProvider.TransientTable, FindTransientConstructorParams)));
            InitializeSingleton();
            //Task transientTask = Task.Run(InitializeTransient);
            await Task.Run(() => Parallel.ForEach(_mainTypeTable.Values, SetSingletonProperties));

            //stopwatch.Start();
            await WaitLoadingAll();
            WaitInitAll();
            //stopwatch.Stop();
            //UnityEngine.Debug.Log(stopwatch.ElapsedTicks);
            onDone?.Invoke();
        }

        //private Task WaitLoadingAll()
        //{
        //    Task[] loadingTasks = new Task[_loadingCount];
        //    foreach (ServiceItem item in _mainTypeTable.Values)
        //    {
        //        if (item.LoadingMethod != null)
        //        {
        //            --_loadingCount;
        //            loadingTasks[_loadingCount] = InvokeLoading(item.LoadingMethod, item.Instance);
        //        }
        //    }
        //    return Task.WhenAll(loadingTasks);
        //}

        private Task WaitLoadingAll()
        {
            Task[] loadingTasks = new Task[_loadingCount];
            foreach (ServiceItem item in _mainTypeTable.Values)
            {
                if (item.LoadingMethod != null)
                {
                    --_loadingCount;
                    loadingTasks[_loadingCount] = InvokeLoading(item.LoadingMethod, item.Instance);
                }
            }
            return Task.WhenAll(loadingTasks);
        }

        private void WaitInitAll()
        {
            if (string.IsNullOrEmpty(INIT_METHOD_NAME))
                return;

            foreach (ServiceItem item in _mainTypeTable.Values)
            {
                if (item.InitMethod != null)
                {
                    InvokeInit(item.InitMethod, item.InitParamsArray, item.Instance);
                }
            }
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
                    serviceItem.CreateInstance(mainType, _serviceProvider, injectType, this);
                }
                else
                {
                    _serviceProvider.SetSingletonService(injectType, serviceItem.Instance);
                }
            }
        }

        private void AddItem(Type injectType, ServiceItem item)
        {
            if (!_mainTypeTable.ContainsKey(injectType))
                _mainTypeTable.Add(injectType, item);
        }

        private void FindSingletonConstructorParams(KeyValuePair<Type, ServiceItem> input)
        {
            Type mainType = input.Key;
            ServiceItem item = input.Value;
            item.SetupParams(mainType, INIT_METHOD_NAME);
            if (item.LoadingMethod != null)
            {
                lock (_mainTypeTable)
                {
                    ++_loadingCount;
                }
            }
        }

        private void FindTransientConstructorParams(KeyValuePair<Type, TransientServiceItem> input)
        {
            TransientServiceItem item = input.Value;
            item.SetupParams(INIT_METHOD_NAME);
        }

        private void SetSingletonProperties(ServiceItem targetType)
        {
            targetType.SetProperties(_serviceProvider);
        }

        private Task InvokeLoading(MethodInfo methodInfo, object instance)
        {
            var tmp = methodInfo.Invoke(instance, null) as Task;
            if (tmp == null)
                UnityEngine.Debug.LogError(instance);
            return tmp;
        }

        private void InvokeInit(MethodInfo methodInfo, ParameterInfo[] infos, object instance)
        {
            int length = infos?.Length ?? 0;
            if (length > 0)
            {
                object[] tmp = new object[length];
                for (int i = 0; i < length; ++i)
                {
                    Type argType = infos[i].ParameterType;
                    if (_abstractionToMainTable.TryGetValue(argType, out var mainType) && _mainTypeTable.TryGetValue(mainType, out ServiceItem item))
                    {
                        tmp[i] = item.Instance;
                    }
                }
                methodInfo.Invoke(instance, tmp);
            }
            else
                methodInfo.Invoke(instance, null);
        }

        internal object ResolveSingletonService(Type serviceType)
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
                    return item.CreateInstance(mainType, _serviceProvider, serviceType, this);
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