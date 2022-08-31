using GameWarriors.DependencyInjection.Attributes;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GameWarriors.DependencyInjection.Extensions
{
    public static class ServiceProviderExtension
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetService<T>(this IServiceProvider serviceProvider) where T : class
        {
            // Debug.Log(serviceProvider);
            return serviceProvider.GetService(typeof(T)) as T;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetProperties(this IServiceProvider serviceProvider, object instance, PropertyInfo[] properties)
        {
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void InvokeInitMethod(this IServiceProvider serviceProvider, object instance, MethodInfo initMethod, ParameterInfo[] infos)
        {
            if (initMethod != null)
            {
                int length = infos?.Length ?? 0;
                if (length > 0)
                {
                    object[] tmp = new object[length];
                    for (int i = 0; i < length; ++i)
                    {
                        Type argType = infos[i].ParameterType;
                        tmp[i] = serviceProvider.GetService(argType);
                    }
                    initMethod.Invoke(instance, tmp);
                }
                else
                    initMethod.Invoke(instance, null);
            }
        }
    }
}