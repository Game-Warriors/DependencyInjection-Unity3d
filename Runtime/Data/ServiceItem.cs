using GameWarriors.DependencyInjection.Abstraction;
using GameWarriors.DependencyInjection.Attributes;
using GameWarriors.DependencyInjection.Core;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GameWarriors.DependencyInjection.Core
{
    //internal enum EServiceLifeType { None, Singleton, Scope, Transient }

    internal class ServiceItem : IServiceItem
    {
        public object Instance { get; set; }
        public bool IsChainDepend { get; set; }
        public ParameterInfo[] CtorParamsArray { get; internal set; }
        public MethodInfo LoadingMethod { get; internal set; }
        public PropertyInfo[] Properties { get; internal set; }
        public MethodInfo InitMethod { get; internal set; }
        public ParameterInfo[] InitParamsArray { get; internal set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetupParams(Type mainType, string initMethodName, string loadingMethodName)
        {
            if (!mainType.IsUnityMonoBehaviour())
            {
                CtorParamsArray = mainType.GetConstructorParams();
            }
            Properties = mainType.FindProperties();
            LoadingMethod = mainType.FindMethod(loadingMethodName);

            if (!string.IsNullOrEmpty(initMethodName))
            {
                InitMethod = mainType.FindMethod(initMethodName);
                if (InitMethod != null)
                {
                    InitParamsArray = InitMethod.GetParameters();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object CreateInstance(Type mainType, Type injectType, IServiceCollection serviceCollection)
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
                    IsChainDepend = true;
                    object[] tmp = new object[length];
                    for (int i = 0; i < length; ++i)
                    {
                        Type argType = constructorParams[i].ParameterType;
                        if (serviceCollection.IsChainDepend(argType))
                            throw new Exception($"There are circle dependency reference between type {mainType} & {argType}");
                        tmp[i] = serviceCollection.ResolveSingletonService(argType);
                    }

                    IsChainDepend = false;
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
            PropertyInfo[] properties = Properties;
            int length = properties?.Length ?? 0;
            for (int i = 0; i < length; ++i)
            {
                InjectAttribute attribute = properties[i].GetCustomAttribute<InjectAttribute>();
                Type abstractionType = properties[i].PropertyType;
                if (attribute != null && properties[i].CanWrite)
                {
                    object service = serviceProvider.GetService(abstractionType);
                    if (service != null)
                        properties[i].SetValue(Instance, service);
                }
            }
        }
    }
}