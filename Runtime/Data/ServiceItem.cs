using System;
using System.Reflection;

namespace GameWarriors.DependencyInjection.Data
{
    //internal enum EServiceLifeType { None, Singleton, Scope, Transient }

    internal class ServiceItem
    {
        public object Instance { get; set; }
        public bool IsChainDepend { get; set; }
        public ParameterInfo[] CtorParamsArray { get; internal set; }
        public MethodInfo LoadingMethod { get; internal set; }
        public PropertyInfo[] Properties { get; internal set; }
        public MethodInfo InitMethod { get; internal set; }
        public ParameterInfo[] InitParamsArray { get; internal set; }
    }
}