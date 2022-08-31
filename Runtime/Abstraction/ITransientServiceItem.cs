using System.Runtime.CompilerServices;


namespace GameWarriors.DependencyInjection.Abstraction
{
    public interface ITransientServiceItem
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetupParams(string initMethodName);
    }
}