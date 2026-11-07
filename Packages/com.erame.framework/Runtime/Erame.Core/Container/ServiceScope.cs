using System;
using System.Collections.Generic;
using System.Text;

namespace Erame.Core{
    /// <summary>
    /// 局级作用域：在根容器之上叠加"这一局才需要"的服务。
    /// <para>
    /// 解析顺序：作用域自身 → 根容器；作用域 Dispose 时只释放自己创建的服务，根容器单例保持存活。
    /// 典型用法（新开一局）：
    /// <code>
    /// using var session = ErameRuntime.Services.CreateScope();
    /// session.RegisterSingleton&lt;IEconomyService&gt;(p => new EconomyService(p.Get&lt;IDataRegistry&gt;()));
    /// session.InitializeAll();
    /// // ... 这一局结束后 using 自动释放局级服务
    /// </code>
    /// </para>
    /// 与根容器不同，作用域允许在初始化之后再注册（一局中的章节、关卡可以随时追加服务）。
    /// </summary>
    public sealed class ServiceScope : IServiceProvider, IServiceRegistrar, IDisposable{
        private readonly ServiceContainer _root;

        private readonly Dictionary<Type, object> _instances = new();
        private readonly Dictionary<Type, Func<IServiceProvider, object>> _factories = new();
        private readonly List<Type> _registrationOrder = new();

        // 实际创建顺序即依赖拓扑序，InitializeAll 按它初始化
        private readonly List<Type> _creationOrder = new();
        private readonly List<IService> _initializedServices = new();

        private readonly HashSet<Type> _resolving = new();
        private readonly List<Type> _resolveStack = new();

        private bool _initialized;
        private bool _disposed;

        internal ServiceScope(ServiceContainer root){
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        /// <summary>根容器。</summary>
        public ServiceContainer Root => _root;

        /// <summary>是否已释放。</summary>
        public bool IsDisposed => _disposed;

        /// <summary>只读诊断：按注册顺序返回作用域内注册的服务类型（不含根容器）。</summary>
        public IReadOnlyList<Type> GetRegisteredServiceTypes() => new List<Type>(_registrationOrder);

        /// <summary>只读诊断：按实际创建顺序返回服务类型（即初始化顺序）。</summary>
        public IReadOnlyList<Type> GetCreationOrder() => new List<Type>(_creationOrder);

        public void RegisterSingleton<T>(T instance) where T : class{
            ThrowIfDisposed();
            var type = typeof(T);
            if (IsRegisteredLocal(type)) throw new InvalidOperationException($"Service already registered in scope: {type.Name}");

            _instances[type] = instance;
            _registrationOrder.Add(type);
            _creationOrder.Add(type);
        }

        public void RegisterSingleton<T>(Func<IServiceProvider, T> factory) where T : class{
            ThrowIfDisposed();
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            var type = typeof(T);
            if (IsRegisteredLocal(type)) throw new InvalidOperationException($"Service already registered in scope: {type.Name}");

            _factories[type] = factory;
            _registrationOrder.Add(type);
        }

        /// <summary>实例化作用域内注册的所有服务，并按依赖顺序初始化实现了 <see cref="IService"/> 的服务。</summary>
        public void InitializeAll(){
            ThrowIfDisposed();
            if (_initialized) return;

            try{
                foreach (var type in _registrationOrder.ToArray()){
                    Get(type);
                }

                // 与根容器一致：同一实例可能以具体类型 + 多个接口视图注册，按引用去重
                foreach (var type in _creationOrder.ToArray()){
                    if (!(_instances[type] is IService service)) continue;
                    if (ServiceContainer.IndexOfSame(_initializedServices, service) >= 0) continue;

                    _initializedServices.Add(service);
                    service.Initialize();
                }
                _initialized = true;
            }
            catch (Exception ex){
                RollbackInitialized();
                throw new InvalidOperationException($"[Scope] 初始化失败: {ex.Message}", ex);
            }
        }

        public T Get<T>() where T : class => (T)Get(typeof(T));

        public bool TryGet<T>(out T service) where T : class{
            var type = typeof(T);
            if (!IsRegisteredLocal(type) && !_root.TryResolve(type, out _)){
                service = null;
                return false;
            }
            service = (T)Get(type);
            return true;
        }

        private object Get(Type type){
            ThrowIfDisposed();

            if (_instances.TryGetValue(type, out var instance)) return instance;

            if (_factories.TryGetValue(type, out var factory)){
                if (_resolving.Contains(type)){
                    throw new InvalidOperationException($"[Scope] 检测到循环依赖: {BuildResolveChain(type)}");
                }

                _resolving.Add(type);
                _resolveStack.Add(type);
                try{
                    var created = factory(this);
                    _instances[type] = created;
                    _factories.Remove(type);
                    _creationOrder.Add(type);
                    return created;
                }
                finally{
                    _resolving.Remove(type);
                    _resolveStack.RemoveAt(_resolveStack.Count - 1);
                }
            }

            // 回退到根容器：局级服务可以依赖全局单例，反过来不行
            if (_root.TryResolve(type, out var fromRoot)) return fromRoot;

            throw new InvalidOperationException($"Service not found: {type.Name}");
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

        private bool IsRegisteredLocal(Type type){
            return _instances.ContainsKey(type) || _factories.ContainsKey(type);
        }

        private void RollbackInitialized(){
            for (int i = _initializedServices.Count - 1; i >= 0; i--){
                try{ _initializedServices[i].Dispose(); }
                catch (Exception ex){ Console.WriteLine($"[Scope] 回滚 Dispose 失败: {ex.Message}"); }
            }
            _initializedServices.Clear();
        }

        private void ThrowIfDisposed(){
            if (_disposed) throw new ObjectDisposedException(nameof(ServiceScope));
        }

        public void Dispose(){
            if (_disposed) return;
            _disposed = true;

            RollbackInitialized();

            _instances.Clear();
            _factories.Clear();
            _registrationOrder.Clear();
            _creationOrder.Clear();
            _resolving.Clear();
            _resolveStack.Clear();
        }
    }
}
