using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GameWarriors.DependencyInjection.Extensions
{
    internal static class ReflectionExtension
    {
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
        public static bool IsUnityMonoBehaviour(this Type mainType)
        {
#if UNITY_2018_4_OR_NEWER
            return mainType.IsSubclassOf(typeof(UnityEngine.MonoBehaviour));
#else
            return false;
#endif
        }

        public static object CreateUnityGameObject(this Type mainType)
        {
#if UNITY_2018_4_OR_NEWER
            return new UnityEngine.GameObject(mainType.Name, mainType).GetComponent(mainType);
#else
            return null;
#endif
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
                    ServiceCollectionExtenstion.LogError(mainType, $"error in GetConstructorParams");
                    return null;
                }
            }
            return null;
        }


    }
}