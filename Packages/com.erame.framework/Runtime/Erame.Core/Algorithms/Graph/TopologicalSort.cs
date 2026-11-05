using System;
using System.Collections.Generic;

namespace Erame.Core.Algorithms.Graph{
    public static class TopologicalSort{
        /// <summary>
        /// 使用 Kahn 算法进行拓扑排序。
        /// </summary>
        /// <param name="nodesCount">节点总数（从 0 到 n-1 编号）</param>
        /// <param name="edges">
        /// 后继关系：edges[i] 中的每个节点都是 i 的后继，即 i 完成后才能执行它们。
        /// 例如 edges[a] = { b } 表示 a 必须先于 b。
        /// </param>
        /// <returns>排好序的节点列表，如果存在环则返回 null</returns>
        /// <exception cref="ArgumentNullException">edges 为 null。</exception>
        /// <exception cref="ArgumentOutOfRangeException">nodesCount 为负，或存在越界节点编号。</exception>
        /// <exception cref="ArgumentException">edges 的长度与 nodesCount 不一致。</exception>
        public static List<int> Sort(int nodesCount, List<int>[] edges){
            if (edges == null) throw new ArgumentNullException(nameof(edges));
            if (nodesCount < 0) throw new ArgumentOutOfRangeException(nameof(nodesCount), "节点数量不能为负数。");
            if (edges.Length != nodesCount){
                throw new ArgumentException($"edges 长度 ({edges.Length}) 与 nodesCount ({nodesCount}) 不一致。", nameof(edges));
            }

            int[] inDegree = new int[nodesCount];

            // 计算入度
            for (int i = 0; i < nodesCount; i++){
                if (edges[i] == null) continue;
                foreach (var target in edges[i]){
                    if (target < 0 || target >= nodesCount){
                        throw new ArgumentOutOfRangeException(nameof(edges), $"节点编号 {target} 越界（节点范围 0..{nodesCount - 1}）。");
                    }
                    inDegree[target]++;
                }
            }

            // 将所有入度为 0 的节点入队
            var queue = new Queue<int>();
            for (int i = 0; i < nodesCount; i++){
                if (inDegree[i] == 0) queue.Enqueue(i);
            }

            var result = new List<int>();
            while (queue.Count > 0){
                int current = queue.Dequeue();
                result.Add(current);

                if (edges[current] != null){
                    foreach (var target in edges[current]){
                        inDegree[target]--;
                        if (inDegree[target] == 0){
                            queue.Enqueue(target);
                        }
                    }
                }
            }

            // 如果结果中的节点数少于总节点数，说明有环（死循环依赖）
            if (result.Count != nodesCount){
                return null;
            }
            return result;
        }

        /// <summary>
        /// 按"前驱依赖"进行拓扑排序。
        /// </summary>
        /// <param name="nodesCount">节点总数（从 0 到 n-1 编号）</param>
        /// <param name="dependsOn">
        /// 依赖关系：dependsOn[i] 中的每个节点都是 i 依赖的前驱，即它们必须先于 i 执行。
        /// 例如 dependsOn[b] = { a } 表示 a 必须先于 b。
        /// </param>
        /// <returns>排好序的节点列表（前驱在前），如果存在环则返回 null</returns>
        public static List<int> SortByDependencies(int nodesCount, List<int>[] dependsOn){
            if (dependsOn == null) throw new ArgumentNullException(nameof(dependsOn));
            if (nodesCount < 0) throw new ArgumentOutOfRangeException(nameof(nodesCount), "节点数量不能为负数。");
            if (dependsOn.Length != nodesCount){
                throw new ArgumentException($"dependsOn 长度 ({dependsOn.Length}) 与 nodesCount ({nodesCount}) 不一致。", nameof(dependsOn));
            }

            // 把"前驱依赖"反转成"后继关系"，再交给 Sort
            var successors = new List<int>[nodesCount];
            for (int i = 0; i < nodesCount; i++){
                if (dependsOn[i] == null) continue;
                foreach (var dependency in dependsOn[i]){
                    if (dependency < 0 || dependency >= nodesCount){
                        throw new ArgumentOutOfRangeException(nameof(dependsOn), $"节点编号 {dependency} 越界（节点范围 0..{nodesCount - 1}）。");
                    }
                    if (successors[dependency] == null) successors[dependency] = new List<int>();
                    successors[dependency].Add(i);
                }
            }

            return Sort(nodesCount, successors);
        }
    }
}
