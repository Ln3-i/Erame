using Game.Runtime;
using Game.Runtime.Services;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Erame.Core;

namespace Tests{
    public interface IMarkerA { }
    public interface IMarkerB { }

    public class ServiceContainerTests{
        private class Probe : IService{
            private readonly string _name;
            private readonly List<string> _order;
            private readonly bool _throwOnInit;

            protected Probe(string name, List<string> order, bool throwOnInit = false){
                _name = name;
                _order = order;
                _throwOnInit = throwOnInit;
            }

            public void Initialize(){
                _order.Add(_name);
                if (_throwOnInit) throw new InvalidOperationException($"{_name} 初始化失败");
            }

            public void Dispose(){
                _order.Add($"{_name}:dispose");
            }
        }

        private sealed class A : Probe, IMarkerA{
            public A(List<string> order, bool throwOnInit = false) : base("A", order, throwOnInit){ }
        }

        private sealed class B : Probe, IMarkerB{
            public B(List<string> order, bool throwOnInit = false) : base("B", order, throwOnInit){ }
        }

        private sealed class AB : Probe, IMarkerA, IMarkerB{
            public AB(List<string> order) : base("AB", order){ }
        }

        [Test]
        public void InitializeAll_InitializesDependenciesBeforeDependents(){
            var order = new List<string>();
            var container = new ServiceContainer();

            // A 依赖 B，但 A 先注册：容器应按"依赖优先"初始化，而不是按注册顺序
            container.RegisterSingleton<IMarkerA>(p => {
                p.Get<IMarkerB>();
                return new A(order);
            });
            container.RegisterSingleton<IMarkerB>(p => new B(order));

            container.Build();
            container.InitializeAll();

            Assert.AreEqual(new[] { "B", "A" }, order.ToArray());
        }

        [Test]
        public void Get_CircularDependency_Throws(){
            var container = new ServiceContainer();
            container.RegisterSingleton<IMarkerA>(p => {
                p.Get<IMarkerB>();
                return new A(new List<string>());
            });
            container.RegisterSingleton<IMarkerB>(p => {
                p.Get<IMarkerA>();
                return new B(new List<string>());
            });

            Assert.Throws<InvalidOperationException>(() => container.Get<IMarkerA>());
        }

        [Test]
        public void InitializeAll_OnFailure_RollsBackAlreadyInitializedServices(){
            var order = new List<string>();
            var container = new ServiceContainer();

            container.RegisterSingleton<IMarkerA>(p => new A(order));
            container.RegisterSingleton<IMarkerB>(p => new B(order, true));

            container.Build();
            Assert.Throws<InvalidOperationException>(() => container.InitializeAll());

            // A 已初始化，随后 B 抛异常 -> 逆序回滚：B:dispose, A:dispose
            Assert.AreEqual(new[] { "A", "B", "B:dispose", "A:dispose" }, order.ToArray());
        }

        [Test]
        public void InitializeAll_SameInstanceUnderMultipleTypes_InitializesOnce(){
            var order = new List<string>();
            var instance = new AB(order);

            var container = new ServiceContainer();
            // 同一个实例既是 IMarkerA 又是 IMarkerB（等价于"具体类 + 接口视图"的注册方式）
            container.RegisterSingleton<IMarkerA>(instance);
            container.RegisterSingleton<IMarkerB>(instance);

            container.Build();
            container.InitializeAll();

            Assert.AreEqual(new[] { "AB" }, order.ToArray(), "一个实例只应初始化一次");

            container.Dispose();
            Assert.AreEqual(new[] { "AB", "AB:dispose" }, order.ToArray(), "释放同理只应执行一次");
        }

        [Test]
        public void TryGet_UnregisteredService_ReturnsFalse(){
            var container = new ServiceContainer();
            container.Build();

            Assert.IsFalse(container.TryGet<IMarkerA>(out _));
        }
    }
}
