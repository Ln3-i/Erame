using System;
using System.Collections.Generic;

namespace __NS__.Runtime{
    // 泛型数据表，提供按 Id 查找的能力
    public sealed class DataTable<T> where T : class, IData{
        private readonly Dictionary<int, T> _byId = new Dictionary<int, T>(256);
        private readonly List<T> _ordered = new List<T>(256);

        public int Count => _ordered.Count;
        public IReadOnlyList<T> All => _ordered;

        public void Add(T row){
            if (row == null) throw new ArgumentNullException(nameof(row));
            if (_byId.ContainsKey(row.Id))
                throw new InvalidOperationException($"数据表中存在重复的 ID: {row.Id}");

            _byId[row.Id] = row;
            _ordered.Add(row);
        }

        public T Get(int id){
            return _byId.TryGetValue(id, out var r) ? r : null;
        }

        public bool TryGet(int id, out T row){
            return _byId.TryGetValue(id, out row);
        }
    }
}