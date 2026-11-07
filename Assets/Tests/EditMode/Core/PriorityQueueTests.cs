using Game.Runtime;
using Game.Runtime.Services;
using System;
using NUnit.Framework;
using Erame.Core.Algorithms.Collections;

namespace Tests{
    public class PriorityQueueTests{
        [Test]
        public void Dequeue_ReturnsElementsInAscendingPriority(){
            var queue = new PriorityQueue<string, int>();
            queue.Enqueue("c", 30);
            queue.Enqueue("a", 10);
            queue.Enqueue("d", 40);
            queue.Enqueue("b", 20);

            Assert.AreEqual("a", queue.Dequeue());
            Assert.AreEqual("b", queue.Dequeue());
            Assert.AreEqual("c", queue.Dequeue());
            Assert.AreEqual("d", queue.Dequeue());
            Assert.AreEqual(0, queue.Count);
        }

        [Test]
        public void Peek_DoesNotRemoveElement(){
            var queue = new PriorityQueue<string, int>();
            queue.Enqueue("only", 5);

            Assert.AreEqual("only", queue.Peek());
            Assert.AreEqual(1, queue.Count);
            Assert.AreEqual("only", queue.Dequeue());
        }

        [Test]
        public void TryDequeue_EmptyQueue_ReturnsFalse(){
            var queue = new PriorityQueue<string, int>();

            Assert.IsFalse(queue.TryDequeue(out _));
            Assert.IsFalse(queue.TryPeek(out _));
            Assert.IsTrue(queue.IsEmpty);
        }

        [Test]
        public void DequeueOrPeek_EmptyQueue_Throws(){
            var queue = new PriorityQueue<string, int>();

            Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
            Assert.Throws<InvalidOperationException>(() => queue.Peek());
        }

        [Test]
        public void Clear_EmptiesTheQueue(){
            var queue = new PriorityQueue<string, int>();
            queue.Enqueue("a", 1);
            queue.Enqueue("b", 2);

            queue.Clear();

            Assert.AreEqual(0, queue.Count);
            Assert.IsTrue(queue.IsEmpty);
            Assert.IsFalse(queue.TryDequeue(out _));
        }

        [Test]
        public void MixedPriorities_LargeBatch_StayOrdered(){
            var queue = new PriorityQueue<int, int>();
            // 逆序批量入队，再全部出队验证有序
            for (int i = 999; i >= 0; i--) queue.Enqueue(i, i);

            int previous = -1;
            while (queue.TryDequeue(out var element)){
                Assert.Greater(element, previous);
                previous = element;
            }
        }
    }
}
