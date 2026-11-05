using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Erame.Core{
    public static class CsvParser{
        // 解析 CSV 文本并生成 DataTable
        public static DataTable<T> Parse<T>(string csvText, ILogService logger, char sep = ',') where T : class, IData, new(){
            var table = new DataTable<T>();
            if (string.IsNullOrWhiteSpace(csvText)) return table;

            var rows = Tokenize(csvText, sep);
            if (rows.Count < 2) return table;

            var headers = rows[0];
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // 建立列与属性的映射
            var map = new PropertyInfo[headers.Length];
            for (int c = 0; c < headers.Length; c++){
                var name = headers[c].Trim();
                for (int p = 0; p < props.Length; p++){
                    if (string.Equals(props[p].Name, name, StringComparison.OrdinalIgnoreCase)){
                        map[c] = props[p];
                        break;
                    }
                }
            }

            for (int i = 1; i < rows.Count; i++){
                var cells = rows[i];
                if (cells.Length == 1 && string.IsNullOrWhiteSpace(cells[0])) continue; // 跳过空行
                if (cells.Length == 0) continue;

                var row = new T();

                for (int c = 0; c < cells.Length && c < map.Length; c++){
                    var prop = map[c];
                    if (prop == null || !prop.CanWrite) continue;

                    var raw = cells[c].Trim();
                    try{
                        prop.SetValue(row, ConvertValue(raw, prop.PropertyType));
                    }
                    catch (Exception ex){
                        logger.Warn($"[CsvParser] 第 {i + 1} 行，列 '{headers[c]}' 解析失败: {ex.Message}");
                    }
                }

                try{
                    table.Add(row);
                }
                catch (Exception ex){
                    logger.Warn($"[CsvParser] 第 {i + 1} 行添加失败: {ex.Message}");
                }
            }

            return table;
        }

        /// <summary>
        /// 按 RFC 4180 切分 CSV 文本，支持引号包裹的字段（字段内可含分隔符、换行、"" 转义）。
        /// 未闭合的引号视为文件损坏，抛出 <see cref="InvalidDataException"/>。
        /// </summary>
        private static List<string[]> Tokenize(string text, char sep){
            var rows = new List<string[]>();
            var cells = new List<string>();
            var sb = new StringBuilder();

            bool inQuotes = false;
            bool rowStarted = false;
            int i = 0;

            while (i < text.Length){
                char ch = text[i];

                if (inQuotes){
                    if (ch == '"'){
                        // 连续两个引号表示一个转义的引号字符
                        if (i + 1 < text.Length && text[i + 1] == '"'){
                            sb.Append('"');
                            i += 2;
                            continue;
                        }
                        inQuotes = false;
                        i++;
                        continue;
                    }
                    sb.Append(ch);
                    i++;
                    continue;
                }

                if (ch == '"' && sb.Length == 0){
                    inQuotes = true;
                    rowStarted = true;
                    i++;
                    continue;
                }

                if (ch == sep){
                    cells.Add(sb.ToString());
                    sb.Clear();
                    rowStarted = true;
                    i++;
                    continue;
                }

                if (ch == '\r' || ch == '\n'){
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    cells.Add(sb.ToString());
                    sb.Clear();
                    rows.Add(cells.ToArray());
                    cells.Clear();
                    rowStarted = false;
                    i++;
                    continue;
                }

                sb.Append(ch);
                rowStarted = true;
                i++;
            }

            if (inQuotes){
                throw new InvalidDataException("CSV 存在未闭合的引号，文件可能已损坏。");
            }

            // 最后一行没有以换行结尾时补上
            if (rowStarted || sb.Length > 0 || cells.Count > 0){
                cells.Add(sb.ToString());
                rows.Add(cells.ToArray());
            }

            return rows;
        }

        private static object ConvertValue(string raw, Type t){
            if (t == typeof(string)) return raw;
            // 空单元格：值类型返回默认值（如 0 / false / 枚举第 0 项），引用类型返回 null
            if (raw.Length == 0){
                if (t.IsValueType) return Activator.CreateInstance(t);
                return null;
            }
            if (t == typeof(int)) return int.Parse(raw, CultureInfo.InvariantCulture);
            if (t == typeof(float)) return float.Parse(raw, CultureInfo.InvariantCulture);
            if (t == typeof(bool)) return raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (t.IsEnum) return Enum.Parse(t, raw, true);
            return Convert.ChangeType(raw, t, CultureInfo.InvariantCulture);
        }
    }
}
