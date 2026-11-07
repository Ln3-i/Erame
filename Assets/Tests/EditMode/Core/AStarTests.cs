using Game.Runtime;
using Game.Runtime.Services;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Erame.Core.Algorithms.Pathfinding;

namespace Tests{
    public class AStarTests{
        private static int[,] OpenGrid(int width, int height) => new int[width, height];

        private static void AssertValidPath(int[,] grid, List<(int x, int y)> path,
            (int x, int y) start, (int x, int y) goal){
            Assert.IsNotNull(path);
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);

            for (int i = 0; i < path.Count; i++){
                Assert.AreNotEqual(-1, grid[path[i].x, path[i].y], $"路径经过不可通行格 {path[i]}");
                if (i == 0) continue;
                int step = Math.Abs(path[i].x - path[i - 1].x) + Math.Abs(path[i].y - path[i - 1].y);
                Assert.AreEqual(1, step, $"路径第 {i} 步不相邻");
            }
        }

        [Test]
        public void FindPath_OpenGrid_ReturnsShortestPath(){
            var grid = OpenGrid(5, 5);
            var start = (0, 0);
            var goal = (4, 4);

            var path = AStar.FindPath(grid, start, goal);

            AssertValidPath(grid, path, start, goal);
            Assert.AreEqual(9, path.Count, "5x5 曼哈顿路径应为 8 步 + 起点");
        }

        [Test]
        public void FindPath_WallWithGap_RoutesThroughGap(){
            var grid = OpenGrid(5, 5);
            for (int y = 0; y <= 3; y++) grid[2, y] = -1; // x=2 列只留 y=4 缺口

            var start = (0, 0);
            var goal = (4, 4);
            var path = AStar.FindPath(grid, start, goal);

            AssertValidPath(grid, path, start, goal);
            CollectionAssert.Contains(path, (2, 4), "必须从唯一缺口穿过");
        }

        [Test]
        public void FindPath_SolidWall_ReturnsNull(){
            var grid = OpenGrid(5, 5);
            for (int y = 0; y < 5; y++) grid[2, y] = -1; // 封死整列

            Assert.IsNull(AStar.FindPath(grid, (0, 0), (4, 4)));
        }

        [Test]
        public void FindPath_StartEqualsGoal_ReturnsSingleCell(){
            var grid = OpenGrid(3, 3);
            var path = AStar.FindPath(grid, (1, 1), (1, 1));

            Assert.AreEqual(1, path.Count);
            Assert.AreEqual((1, 1), path[0]);
        }

        [Test]
        public void FindPath_NullGrid_Throws(){
            Assert.Throws<ArgumentNullException>(() => AStar.FindPath(null, (0, 0), (1, 1)));
        }

        [Test]
        public void FindPath_StartOutOfBounds_Throws(){
            var grid = OpenGrid(3, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => AStar.FindPath(grid, (-1, 0), (2, 2)));
        }

        [Test]
        public void FindPath_GoalOutOfBounds_Throws(){
            var grid = OpenGrid(3, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => AStar.FindPath(grid, (0, 0), (3, 3)));
        }
    }
}
