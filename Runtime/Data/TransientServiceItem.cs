using GameWarriors.DependencyInjection.Attributes;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GameWarriors.DependencyInjection.Core
{
    public class TransientServiceItem
    {
        public Type MainType { get; }
        public bool IsChainDepend { get; set; }
        public ParameterInfo[] ParamsArray { get; internal set; }
        public PropertyInfo[] Properties { get; internal set; }
        public MethodInfo InitMethod { get; internal set; }


        public TransientServiceItem(Type mainType)
        {
            MainType = mainType;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetupParams(string initMethodName)
        {
            Type mainType = MainType;
            if (!mainType.IsUnityMonoBehaviour())
            {
                ParamsArray = mainType.GetConstructorParams();
            }
            else
            {
                if (!string.IsNullOrEmpty(initMethodName))
                {
                    InitMethod = mainType.FindMethod(initMethodName);
                    if (InitMethod != null)
                    {
                        ParamsArray = InitMethod.GetParameters();
                    }
                }
            }
            Properties = mainType.FindProperties();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal object CreateInstance(ServiceProvider serviceProvider)
        {
            try
            {
                Type mainType = MainType;
                ParameterInfo[] constructorParams = ParamsArray;
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
                }
                return serviceObject;
            }
            catch (Exception E)
            {
                this.LogError("Exception in CreateInstance method, " +  E.Message);
                return null;
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetProperties(object instance, ServiceProvider serviceProvider)
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
                        properties[i].SetValue(instance, service);
                }
            }
        }

    }
}