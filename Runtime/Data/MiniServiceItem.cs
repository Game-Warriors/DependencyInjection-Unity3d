using GameWarriors.DependencyInjection.Abstraction;
using GameWarriors.DependencyInjection.Extensions;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GameWarriors.DependencyInjection.Core
{
    internal class MiniServiceItem : IServiceItem
    {
        public object Instance { get; set; }
        public MethodInfo LoadingMethod { get; private set; }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetProperties(IServiceProvider serviceProvider)
        {
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetupParams(Type mainType, string initMethodName, string loadingMethodName)
        {
            LoadingMethod = mainType.FindMethod(loadingMethodName);
        }
    }
}