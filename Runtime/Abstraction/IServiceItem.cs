using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GameWarriors.DependencyInjection.Abstraction
{
    public interface IServiceItem
    {
        MethodInfo LoadingMethod { get; }
        bool IsChainDepend { get; }
        object Instance { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetupParams(Type mainType, string initMethodName,string loadingMethodName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetProperties(IServiceProvider serviceProvider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        object CreateInstance(Type mainType, Type injectType, IServiceCollection serviceCollection);
    }
}