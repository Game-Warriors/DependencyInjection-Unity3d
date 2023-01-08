using GameWarriors.DependencyInjection.Abstraction;
using GameWarriors.DependencyInjection.Attributes;
using GameWarriors.DependencyInjection.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace GameWarriors.DependencyInjection.Core
{
    public class ServiceCollectionEnumerator : IServiceCollection
    {
        private const string WAIT_FOR_LOADING_METHOD_NAME = "WaitForLoadingCoroutine";
        private readonly string INIT_METHOD_NAME;
        private Dictionary<Type, IServiceItem> _mainTypeTable;
        private Dictionary<Type, Type> _abstractionToMainTable;
        private List<ITransientServiceItem> _transientItems;
        private ServiceProvider _serviceProvider;
        private DependencyHistory _dependencyHistory;
        private int _loadingCount;

        public ServiceCollectionEnumerator(string initMethodName = default)
        {
            INIT_METHOD_NAME = initMethodName;
            _mainTypeTable = new Dictionary<Type, IServiceItem>();
            _abstractionToMainTable = new Dictionary<Type, Type>();
            _transientItems = new List<ITransientServiceItem>();
            _serviceProvider = new ServiceProvider();
            AddSingleton<IServiceProvider, ServiceProvider>(_serviceProvider);
            _dependencyHistory = new DependencyHistory();
        }

        public void AddSingleton<T>() where T : class
        {
            Type mainType = typeof(T);
            AddItem(mainType, new MiniServiceItem());
            _abstractionToMainTable.Add(mainType, mainType);
        }

        public void AddSingleton<T>(T instance) where T : class
        {
            Type mainType = typeof(T);
            AddItem(mainType, new MiniServiceItem() { Instance = instance });
            _abstractionToMainTable.Add(mainType, mainType);
        }

        public void AddSingleton<I, T>() where T : class, I
        {
            Type mainType = typeof(T);
            var item = new MiniServiceItem();
            AddItem(mainType, item);
            _abstractionToMainTable.Add(typeof(I), mainType);
        }

        public void AddSingleton<I, T>(T instance) where T : class, I
        {
            Type mainType = typeof(T);
            Type injectType = typeof(I);
            AddItem(mainType, new MiniServiceItem() { Instance = instance });
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

        //public void AddTransient<I, T>(IServiceFactory<T> serviceFactory) where T : class, I
        //{
        //    _serviceProvider.SetTransientService(typeof(I), serviceFactory);
        //}

        bool IServiceCollection.IsChainDepend(Type argType)
        {
            //if (_mainTypeTable.TryGetValue(argType, out var serviceItem) && serviceItem.IsChainDepend)
            //    return true;
            return false;
        }

        void IServiceCollection.SetSingletonService(Type injectType, object serviceObject)
        {
            _serviceProvider.SetSingletonService(injectType, serviceObject);
        }
        public IEnumerator Build(Action<IServiceProvider> onDone = null)
        {
            //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            //stopwatch.Start();
            _loadingCount = 0;

            int counter = 0;
            foreach (var item in _mainTypeTable)
            {
                FindLoadingMethod(item);
                ++counter;
                if (counter > 50)
                {
                    counter = 0;
                    yield return null;
                }
            }

            yield return null;
            InitializeSingleton();

            foreach (var item in _transientItems)
            {
                item.SetupParams(INIT_METHOD_NAME);
            }

            yield return null;
            counter = 0;
            foreach (var item in _mainTypeTable)
            {
                SetSingletonProperties(item.Key, item.Value);
                ++counter;
                if (counter > 50)
                {
                    counter = 0;
                    yield return null;
                }
            }
            IEnumerator[] loadingList = new IEnumerator[_loadingCount];
            foreach (var item in _mainTypeTable.Values)
            {
                if (item.LoadingMethod != null)
                {
                    --_loadingCount;
                    loadingList[_loadingCount] = InvokeLoading(item.LoadingMethod, item.Instance);
                }
            }
            int length = loadingList.Length;
            for (int i = 0; i < length; ++i)
                yield return loadingList[i];

            //stopwatch.Start();
            yield return null;
            WaitInitAll();
            //stopwatch.Stop();
            //UnityEngine.Debug.Log(stopwatch.ElapsedTicks);
            onDone?.Invoke(_serviceProvider);
        }

        private void WaitInitAll()
        {
            if (string.IsNullOrEmpty(INIT_METHOD_NAME))
                return;

            foreach (var item in _mainTypeTable)
            {
                MethodInfo initMethod = item.Key.FindMethod(INIT_METHOD_NAME);
                if (initMethod != null)
                {
                    ParameterInfo[] parameterInfos = initMethod.GetParameters();
                    this.InvokeInit(initMethod, parameterInfos, item.Value.Instance);
                }
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

        private void AddItem(Type injectType, MiniServiceItem item)
        {
            if (!_mainTypeTable.ContainsKey(injectType))
                _mainTypeTable.Add(injectType, item);
        }

        private void FindLoadingMethod(KeyValuePair<Type, IServiceItem> input)
        {
            Type mainType = input.Key;
            IServiceItem item = input.Value;

            item.SetupParams(mainType, INIT_METHOD_NAME, WAIT_FOR_LOADING_METHOD_NAME);
            if (item.LoadingMethod != null)
            {
                ++_loadingCount;
            }
        }

        private void SetSingletonProperties(Type targetType, IServiceItem targetItem)
        {
            PropertyInfo[] properties = targetType.FindProperties();
            int length = properties?.Length ?? 0;
            for (int i = 0; i < length; ++i)
            {
                InjectAttribute attribute = properties[i].GetCustomAttribute<InjectAttribute>();
                Type abstractionType = properties[i].PropertyType;
                if (attribute != null && properties[i].CanWrite && _abstractionToMainTable.TryGetValue(abstractionType, out var mainType) && _mainTypeTable.TryGetValue(mainType, out var item))
                {
                    properties[i].SetValue(targetItem.Instance, item.Instance);
                }
            }
        }

        private IEnumerator InvokeLoading(MethodInfo methodInfo, object instance)
        {
            IEnumerator tmp = methodInfo.Invoke(instance, null) as IEnumerator;
            if (tmp == null)
                UnityEngine.Debug.LogError(instance);
            return tmp;
        }

        public object ResolveSingletonService(Type serviceType)
        {
            if (_abstractionToMainTable.TryGetValue(serviceType, out var mainType) && _mainTypeTable.TryGetValue(mainType, out var item))
            {
                if (item.Instance != null)
                {
                    //Debug.Log(item.Instance);
                    return item.Instance;
                }
                else
                {
                    return item.CreateInstance(mainType, serviceType, _dependencyHistory, this);
                }
            }
            return null;
        }

    }
}