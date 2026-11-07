using __NS__.Runtime;
using System;

namespace __NS__.Runtime.Services{
    // 代表一次订阅，Dispose 时自动解绑
    public sealed class EventSubscription : IDisposable{
        private Action _unsubscribeAction;
        
        public EventSubscription(Action unsubscribeAction) => _unsubscribeAction = unsubscribeAction;

        public void Dispose(){
            _unsubscribeAction?.Invoke();
            _unsubscribeAction = null; // 防止重复解绑
        }
    }
}