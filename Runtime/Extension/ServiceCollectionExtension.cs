using GameWarriors.DependencyInjection.Abstraction;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GameWarriors.DependencyInjection.Extensions
{
    internal static class ServiceCollectionExtension
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCreated(this IServiceItem serviceItem)
        {
            return serviceItem.Instance != null;
        }

        public static void LogError(this object item, string message)
        {
#if UNITY_2017_4_OR_NEWER
            UnityEngine.Debug.LogError($"Error in :{item}, by message: " + message);
#endif
        }

        public static void InvokeInit(this IServiceCollection serviceCollection, MethodInfo methodInfo, ParameterInfo[] infos, object instance)
        {
            int length = infos?.Length ?? 0;
            if (length > 0)
            {
                object[] tmp = new object[length];
                for (int i = 0; i < length; ++i)
                {
                    Type argType = infos[i].ParameterType;
                    tmp[i] = serviceCollection.ResolveService(argType);
                }
                methodInfo.Invoke(instance, tmp);
            }
            else
                methodInfo.Invoke(instance, null);
        }
        //public static void LogError(this object item, string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string sourceFilePath = null)
        //{
        //    Debug.LogError($"Error in Class:{item}, by message: " +  message);
        //}


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SetProperties(this IServiceCollection serviceCollection, object instance, PropertyInfo[] properties)
        {
            int length = properties?.Length ?? 0;
            for (int i = 0; i < length; ++i)
            {
                Type abstractionType = properties[i].PropertyType;
                if (properties[i].CanWrite)
                {
                    object service = serviceCollection.ResolveService(abstractionType);
                    if (service != null)
                        properties[i].SetValue(instance, service);
                }
            }
        }
    }
}