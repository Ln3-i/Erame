using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Erame.Core{
    public sealed class ServiceContainer : IServiceRegistrar, IServiceProvider, IDisposable, IAsyncDisposable{
        private readonly Dictionary<Type, object> _instances = new();
        private readonly Dictionary<Type, Func<IServiceProvider, object>> _factories = new();
        private readonly List<Type> _registrationOrder = new();

        // 实际创建顺序：工厂里解析依赖时，依赖会先于依赖者进入此列表，
        // 因此它天然是一个满足依赖关系的拓扑序，InitializeAll 按它初始化即可。
        private readonly List<Type> _creationOrder = new();
        private readonly List<IService> _initializedServices = new();

        private readonly HashSet<Type> _resolving = new();
        private readonly List<Type> _resolveStack = new();

        private bool _built;
        private bool _disposed;

        /// <summary>容器内部错误（构造失败、Dispose 失败等）的接收者，设置后不再直接输出到控制台。</summary>
        public Action<string> ErrorReporter { get; set; }

        public void RegisterSingleton<T>(T instance) where T : class{
            if (_built) throw new InvalidOperationException("Cannot register after Build().");
            var type = typeof(T);
            if (_instances.ContainsKey(type) || _factories.ContainsKey(type))
                throw new InvalidOperationException($"Service already registered: {type.Name}");

            _instances[type] = instance;
            _registrationOrder.Add(type);
            _creationOrder.Add(type);
        }

        public void RegisterSingleton<T>(Func<IServiceProvider, T> factory) where T : class{
            if (_built) throw new InvalidOperationException("Cannot register after Build().");
            var type = typeof(T);
            if (_factories.ContainsKey(type) || _instances.ContainsKey(type))
                throw new InvalidOperationException($"Service already registered: {type.Name}");

            _factories[type] = factory;
            _registrationOrder.Add(type);
        }

        public void Build(){
            if (_built) throw new InvalidOperationException("Already built.");
            _built = true;
        }

        public void InitializeAll(){
            if (!_built) throw new InvalidOperationException("Call Build() first.");
            if (_disposed) throw new InvalidOperationException("Container already disposed.");

            try{
                // 第一遍：实例化所有服务（惰性创建过程中自动完成依赖排序与环检测）
                foreach (var type in _registrationOrder){
                    Get(type);
                }

                // 第二遍：按创建顺序初始化，保证被依赖者先初始化
                foreach (var type in _creationOrder){
                    var inst = _instances[type];
                    if (inst is IService service){
                        service.Initialize();
                        _initializedServices.Add(service);
                    }
                }
            }
            catch (Exception ex){
                RollbackInitialized();
                throw new InvalidOperationException($"[Container] 初始化失败: {ex.Message}", ex);
            }
        }

        public T Get<T>() where T : class => (T)Get(typeof(T));

        private object Get(Type type){
            if (_instances.TryGetValue(type, out var instance)) return instance;

            if (!_factories.TryGetValue(type, out var factory)){
                throw new InvalidOperationException($"Service not found: {type.Name}");
            }

            if (_resolving.Contains(type)){
                throw new InvalidOperationException($"[Container] 检测到循环依赖: {BuildResolveChain(type)}");
            }

            _resolving.Add(type);
            _resolveStack.Add(type);
            try{
                var newInstance = factory(this);
                _instances[type] = newInstance;
                _factories.Remove(type);
                _creationOrder.Add(type);
                return newInstance;
            }
            catch (Exception ex){
                ReportError($"[Container] 构造服务 {type.Name} 失败: {ex}");
                throw;
            }
            finally{
                _resolving.Remove(type);
                _resolveStack.RemoveAt(_resolveStack.Count - 1);
            }
        }

        private string BuildResolveChain(Type repeat){
            var sb = new StringBuilder();
            int start = _resolveStack.IndexOf(repeat);
            for (int i = start; i < _resolveStack.Count; i++){
                sb.Append(_resolveStack[i].Name).Append(" -> ");
            }
            sb.Append(repeat.Name);
            return sb.ToString();
        }

        public bool TryGet<T>(out T service) where T : class{
            var type = typeof(T);
            // 只对"未注册"返回 false；构造过程中的真实异常必须向上抛出，不能被伪装成"不存在"
            if (!_instances.ContainsKey(type) && !_factories.ContainsKey(type)){
                service = null;
                return false;
            }
            service = (T)Get(type);
            return true;
        }

        private void RollbackInitialized(){
            for (int i = _initializedServices.Count - 1; i >= 0; i--){
                try{ _initializedServices[i].Dispose(); }
                catch (Exception ex){ ReportError($"[Container] 回滚 Dispose 失败: {ex.Message}"); }
            }
            _initializedServices.Clear();
        }

        private void ReportError(string message){
            if (ErrorReporter != null) ErrorReporter(message);
            else Console.WriteLine(message);
        }

        public void Dispose(){
            if (_disposed) return;
            _disposed = true;

            RollbackInitialized();

            ClearRegistrations();
        }

        /// <summary>
        /// 逆序异步释放已初始化的服务。若某个服务没有自定义异步清理，
        /// 会通过 <see cref="IService"/> 的默认实现退回到同步 <see cref="IService.Dispose"/>。
        /// </summary>
        public async ValueTask DisposeAsync(){
            if (_disposed) return;
            _disposed = true;

            for (int i = _initializedServices.Count - 1; i >= 0; i--){
                try{ await _initializedServices[i].DisposeAsync(); }
                catch (Exception ex){ ReportError($"[Container] 异步 Dispose 失败: {ex.Message}"); }
            }
            _initializedServices.Clear();

            ClearRegistrations();
        }

        private void ClearRegistrations(){
            _instances.Clear();
            _factories.Clear();
            _registrationOrder.Clear();
            _creationOrder.Clear();
            _resolving.Clear();
            _resolveStack.Clear();
        }
    }
}
