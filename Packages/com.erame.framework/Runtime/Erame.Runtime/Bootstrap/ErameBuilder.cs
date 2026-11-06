using System;
using System.Collections.Generic;
using Erame.Core;
using Erame.Runtime.Config;

namespace Erame.Runtime.Bootstrap{
    /// <summary>
    /// 构建器：在启动前收集配置（设置、输入源、模块），由 <see cref="ErameRuntime.Start"/>
    /// 统一执行——模块先收集、后 Configure，保证注册顺序确定、不会出现半配置状态。
    /// 注意：本类型不提供 RegisterService&lt;TI,TImpl&gt; 之类的逐服务映射 API；
    /// 服务注册统一走 IServiceRegistrar 工厂或 IModule。
    /// </summary>
    public sealed class ErameBuilder{
        private readonly List<IModule> _modules = new();

        /// <summary>框架配置。为 null 时使用 <see cref="ErameSettings.Default"/>。</summary>
        public ErameSettings Settings { get; private set; }

        /// <summary>输入源工厂。为 null 时输入服务不绑定输入源。</summary>
        public Func<Erame.Core.IServiceProvider, IInputSource> InputSourceFactory { get; private set; }

        /// <summary>已收集的模块（尚未 Configure）。</summary>
        public IReadOnlyList<IModule> Modules => _modules;

        public ErameBuilder WithSettings(ErameSettings settings){
            Settings = settings;
            return this;
        }

        public ErameBuilder WithInputSource(Func<Erame.Core.IServiceProvider, IInputSource> inputSourceFactory){
            InputSourceFactory = inputSourceFactory;
            return this;
        }

        public ErameBuilder AddModule(IModule module){
            if (module != null) _modules.Add(module);
            return this;
        }
    }
}
