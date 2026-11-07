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

        /// <summary>是否已调用 Build()。</summary>
        public bool IsBuilt => _built;

        /// <summary>是否已释放。</summary>
        public bool IsDisposed => _disposed;

        /// <summary>只读诊断：按注册顺序返回服务类型。</summary>
        public IReadOnlyList<Type> GetRegisteredServiceTypes() => new List<Type>(_registrationOrder);

        /// <summary>只读诊断：按实际创建顺序返回服务类型（即初始化顺序）。</summary>
        public IReadOnlyList<Type> GetCreationOrder() => new List<Type>(_creationOrder);

        // 注册来源（诊断用）：类型 -> 谁注册的（如 "InputModule"、"LogModule"）
        private readonly Dictionary<Type, string> _registrationSources = new();

        /// <summary>标记某服务的注册来源，供容器快照显示；重复标记以先到为准。</summary>
        public void NoteRegistrationSource(Type serviceType, string source){
            if (serviceType == null || string.IsNullOrEmpty(source)) return;
            if (!_registrationSources.ContainsKey(serviceType)) _registrationSources[serviceType] = source;
        }

        /// <summary>只读诊断：查询服务的注册来源（模块名等），未知返回 null。</summary>
        public string GetRegistrationSource(Type serviceType){
            if (serviceType == null) return null;
            return _registrationSources.TryGetValue(serviceType, out var source) ? source : null;
        }

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

        /// <summary>
        /// 创建一个局级作用域：新一局/新场景可以往里注册这一局才有的服务，
        /// 解析时自动回退到根容器；作用域 Dispose 只释放自己创建的服务，根容器单例不受影响。
        /// 作用域可以叠加多层（scope 里再 CreateScope）。
        /// </summary>
        public ServiceScope CreateScope(){
            if (_disposed) throw new InvalidOperationException("Container already disposed.");
            return new ServiceScope(this);
        }

        public void InitializeAll(){
            if (!_built) throw new InvalidOperationException("Call Build() first.");
            if (_disposed) throw new InvalidOperationException("Container already disposed.");

            try{
                // 第一遍：实例化所有服务（惰性创建过程中自动完成依赖排序与环检测）
                foreach (var type in _registrationOrder){
                    Get(type);
                }

                // 第二遍：按创建顺序初始化，保证被依赖者先初始化。
                // 同一个实例可能以"具体类型 + 多个接口视图"注册多次（如 FileSaveService / ISaveService / ISaveSlotService），
                // 这里按引用去重，Initialize 与 Dispose 都只执行一次。
                var seen = new List<IService>();
                foreach (var type in _creationOrder){
                    var inst = _instances[type];
                    if (!(inst is IService service)) continue;
                    if (IndexOfSame(seen, service) >= 0) continue;

                    // 先登记再 Initialize：Initialize 抛异常时该服务也要被回滚 Dispose，
                    // 否则"半初始化"的服务会带着副作用残留（测试期望 B:dispose 必须出现）
                    seen.Add(service);
                    _initializedServices.Add(service);
                    service.Initialize();
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

        // 供 ServiceScope 回退解析使用：作用域不能访问根容器的私有注册表，
        // 但必须能区分"根容器没有这个服务"（继续抛 not found）与"构造失败"（向上抛）
        internal object Resolve(Type type) => Get(type);

        internal bool TryResolve(Type type, out object instance){
            if (_instances.TryGetValue(type, out instance)) return true;
            if (_factories.ContainsKey(type)){
                instance = Get(type);
                return true;
            }
            instance = null;
            return false;
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

        /// <summary>
        /// 按初始化顺序返回所有可解析为 T 的实例（同一实例以多个接口视图注册时按引用去重）。
        /// 供底座驱动协议（如 IFrameUpdate）使用，不暴露具体服务类型。
        /// </summary>
        public IReadOnlyList<T> GetAll<T>() where T : class{
            var result = new List<T>();
            var seen = new List<object>();

            foreach (var type in _creationOrder){
                if (!_instances.TryGetValue(type, out var instance)) continue;
                if (!(instance is T typed)) continue;

                bool duplicate = false;
                for (int i = 0; i < seen.Count; i++){
                    if (ReferenceEquals(seen[i], instance)){
                        duplicate = true;
                        break;
                    }
                }
                if (duplicate) continue;

                seen.Add(instance);
                result.Add(typed);
            }

            return result;
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

        /// <summary>按引用查找：同一个实例以多个接口视图注册时，初始化/释放阶段需要去重。</summary>
        internal static int IndexOfSame(IReadOnlyList<IService> list, IService service){
            for (int i = 0; i < list.Count; i++){
                if (ReferenceEquals(list[i], service)) return i;
            }
            return -1;
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
            _registrationSources.Clear();
            _resolving.Clear();
            _resolveStack.Clear();
        }
    }
}
