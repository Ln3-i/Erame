using Game.Runtime;
using Game.Runtime.Services;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Erame.Core;

namespace Tests{
    public class InputGateTests{
        private FakeLogService _log;

        [SetUp]
        public void SetUp(){
            _log = new FakeLogService();
        }

        [Test]
        public void Block_RaisesChangedOnlyOnFirstAndLast(){
            var gate = new InputGate(_log);
            var changes = new List<bool>();
            gate.BlockedChanged += blocked => changes.Add(blocked);

            var first = gate.Block("ui");
            var second = gate.Block("dialog");
            Assert.AreEqual(2, gate.BlockCount);

            first.Dispose();
            Assert.IsTrue(gate.IsBlocked, "还有一层拦截时不应恢复");
            second.Dispose();

            CollectionAssert.AreEqual(new[] { true, false }, changes);
            Assert.AreEqual(0, gate.BlockCount);
            Assert.IsFalse(gate.IsBlocked);
        }

        [Test]
        public void Handle_DisposeTwice_ReleasesOnlyOnce(){
            var gate = new InputGate(_log);
            var outer = gate.Block("ui");
            var inner = gate.Block("ui"); // 同一个 owner 叠两层

            outer.Dispose();
            outer.Dispose(); // 重复 Dispose 无副作用
            Assert.AreEqual(1, gate.BlockCount);

            inner.Dispose();
            Assert.AreEqual(0, gate.BlockCount);
        }

        [Test]
        public void Block_NullOwner_IsTracked(){
            var gate = new InputGate(_log);
            var handle = gate.Block();
            Assert.AreEqual(1, gate.BlockCount);

            handle.Dispose();
            Assert.AreEqual(0, gate.BlockCount);
        }

        [Test]
        public void BlockedChanged_SubscriberThrows_IsLoggedAndOtherSubscribersStillRun(){
            var gate = new InputGate(_log);
            int reached = 0;
            gate.BlockedChanged += _ => throw new InvalidOperationException("boom");
            gate.BlockedChanged += _ => reached++;

            Assert.DoesNotThrow(() => gate.Block("ui").Dispose());
            Assert.AreEqual(1, reached);
            // Block 与 Dispose 各触发一次状态变化，两次都应把异常吞掉并记录
            Assert.GreaterOrEqual(_log.Errors.Count, 1);
            StringAssert.Contains("boom", _log.Errors[0]);
        }

        [Test]
        public void Dispose_ClearsState(){
            var gate = new InputGate(_log);
            gate.Block("ui");

            gate.Dispose();

            Assert.AreEqual(0, gate.BlockCount);
            Assert.IsFalse(gate.IsBlocked);
        }
    }
}
