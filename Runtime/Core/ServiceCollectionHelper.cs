using GameWarriors.DependencyInjection.Abstraction;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameWarriors.DependencyInjection.Core
{
    internal static class ServiceCollectionHelper
    {

        //private Task WaitLoadingAll()
        //{
        //    Task[] loadingTasks = new Task[_loadingCount];
        //    foreach (ServiceItem item in _mainTypeTable.Values)
        //    {
        //        if (item.LoadingMethod != null)
        //        {
        //            --_loadingCount;
        //            loadingTasks[_loadingCount] = InvokeLoading(item.LoadingMethod, item.Instance);
        //        }
        //    }
        //    return Task.WhenAll(loadingTasks);
        //}


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodInfo FindMethod(this Type mainType, string methodName)
        {
            return mainType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyInfo[] FindProperties(this Type mainType)
        {
            return mainType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCreated(this IServiceItem serviceItem)
        {
            return serviceItem.Instance != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUnityMonoBehaviour(this Type mainType)
        {
            return mainType.IsSubclassOf(typeof(MonoBehaviour));
        }

        public static object CreateUnityGameObject(this Type mainType)
        {
            return new GameObject(mainType.Name, mainType).GetComponent(mainType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParameterInfo[] GetConstructorParams(this Type mainType)
        {
            if (!mainType.IsUnityMonoBehaviour())
            {
                ConstructorInfo[] constructors = mainType.GetConstructors();
                if (constructors != null && constructors.Length > 0)
                {
                    ConstructorInfo firstConstructors = constructors[0];
                    ParameterInfo[] constructorParams = firstConstructors.GetParameters();
                    return constructorParams;
                }
                else
                {
                    LogError(mainType, $"error in GetConstructorParams");
                    return null;
                }
            }
            return null;
        }

        public static void LogError(this object item, string message)
        {
            Debug.LogError($"Error in :{item}, by message: " + message);
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
                    tmp[i] = serviceCollection.ResolveSingletonService(argType);
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
    }
}