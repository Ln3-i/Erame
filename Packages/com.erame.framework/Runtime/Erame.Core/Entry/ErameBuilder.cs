using System;
using System.Collections.Generic;

namespace Erame.Core{
    /// <summary>
    /// 入口构建器（纯 C#）：业务只通过它注册服务与模块，不必直接操作容器。
    /// 模块只做收集，由 ErameRuntime 在业务注册完成后统一 Configure，保证模块间不依赖注册顺序。
    /// 不提供 RegisterService&lt;TI,TImpl&gt;：那需要反射创建实例，本框架不使用。
    /// </summary>
    public sealed class ErameBuilder{
        private readonly IServiceRegistrar _registrar;
        private readonly List<IModule> _modules = new();

        internal ErameBuilder(IServiceRegistrar registrar){
            _registrar = registrar ?? throw new ArgumentNullException(nameof(registrar));
        }

        /// <summary>注册单例实例。</summary>
        public ErameBuilder RegisterSingleton<T>(T instance) where T : class{
            _registrar.RegisterSingleton(instance);
            return this;
        }

        /// <summary>注册单例工厂；依赖通过 Erame.Core.IServiceProvider 解析。</summary>
        public ErameBuilder RegisterSingleton<T>(Func<Erame.Core.IServiceProvider, T> factory) where T : class{
            _registrar.RegisterSingleton(factory);
            return this;
        }

        /// <summary>收集模块（不立即注册），由 Runtime 统一 Configure。</summary>
        public ErameBuilder AddModule(IModule module){
            if (module != null) _modules.Add(module);
            return this;
        }

        /// <summary>收集模块类型（编译期 new，无反射）。</summary>
        public ErameBuilder AddModule<T>() where T : IModule, new(){
            _modules.Add(new T());
            return this;
        }

        /// <summary>取出已收集的模块，仅供 ErameRuntime 使用。</summary>
        internal IReadOnlyList<IModule> ConsumeModules(){
            return _modules;
        }
    }
}
