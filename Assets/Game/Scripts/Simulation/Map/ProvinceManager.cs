using System.Collections.Generic;
using Erame.Core.Algorithms.Graph;
using Erame.Core;

namespace Game.Scripts.Simulation.Map{
    public class ProvinceManager{
        private readonly ILogService _log;
        
        // 假设地图有 100 个地块
        private const int MAP_TILE_COUNT = 100;
        
        // 并查集实例
        private UnionFind _provinceGraph;

        public ProvinceManager(ILogService log){
            _log = log;
            _provinceGraph = new UnionFind(MAP_TILE_COUNT);
        }

        // 模拟：玩家吞并了一块地，让它和原本的领地合并
        public void AnnexTile(int myTileId, int newTileId){
            // 业务逻辑：检查是否有相邻地块（这里省略边界判断）
            // 直接将新地块合并进我们的省份
            _provinceGraph.Union(myTileId, newTileId);
            
            _log.Info($"[Simulation] 地块 {newTileId} 已被吞并，现在与 {myTileId} 同属一个省份。");
        }

        // 判断某个地块是否属于我们的省份
        public bool IsTileOwned(int myTileId, int targetTileId){
            return _provinceGraph.IsConnected(myTileId, targetTileId);
        }
    }
}