using Game.Runtime;
using Game.Runtime.Services;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Erame.Core;

namespace Tests{
    public class ServiceScopeTests{
        private class ScopeProbe : IService{
            private readonly string _name;
            private readonly List<string> _order;

            public ScopeProbe(string name, List<string> order){
                _name = name;
                _order = order;
            }

            public void Initialize() => _order.Add(_name);
            public void Dispose() => _order.Add($"{_name}:dispose");
        }

        // 标记接口只是用来区分"根容器注册"与"作用域注册"，实例本身仍共享同一套探针逻辑
        private sealed class ProbeA : ScopeProbe, IMarkerA{
            public ProbeA(string name, List<string> order) : base(name, order){ }
        }

        private sealed class ProbeB : ScopeProbe, IMarkerB{
            public ProbeB(string name, List<string> order) : base(name, order){ }
        }

        private static ServiceContainer NewRoot(List<string> order){
            var root = new ServiceContainer();
            root.RegisterSingleton<IMarkerB>(_ => new ProbeB("rootB", order));
            root.Build();
            root.InitializeAll();
            return root;
        }

        [Test]
        public void Scope_ResolvesLocalService_ThatDependsOnRootSingleton(){
            var order = new List<string>();
            var root = NewRoot(order);

            using var scope = root.CreateScope();
            scope.RegisterSingleton<IMarkerA>(p => {
                p.Get<IMarkerB>(); // 局级服务可以依赖全局单例
                return new ProbeA("scopeA", order);
            });
            scope.InitializeAll();

            Assert.IsNotNull(scope.Get<IMarkerA>());
            Assert.AreSame(root.Get<IMarkerB>(), scope.Get<IMarkerB>(), "作用域内解析根服务应返回同一实例");
            Assert.AreEqual(new[] { "rootB", "scopeA" }, order.ToArray());
        }

        [Test]
        public void Scope_Dispose_ReleasesOnlyLocalServices(){
            var order = new List<string>();
            var root = NewRoot(order);

            var scope = root.CreateScope();
            scope.RegisterSingleton<IMarkerA>(_ => new ProbeA("scopeA", order));
            scope.InitializeAll();

            scope.Dispose();

            Assert.AreEqual(new[] { "rootB", "scopeA", "scopeA:dispose" }, order.ToArray());
            Assert.AreEqual(1, scope.Root.GetRegisteredServiceTypes().Count, "根容器服务不应随作用域释放");
            Assert.Throws<ObjectDisposedException>(() => scope.Get<IMarkerA>());
        }

        [Test]
        public void Scope_CircularDependency_Throws(){
            var root = NewRoot(new List<string>());
            using var scope = root.CreateScope();

            scope.RegisterSingleton<IMarkerA>(p => {
                p.Get<IMarkerB>();
                return new ProbeA("A", new List<string>());
            });
            scope.RegisterSingleton<IMarkerB>(p => {
                p.Get<IMarkerA>();
                return new ProbeB("B", new List<string>());
            });

            var ex = Assert.Throws<InvalidOperationException>(() => scope.Get<IMarkerA>());
            StringAssert.Contains("[Scope] 检测到循环依赖", ex.Message);
        }

        [Test]
        public void Scope_DuplicateRegistration_Throws(){
            var root = NewRoot(new List<string>());
            using var scope = root.CreateScope();

            scope.RegisterSingleton<IMarkerA>(_ => new ProbeA("A", new List<string>()));

            var ex = Assert.Throws<InvalidOperationException>(() =>
                scope.RegisterSingleton<IMarkerA>(_ => new ProbeA("A2", new List<string>())));
            StringAssert.Contains("Service already registered in scope", ex.Message);
        }

        [Test]
        public void Scope_RegisterAfterInitialize_IsAllowed(){
            var root = NewRoot(new List<string>());
            using var scope = root.CreateScope();
            scope.InitializeAll();

            // 与根容器不同：一局中的章节/关卡可以随时追加服务
            scope.RegisterSingleton<IMarkerA>(_ => new ProbeA("late", new List<string>()));
            Assert.IsNotNull(scope.Get<IMarkerA>());
        }

        [Test]
        public void Scope_CreateScope_OnDisposedContainer_Throws(){
            var root = NewRoot(new List<string>());
            root.Dispose();

            Assert.Throws<InvalidOperationException>(() => root.CreateScope());
        }
    }
}
