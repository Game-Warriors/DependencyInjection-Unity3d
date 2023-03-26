using GameWarriors.DependencyInjection.Abstraction;
using GameWarriors.DependencyInjection.Extensions;
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GameWarriors.DependencyInjection.Core
{
    internal class ServiceItem<T> : IServiceItem
    {
        public object Instance { get; set; }
        public ParameterInfo[] CtorParamsArray { get; internal set; }
        public Func<T, Task> LoadingMethod { get; internal set; }
        public MethodInfo InitMethod { get; internal set; }
        public ParameterInfo[] InitParamsArray { get; internal set; }
        private PropertyInfo[] Properties { get; set; }

        public ServiceItem(Func<T, Task> loadingMethod)
        {
            LoadingMethod = loadingMethod;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetupParams(Type mainType, string initMethodName)
        {
            if (!mainType.IsUnityMonoBehaviour())
            {
                CtorParamsArray = mainType.GetConstructorParams();
            }
            Properties = mainType.FindProperties();
            if (!string.IsNullOrEmpty(initMethodName))
            {
                InitMethod = mainType.FindMethod(initMethodName);
                if (InitMethod != null)
                {
                    InitParamsArray = InitMethod.GetParameters();
                }
            }
            return LoadingMethod != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object CreateInstance(Type mainType, Type injectType, IDependencyHistory history, IServiceCollection serviceCollection)
        {
            ParameterInfo[] constructorParams = CtorParamsArray;
            object serviceObject;
            if (constructorParams == null && mainType.IsUnityMonoBehaviour())
            {
                serviceObject = mainType.CreateUnityGameObject();
            }
            else
            {
                int length = constructorParams?.Length ?? 0;
                if (length > 0)
                {
                    history.AddDependency(mainType);
                    history.AddDependency(injectType);
                    object[] tmp = new object[length];
                    for (int i = 0; i < length; ++i)
                    {
                        Type argType = constructorParams[i].ParameterType;
                        history.CheckDependencyHistory(argType);
                        tmp[i] = serviceCollection.ResolveSingletonService(argType);
                    }
                    history.RemoveDependency(mainType);
                    history.RemoveDependency(injectType);
                    serviceObject = Activator.CreateInstance(mainType, tmp);
                }
                else
                {
                    serviceObject = Activator.CreateInstance(mainType);
                }
            }
            Instance = serviceObject;
            serviceCollection.SetSingletonService(injectType, serviceObject);
            return serviceObject;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperties(IServiceProvider serviceProvider)
        {
            serviceProvider.SetProperties(Instance, Properties);
        }

        public void InvokeInitialization(Type baseType, IServiceCollection serviceCollection)
        {
            if (InitMethod != null)
                serviceCollection.InvokeInit(InitMethod, InitParamsArray, Instance);
        }

        public Task InvokeLoadingAsync()
        {
            return LoadingMethod?.Invoke((T)Instance);
        }

        public IEnumerator InvokeLoading()
        {
            return null;
        }
    }
}