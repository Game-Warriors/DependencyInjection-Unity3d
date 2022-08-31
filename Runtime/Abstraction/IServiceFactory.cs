using System;

namespace GameWarriors.DependencyInjection.Abstraction
{
    public interface IServiceFactory<T> : IObjectFactory
    {
        T CreateService(IServiceProvider serviceProvider);
    }
}
