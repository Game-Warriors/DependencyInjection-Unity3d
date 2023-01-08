using System;

namespace GameWarriors.DependencyInjection.Abstraction
{
    public interface IDependencyHistory
    {
        void AddDependency(Type injectType);
        void RemoveDependency(Type injectType);
        bool CheckDependencyHistory(Type argType);
    }
}