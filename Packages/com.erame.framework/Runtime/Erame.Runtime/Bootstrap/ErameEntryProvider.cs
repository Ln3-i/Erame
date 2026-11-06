using Erame.Core;
using UnityEngine;

namespace Erame.Runtime.Bootstrap{
    /// <summary>
    /// 入口提供者（ScriptableObject）。
    /// 场景中的 <see cref="ErameBootstrap"/> 通过序列化引用拿到本资产，
    /// 再由 <see cref="Create"/> 创建入口实例——全程不使用反射
    /// （禁止 MonoScript/Activator/GetClass 形式的类型查找）。
    /// </summary>
    public abstract class ErameEntryProvider : ScriptableObject{
        /// <summary>创建入口实例。每个 Provider 恰好对应一种入口类型。</summary>
        public abstract ErameEntry Create();

        /// <summary>可选：填充构建器（设置、输入源、模块）。默认不做任何事。</summary>
        public virtual void Configure(ErameBuilder builder){
        }
    }
}
