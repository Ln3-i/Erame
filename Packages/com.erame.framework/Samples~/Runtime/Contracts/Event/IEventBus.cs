using System;

namespace __NS__.Runtime{
    public interface IEventBus{
        IDisposable Subscribe<T>(Action<T> handler) where T : struct;
        
        void Unsubscribe<T>(Action<T> handler) where T : struct;
        void Publish<T>(in T evt) where T : struct;
    }
}