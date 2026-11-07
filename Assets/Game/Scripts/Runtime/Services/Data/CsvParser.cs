using Game.Runtime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Game.Runtime.Services{
    public static class CsvParser{
        // 解析 CSV 文本并生成 DataTable。
        // 不使用反射：行对象由 rowFactory 创建，单元格赋值由 applyColumn 显式完成。
        public static DataTable<T> Parse<T>(
            string csvText,
            ILogService logger,
            Func<T> rowFactory,
            Action<T, string, string> applyColumn,
            char sep = ',') where T : class, IData{
            if (rowFactory == null) throw new ArgumentNullException(nameof(rowFactory));
            if (applyColumn == null) throw new ArgumentNullException(nameof(applyColumn));

            var table = new DataTable<T>();
            if (string.IsNullOrWhiteSpace(csvText)) return table;

            var rows = Tokenize(csvText, sep);
            if (rows.Count < 2) return table;

            var headers = rows[0];

            for (int i = 1; i < rows.Count; i++){
                var cells = rows[i];
                if (cells.Length == 1 && string.IsNullOrWhiteSpace(cells[0])) continue; // 跳过空行
                if (cells.Length == 0) continue;

                var row = rowFactory();
                if (row == null) continue;

                for (int c = 0; c < cells.Length && c < headers.Length; c++){
                    var raw = cells[c].Trim();
                    try{
                        applyColumn(row, headers[c].Trim(), raw);
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

        /// <summary>把单元格文本转换为目标类型（供 applyColumn 使用）。</summary>
        public static object ConvertValue(string raw, Type t){
            if (t == typeof(string)) return raw;
            // 空单元格：常见值类型返回默认值，其余返回 null
            if (raw.Length == 0){
                if (t == typeof(int)) return 0;
                if (t == typeof(float)) return 0f;
                if (t == typeof(double)) return 0d;
                if (t == typeof(long)) return 0L;
                if (t == typeof(bool)) return false;
                if (t.IsEnum) return Enum.GetValues(t).GetValue(0);
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
