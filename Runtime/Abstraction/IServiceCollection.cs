using System;

namespace GameWarriors.DependencyInjection.Abstraction
{
    public interface IServiceCollection
    {
        string InitializeMethodName { get; }

        object ResolveSingletonService(Type serviceType);
        object ResolveService(Type serviceType);
        void SetSingletonService(Type injectType, object serviceObject);
    }
}