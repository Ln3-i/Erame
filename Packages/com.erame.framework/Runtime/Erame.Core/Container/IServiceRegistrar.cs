using System;

namespace Erame.Core{
    public interface IServiceRegistrar{
        void RegisterSingleton<T>(T instance) where T : class;
        
        void RegisterSingleton<T>(Func<IServiceProvider, T> factory) where T : class;
    }
}