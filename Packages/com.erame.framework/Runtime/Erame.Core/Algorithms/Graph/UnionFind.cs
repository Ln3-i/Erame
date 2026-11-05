namespace Erame.Core.Algorithms.Graph{
    /// <summary>
    /// 并查集（Union-Find）：用于高效处理动态连通性问题。
    /// </summary>
    public class UnionFind{
        private readonly int[] _parent;
        private readonly int[] _rank;

        public UnionFind(int size){
            _parent = new int[size];
            _rank = new int[size];
            for (int i = 0; i < size; i++){
                _parent[i] = i; // 初始时，每个节点的父节点是自己
                _rank[i] = 1;
            }
        }

        // 查找节点 x 的根节点（带路径压缩，极快）
        public int Find(int x){
            if (_parent[x] != x){
                _parent[x] = Find(_parent[x]); // 路径压缩
            }
            return _parent[x];
        }

        // 合并两个节点所在的集合
        public void Union(int x, int y){
            int rootX = Find(x);
            int rootY = Find(y);

            if (rootX == rootY) return; // 已经在同一集合

            // 按秩合并（把矮的树挂到高的树下，保持树的平衡）
            if (_rank[rootX] > _rank[rootY]){
                _parent[rootY] = rootX;
            }
            else if (_rank[rootX] < _rank[rootY]){
                _parent[rootX] = rootY;
            }
            else{
                _parent[rootY] = rootX;
                _rank[rootX]++;
            }
        }

        // 判断两个节点是否连通（是否属于同一个省份）
        public bool IsConnected(int x, int y){
            return Find(x) == Find(y);
        }
    }
}