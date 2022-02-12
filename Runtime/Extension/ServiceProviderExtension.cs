using System;

namespace GameWrriors.DependencyInjection.Extensions
{

    public static class ServiceProviderExtension
    {
        public static T GetService<T>(this IServiceProvider serviceProvider) where T : class
        {
           // Debug.Log(serviceProvider);
            return serviceProvider.GetService(typeof(T)) as T;
        }
    }
}