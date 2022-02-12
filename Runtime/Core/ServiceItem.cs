using System;
using System.Reflection;

namespace GameWrriors.DependencyInjection.Data
{
    //internal enum EServiceLifeType { None, Singleton, Scope, Transient }

    internal class ServiceItem
    {
        public object Instance { get; set; }
        public bool IsChainDepend { get; set; }
        public ParameterInfo[] ParamsArray { get; internal set; }
        public MethodInfo LoadingMethod { get; internal set; }
        public PropertyInfo[] Properties { get; internal set; }
    }
}