using GameWarriors.DependencyInjection.Abstraction;
using GameWarriors.DependencyInjection.Attributes;
using GameWarriors.DependencyInjection.Extensions;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GameWarriors.DependencyInjection.Core
{
    internal class TransientServiceItem : IObjectFactory, ITransientServiceItem
    {
        public Type MainType { get; }
        public bool IsChainDepend { get; set; }
        public ParameterInfo[] ParamsArray { get; internal set; }
        public MethodInfo InitMethod { get; internal set; }
        private PropertyInfo[] Properties { get; set; }


        public TransientServiceItem(Type mainType)
        {
            MainType = mainType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetupParams(string initMethodName)
        {
            Type mainType = MainType;
            if (!mainType.IsUnityMonoBehaviour())
            {
                ParamsArray = mainType.GetConstructorParams();
            }
            else if (!string.IsNullOrEmpty(initMethodName))
            {
                InitMethod = mainType.FindMethod(initMethodName);
                if (InitMethod != null)
                {
                    ParamsArray = InitMethod.GetParameters();
                }
            }
            Properties = mainType.FindProperties();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object CreateObject(IServiceProvider serviceProvider)
        {
            try
            {
                Type mainType = MainType;
                ParameterInfo[] constructorParams = ParamsArray;
                object serviceObject;
                if (mainType.IsUnityMonoBehaviour())
                {
                    serviceObject = mainType.CreateUnityGameObject();
                    serviceProvider.SetProperties(serviceObject, Properties);
                    serviceProvider.InvokeInitMethod(serviceObject, InitMethod, ParamsArray);
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
                            //if (serviceProvider.TryGetValue(argType, out var serviceItem) && serviceItem.IsChainDepend)
                            //    throw new Exception($"There are circle dependency reference between type {mainType} & {argType}");
                            tmp[i] = serviceProvider.GetService(argType);
                        }

                        IsChainDepend = false;
                        serviceObject = Activator.CreateInstance(mainType, tmp);
                    }
                    else
                    {
                        serviceObject = Activator.CreateInstance(mainType);
                    }
                    serviceProvider.SetProperties(serviceObject, Properties);
                }
                return serviceObject;
            }
            catch (Exception E)
            {
                this.LogError("Exception in CreateInstance method, " + E.Message);
                return null;
            }

        }

    }
}