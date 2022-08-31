using GameWarriors.DependencyInjection.Abstraction;
using GameWarriors.DependencyInjection.Extensions;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GameWarriors.DependencyInjection.Core
{
    public class MethodFactory<I> : IObjectFactory
    {
        private readonly Func<IServiceProvider, I> _factoryMethod;
        private PropertyInfo[] Properties { get; set; }

        public MethodFactory(Type mainType, Func<IServiceProvider, I> method, bool isFillProperties)
        {
            _factoryMethod = method;
            if (isFillProperties)
                Properties = mainType.FindProperties();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object CreateObject(IServiceProvider serviceProvider)
        {
            object instance = _factoryMethod(serviceProvider);
            serviceProvider?.SetProperties(instance, Properties);
            return instance;
        }
    }
}