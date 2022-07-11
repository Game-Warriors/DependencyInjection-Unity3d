using System.Reflection;

namespace GameWarriors.DependencyInjection.Data
{
    public class MiniServiceItem
    {
        public object Instance { get; set; }
        public bool IsChainDepend { get; set; }
        public MethodInfo LoadingMethod { get; set; }
    }
}