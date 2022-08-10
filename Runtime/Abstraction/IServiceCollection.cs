
using System;

namespace GameWarriors.DependencyInjection.Abstraction
{
    internal interface IServiceCollection
    {
        bool IsChainDepend(Type argType);
        object ResolveSingletonService(Type argType);
        void SetSingletonService(Type injectType, object serviceObject);
    }
}