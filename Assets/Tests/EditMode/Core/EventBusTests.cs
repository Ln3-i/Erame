using Game.Runtime;
using Game.Runtime.Services;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Erame.Core;

namespace Tests{
    public class EventBusTests{
        private struct TestEvent{
            public int Value;
            public TestEvent(int value){ Value = value; }
        }

        private struct OtherEvent{ }

        private FakeLogService _log;
        private EventBus _bus;

        [SetUp]
        public void SetUp(){
            _log = new FakeLogService();
            _bus = new EventBus(_log);
        }

        [Test]
        public void Publish_DeliversToAllSubscribers(){
            var received = new List<int>();
            _bus.Subscribe<TestEvent>(e => received.Add(e.Value));

            _bus.Publish(new TestEvent(1));
            _bus.Publish(new TestEvent(2));

            CollectionAssert.AreEqual(new[] { 1, 2 }, received);
        }

        [Test]
        public void SubscriptionDispose_Unsubscribes(){
            int count = 0;
            IDisposable subscription = _bus.Subscribe<TestEvent>(_ => count++);

            _bus.Publish(new TestEvent(0));
            subscription.Dispose();
            _bus.Publish(new TestEvent(0));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void Unsubscribe_WithSameHandlerInstance_RemovesIt(){
            int count = 0;
            Action<TestEvent> handler = _ => count++;
            _bus.Subscribe(handler);

            _bus.Publish(new TestEvent(0));
            _bus.Unsubscribe(handler);
            _bus.Publish(new TestEvent(0));

            Assert.AreEqual(1, count);
        }

        [Test]
        public void SubscriberThrows_ExceptionIsIsolatedAndLogged(){
            int reached = 0;
            _bus.Subscribe<TestEvent>(_ => throw new InvalidOperationException("boom"));
            _bus.Subscribe<TestEvent>(_ => reached++);

            Assert.DoesNotThrow(() => _bus.Publish(new TestEvent(0)));
            Assert.AreEqual(1, reached); // 前一个订阅者抛异常不影响后一个
            Assert.AreEqual(1, _log.Errors.Count);
            StringAssert.Contains("boom", _log.Errors[0]);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNotThrow(){
            Assert.DoesNotThrow(() => _bus.Publish(new TestEvent(42)));
        }

        [Test]
        public void GetSubscriptionCounts_ReportsPerType(){
            _bus.Subscribe<TestEvent>(_ => { });
            _bus.Subscribe<TestEvent>(_ => { });
            _bus.Subscribe<OtherEvent>(_ => { });

            var counts = new Dictionary<Type, int>();
            foreach (var pair in _bus.GetSubscriptionCounts()){
                counts[pair.Key] = pair.Value;
            }

            Assert.AreEqual(2, counts[typeof(TestEvent)]);
            Assert.AreEqual(1, counts[typeof(OtherEvent)]);
        }

        [Test]
        public void Dispose_ClearsAllSubscriptions(){
            int count = 0;
            _bus.Subscribe<TestEvent>(_ => count++);
            ((IService)_bus).Dispose();

            _bus.Publish(new TestEvent(0));
            Assert.AreEqual(0, count);
        }

        [Test]
        public void ConcurrentSubscribeAndUnsubscribe_DoesNotLoseHandlers(){
            const int total = 200;
            var handlers = new List<Action<TestEvent>>(total);
            for (int i = 0; i < total; i++) handlers.Add(_ => { });

            // 一半订阅后立刻退订，另一半保留；并发下不能出现"别人的订阅被覆盖"
            System.Threading.Tasks.Parallel.For(0, total, i => {
                var subscription = _bus.Subscribe(handlers[i]);
                if (i % 2 == 0) subscription.Dispose();
            });

            int remaining = 0;
            foreach (var pair in _bus.GetSubscriptionCounts()) remaining += pair.Value;
            Assert.AreEqual(total / 2, remaining);
        }

        [Test]
        public void HasSubscribers_ReflectsCurrentState(){
            Assert.IsFalse(_bus.HasSubscribers<TestEvent>());

            var subscription = _bus.Subscribe<TestEvent>(_ => { });
            Assert.IsTrue(_bus.HasSubscribers<TestEvent>());

            subscription.Dispose();
            Assert.IsFalse(_bus.HasSubscribers<TestEvent>());
        }
    }
}
