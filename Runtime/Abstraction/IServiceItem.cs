using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GameWarriors.DependencyInjection.Abstraction
{
    public interface IServiceItem
    {
        object Instance { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool SetupParams(Type mainType, string initMethodName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetProperties(IServiceProvider serviceProvider);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        object CreateInstance(Type mainType, Type injectType, IDependencyHistory history, IServiceCollection collection);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void InvokeInitialization(Type baseType, IServiceCollection serviceCollection);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Task InvokeLoadingAsync();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator InvokeLoading();
    }
}