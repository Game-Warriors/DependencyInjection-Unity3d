using GameWarriors.DependencyInjection.Attributes;
using GameWarriors.DependencyInjection.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GameWarriors.DependencyInjection.Core
{
    public class ServiceCollectionEnumerator
    {
        private const string WAIT_FOR_LOADING_METHOD_NAME = "WaitForLoadingCoroutine";
        private readonly string INIT_METHOD_NAME;
        private Dictionary<Type, MiniServiceItem> _mainTypeTable;
        private Dictionary<Type, Type> _abstractionToMainTable;
        private ServiceProvider _serviceProvider;
        private int _loadingCount;


        public ServiceCollectionEnumerator(string initMethodName = default)
        {
            INIT_METHOD_NAME = initMethodName;
            _mainTypeTable = new Dictionary<Type, MiniServiceItem>();
            _abstractionToMainTable = new Dictionary<Type, Type>();
            _serviceProvider = new ServiceProvider();
            AddSingleton<IServiceProvider, ServiceProvider>(_serviceProvider);
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

        public void AddTransient<I, T>() where T : class, I where I : IDisposable
        {
            _serviceProvider.SetTransientService(typeof(I), typeof(T));
        }

        public IEnumerator Build(Action onDone = null)
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
            onDone?.Invoke();
        }

        private void WaitInitAll()
        {
            if (string.IsNullOrEmpty(INIT_METHOD_NAME))
                return;

            foreach (var item in _mainTypeTable)
            {
                MethodInfo initMethod = GetInitMethod(item.Key);
                if (initMethod != null)
                {
                    ParameterInfo[] parameterInfos = initMethod.GetParameters();
                    InvokeInit(initMethod, parameterInfos, item.Value.Instance);
                }
            }
        }

        private void InitializeSingleton()
        {
            foreach (var item in _abstractionToMainTable)
            {
                Type mainType = item.Value;
                Type injectType = item.Key;
                MiniServiceItem serviceItem = _mainTypeTable[mainType];
                if (!IsCreated(serviceItem))
                {
                    CreateServiceItem(mainType, injectType, serviceItem);
                }
                else
                {
                    _serviceProvider.SetSingletonService(injectType, serviceItem.Instance);
                }
            }
        }

        private void InitializeTransient()
        {
            //TODO need Implementation
        }

        private void AddItem(Type injectType, MiniServiceItem item)
        {
            if (!_mainTypeTable.ContainsKey(injectType))
                _mainTypeTable.Add(injectType, item);
        }

        private void FindLoadingMethod(KeyValuePair<Type, MiniServiceItem> input)
        {
            Type mainType = input.Key;
            MiniServiceItem item = input.Value;
            item.LoadingMethod = mainType.GetMethod(WAIT_FOR_LOADING_METHOD_NAME, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (item.LoadingMethod != null)
            {
                ++_loadingCount;
            }
        }

        private ParameterInfo[] GetConstructorParams(Type mainType)
        {
            if (!mainType.IsSubclassOf(typeof(MonoBehaviour)))
            {
                ConstructorInfo[] constructors = mainType.GetConstructors();
                ConstructorInfo firstConstructors = constructors[0];
                ParameterInfo[] constructorParams = firstConstructors.GetParameters();
                return constructorParams;
            }
            return null;
        }

        private PropertyInfo[] GetProperties(Type mainType)
        {
            return mainType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty);
        }

        private MethodInfo GetInitMethod(Type mainType)
        {
            return mainType.GetMethod(INIT_METHOD_NAME, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        }

        private object CreateServiceItem(Type mainType, Type injectType, MiniServiceItem item)
        {
            ParameterInfo[] constructorParams = GetConstructorParams(mainType);
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
            _serviceProvider.SetSingletonService(injectType, serviceObject);
            return serviceObject;
        }

        private void SetSingletonProperties(Type targetType, MiniServiceItem targetItem)
        {
            PropertyInfo[] properties = GetProperties(targetType);
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

        private void InvokeInit(MethodInfo methodInfo, ParameterInfo[] infos, object instance)
        {
            int length = infos?.Length ?? 0;
            if (length > 0)
            {
                object[] tmp = new object[length];
                for (int i = 0; i < length; ++i)
                {
                    Type argType = infos[i].ParameterType;
                    if (_abstractionToMainTable.TryGetValue(argType, out var mainType) && _mainTypeTable.TryGetValue(mainType, out MiniServiceItem item))
                    {
                        tmp[i] = item.Instance;
                    }
                }
                methodInfo.Invoke(instance, tmp);
            }
            else
                methodInfo.Invoke(instance, null);
        }

        private object ResolveSingletonService(Type serviceType)
        {
            if (_abstractionToMainTable.TryGetValue(serviceType, out var mainType) && _mainTypeTable.TryGetValue(mainType, out MiniServiceItem item))
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

        private bool IsCreated(MiniServiceItem serviceItem)
        {
            return serviceItem.Instance != null;
        }
    }
}