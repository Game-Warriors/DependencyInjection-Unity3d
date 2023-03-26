
using System;

namespace GameWarriors.DependencyInjection.Abstraction
{
    public interface IServiceCollection
    {
        string InitializeMethodName { get; }

        bool IsChainDepend(Type argType);
        object ResolveSingletonService(Type argType);
        void SetSingletonService(Type injectType, object serviceObject);
    }
}