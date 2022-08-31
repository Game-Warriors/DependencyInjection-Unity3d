using System;

namespace GameWarriors.DependencyInjection.Abstraction
{
    public interface IObjectFactory
    {
        object CreateObject(IServiceProvider serviceProvider);
    }
}