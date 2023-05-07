using GameWarriors.DependencyInjection.Abstraction;
using GameWarriors.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameWarriors.DependencyInjection.Core
{
    public class ServiceCollection : IServiceCollection
    {
        private readonly string INIT_METHOD_NAME;
        private Dictionary<Type, IServiceItem> _mainTypeTable;
        private Dictionary<Type, Type> _abstractionToMainTable;
        private List<ITransientServiceItem> _transientItems;
        private ServiceProvider _serviceProvider;
        private DependencyHistory _dependencyHistory;
        private Func<Task> _extraLoading;
        private int _loadingCount;

        public string InitializeMethodName => INIT_METHOD_NAME;

        public ServiceCollection(string initMethodName = default, Func<Task> extraLoading = default) : this(new ServiceProvider(), initMethodName, extraLoading)
        {
        }

        public ServiceCollection(ServiceProvider serviceProvider, string initMethodName = default, Func<Task> extraLoading = default)
        {
            INIT_METHOD_NAME = initMethodName;
            _mainTypeTable = new Dictionary<Type, IServiceItem>();
            _abstractionToMainTable = new Dictionary<Type, Type>();
            _transientItems = new List<ITransientServiceItem>();
            _extraLoading = extraLoading;
            if (serviceProvider == null)
                throw new NullReferenceException("Ther service provider is null");
            _serviceProvider = serviceProvider;
            _dependencyHistory = new DependencyHistory();
            AddSingleton<IServiceProvider, ServiceProvider>(_serviceProvider);
        }

        public void AddSingleton(object instance)
        {
            Type mainType = instance.GetType();
            AddSingleton(instance, mainType, mainType);
        }

        public void AddSingleton(Type mainType, Type injectType)
        {
            ServiceItem<object> serviceItem = new ServiceItem<object>(null);
            AddItem(mainType, serviceItem);
            _abstractionToMainTable.Add(injectType, mainType);
        }

        public void AddSingleton(object instance, Type mainType, Type injectType)
        {
            ServiceItem<object> serviceItem = new ServiceItem<object>(null);
            serviceItem.Instance = instance;
            AddItem(mainType, serviceItem);
            _abstractionToMainTable.Add(injectType, mainType);
        }

        public void AddSingleton<T>(Func<T, Task> loading = null) where T : class
        {
            Type mainType = typeof(T);
            AddItem(mainType, new ServiceItem<T>(loading));
            _abstractionToMainTable.Add(mainType, mainType);
        }

        public void AddSingleton<T>(T instance, Func<T, Task> loading = null) where T : class
        {
            Type mainType = typeof(T);
            AddItem(mainType, new ServiceItem<T>(loading) { Instance = instance });
            _abstractionToMainTable.Add(mainType, mainType);
        }

        public void AddSingleton<I, T>(Func<T, Task> loading = null) where T : class, I
        {
            Type mainType = typeof(T);
            var item = new ServiceItem<T>(loading);
            AddItem(mainType, item);
            _abstractionToMainTable.Add(typeof(I), mainType);
        }

        public void AddSingleton<I, T>(T instance, Func<T, Task> loading = null) where T : class, I
        {
            Type mainType = typeof(T);
            Type injectType = typeof(I);
            AddItem(mainType, new ServiceItem<T>(loading) { Instance = instance });
            _abstractionToMainTable.Add(injectType, mainType);
        }

        public void AddTransient<I, T>() where T : class, I
        {
            ITransientServiceItem item = _serviceProvider.SetTransientService(typeof(I), typeof(T));
            _transientItems.Add(item);
        }

        public void AddTransient<I, T>(Func<IServiceProvider, I> factoryMethod, bool isFillProperties = true) where T : class, I
        {
            _serviceProvider.SetTransientService(typeof(I), new MethodFactory<I>(typeof(T), factoryMethod, isFillProperties));
        }

        //public void AddTransient<I, T, Y>(Y factoryInstance = default) where T : class, I where Y : IServiceFactory<I>
        //{
        //    _serviceProvider.SetTransientService(typeof(I), factoryInstance);
        //}

        public void SetSingletonService(Type injectType, object serviceObject)
        {
            _serviceProvider.SetSingletonService(injectType, serviceObject);
        }

        public async Task Build(Action<IServiceProvider> onDone = null)
        {
            //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            //stopwatch.Start();
            _loadingCount = 0;
            await Task.WhenAll(
                Task.Run(() => Parallel.ForEach(_mainTypeTable, FindSingletonConstructorParams)),
                Task.Run(() => Parallel.ForEach(_transientItems, FindTransientConstructorParams)));
            InitializeSingleton();
            await Task.Run(() => Parallel.ForEach(_mainTypeTable.Values, SetSingletonProperties));
            //stopwatch.Start();
            await WaitLoadingAll();
            Task extraLoadingTask = _extraLoading?.Invoke();
            if (extraLoadingTask != null)
                await extraLoadingTask;
            WaitInitAll();
            //stopwatch.Stop();
            //UnityEngine.Debug.Log(stopwatch.ElapsedTicks);
            onDone?.Invoke(_serviceProvider);
        }

        public object ResolveSingletonService(Type serviceType)
        {
            if (_abstractionToMainTable.TryGetValue(serviceType, out var mainType) && _mainTypeTable.TryGetValue(mainType, out var item))
            {
                if (item.Instance != null)
                {
                    return item.Instance;
                }
                else
                {
                    return item.CreateInstance(mainType, serviceType, _dependencyHistory, this);
                }
            }
            return null;
        }

        private Task WaitLoadingAll()
        {
            Task[] loadingTasks = new Task[_loadingCount];
            foreach (IServiceItem item in _mainTypeTable.Values)
            {
                Task task = item.InvokeLoadingAsync();
                if (task != null)
                {
                    --_loadingCount;
                    loadingTasks[_loadingCount] = task;
                }
            }
            return Task.WhenAll(loadingTasks);
        }

        private void WaitInitAll()
        {
            if (string.IsNullOrEmpty(INIT_METHOD_NAME))
                return;

            foreach (var item in _mainTypeTable.Values)
            {
                item.InvokeInitialization(null, this);
            }
        }

        private void InitializeSingleton()
        {
            foreach (var item in _abstractionToMainTable)
            {
                Type mainType = item.Value;
                Type injectType = item.Key;
                IServiceItem serviceItem = _mainTypeTable[mainType];
                if (!serviceItem.IsCreated())
                {
                    serviceItem.CreateInstance(mainType, injectType, _dependencyHistory, this);
                }
                else
                {
                    _serviceProvider.SetSingletonService(injectType, serviceItem.Instance);
                }
            }
        }

        private void AddItem(Type injectType, IServiceItem item)
        {
            if (!_mainTypeTable.ContainsKey(injectType))
                _mainTypeTable.Add(injectType, item);
        }

        private void FindSingletonConstructorParams(KeyValuePair<Type, IServiceItem> input)
        {
            Type mainType = input.Key;
            IServiceItem item = input.Value;
            bool hasLoading = item.SetupParams(mainType, INIT_METHOD_NAME);
            if (hasLoading)
            {
                lock (_mainTypeTable)
                {
                    ++_loadingCount;
                }
            }
        }

        private void FindTransientConstructorParams(ITransientServiceItem item)
        {
            item.SetupParams(INIT_METHOD_NAME);
        }

        private void SetSingletonProperties(IServiceItem targetType)
        {
            targetType.SetProperties(_serviceProvider);
        }
    }
}