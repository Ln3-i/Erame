using System;
using System.Collections.Generic;

namespace Erame.Core.Algorithms.Collections{
    // 极简二叉堆实现的最小优先队列
    public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>{
        private readonly List<(TElement element, TPriority priority)> _nodes = new();

        public int Count => _nodes.Count;

        public bool IsEmpty => _nodes.Count == 0;

        public void Enqueue(TElement element, TPriority priority){
            _nodes.Add((element, priority));
            HeapifyUp(_nodes.Count - 1);
        }

        public TElement Dequeue(){
            if (_nodes.Count == 0) throw new InvalidOperationException("Queue is empty.");
            return DequeueCore();
        }

        public TElement Peek(){
            if (_nodes.Count == 0) throw new InvalidOperationException("Queue is empty.");
            return _nodes[0].element;
        }

        /// <summary>尝试取出队首元素；队列为空时返回 false，不抛异常。</summary>
        public bool TryDequeue(out TElement element){
            if (_nodes.Count == 0){
                element = default;
                return false;
            }
            element = DequeueCore();
            return true;
        }

        /// <summary>尝试查看队首元素；队列为空时返回 false，不抛异常。</summary>
        public bool TryPeek(out TElement element){
            if (_nodes.Count == 0){
                element = default;
                return false;
            }
            element = _nodes[0].element;
            return true;
        }

        public void Clear() => _nodes.Clear();

        private TElement DequeueCore(){
            var root = _nodes[0].element;
            var last = _nodes[^1];
            _nodes.RemoveAt(_nodes.Count - 1);
            if (_nodes.Count > 0){
                _nodes[0] = last;
                HeapifyDown(0);
            }
            return root;
        }

        private void HeapifyUp(int index){
            while (index > 0){
                int parent = (index - 1) / 2;
                if (_nodes[index].priority.CompareTo(_nodes[parent].priority) >= 0) break;
                (_nodes[index], _nodes[parent]) = (_nodes[parent], _nodes[index]);
                index = parent;
            }
        }

        private void HeapifyDown(int index){
            int lastIndex = _nodes.Count - 1;
            while (true){
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;

                if (left <= lastIndex && _nodes[left].priority.CompareTo(_nodes[smallest].priority) < 0)
                    smallest = left;
                if (right <= lastIndex && _nodes[right].priority.CompareTo(_nodes[smallest].priority) < 0)
                    smallest = right;

                if (smallest == index) break;
                (_nodes[index], _nodes[smallest]) = (_nodes[smallest], _nodes[index]);
                index = smallest;
            }
        }
    }
}
