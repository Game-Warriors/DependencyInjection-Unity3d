using GameWarriors.DependencyInjection.Abstraction;
using GameWarriors.DependencyInjection.Extensions;
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GameWarriors.DependencyInjection.Core
{
    internal class MiniServiceItem<T> : IServiceItem
    {
        public object Instance { get; set; }
        public Func<T, IEnumerator> LoadingMethod { get; internal set; }

        public MiniServiceItem(Func<T, IEnumerator> loading)
        {
            LoadingMethod = loading;
        }

        public object CreateInstance(Type mainType, Type injectType, IDependencyHistory history, IServiceCollection serviceCollection)
        {
            ParameterInfo[] constructorParams = mainType.GetConstructorParams();
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


        public void InvokeInitialization(Type baseType, IServiceCollection serviceCollection)
        {
            MethodInfo initMethod = baseType.FindMethod(serviceCollection.InitializeMethodName);
            if (initMethod != null)
            {
                ParameterInfo[] parameterInfos = initMethod.GetParameters();
                serviceCollection.InvokeInit(initMethod, parameterInfos, Instance);
            }
        }

        public IEnumerator InvokeLoading()
        {
            return LoadingMethod?.Invoke((T)Instance);
        }

        public Task InvokeLoadingAsync()
        {
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperties(IServiceProvider serviceProvider)
        {
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetupParams(Type mainType, string initMethodName)
        {
            return LoadingMethod != null;
        }
    }
}