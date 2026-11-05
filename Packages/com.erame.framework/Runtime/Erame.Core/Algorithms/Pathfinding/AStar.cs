using System;
using System.Collections.Generic;
using Erame.Core.Algorithms.Collections;

namespace Erame.Core.Algorithms.Pathfinding{
    public static class AStar{
        /// <summary>
        /// 在四向网格上寻找从 start 到 goal 的最短路径。
        /// </summary>
        /// <param name="grid">网格，下标为 [x, y]；值为 -1 表示不可通行。</param>
        /// <param name="start">起点坐标。</param>
        /// <param name="goal">终点坐标。</param>
        /// <returns>从起点到终点的坐标列表；无路径时返回 null。</returns>
        /// <exception cref="ArgumentNullException">grid 为 null。</exception>
        /// <exception cref="ArgumentOutOfRangeException">起点或终点越界。</exception>
        public static List<(int x, int y)> FindPath(int[,] grid, (int x, int y) start, (int x, int y) goal){
            if (grid == null) throw new ArgumentNullException(nameof(grid));

            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            if (width == 0 || height == 0) return null;

            if (!InBounds(start, width, height)) throw new ArgumentOutOfRangeException(nameof(start), $"起点 {start} 越界（网格 {width}x{height}）。");
            if (!InBounds(goal, width, height)) throw new ArgumentOutOfRangeException(nameof(goal), $"终点 {goal} 越界（网格 {width}x{height}）。");

            var openList = new PriorityQueue<(int x, int y), float>();
            // 已确定最短距离的节点集合：防止同一节点被多次展开
            var closed = new HashSet<(int x, int y)>();
            var cameFrom = new Dictionary<(int x, int y), (int x, int y)>();
            var gScore = new Dictionary<(int x, int y), float>();

            gScore[start] = 0;
            openList.Enqueue(start, Heuristic(start, goal));

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            while (openList.TryDequeue(out var current)){
                if (!closed.Add(current)) continue; // 已经以更优代价展开过，跳过旧条目

                if (current == goal){
                    return ReconstructPath(cameFrom, current);
                }

                for (int i = 0; i < 4; i++){
                    (int x, int y) neighbor = (current.x + dx[i], current.y + dy[i]);

                    if (!InBounds(neighbor, width, height)) continue;
                    if (closed.Contains(neighbor)) continue;
                    if (grid[neighbor.x, neighbor.y] == -1) continue;

                    float tentativeGScore = gScore[current] + 1f;

                    if (!gScore.TryGetValue(neighbor, out var knownG) || tentativeGScore < knownG){
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        openList.Enqueue(neighbor, tentativeGScore + Heuristic(neighbor, goal));
                    }
                }
            }
            return null;
        }

        private static bool InBounds((int x, int y) p, int width, int height){
            return p.x >= 0 && p.x < width && p.y >= 0 && p.y < height;
        }

        private static float Heuristic((int x, int y) a, (int x, int y) b){
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        private static List<(int x, int y)> ReconstructPath(Dictionary<(int x, int y), (int x, int y)> cameFrom, (int x, int y) current){
            var path = new List<(int x, int y)> { current };
            while (cameFrom.ContainsKey(current)){
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }
    }
}
