using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Erame.Editor{
    /// <summary>
    /// 按需生成项目示例：先勾选这个项目真正需要的能力，再生成对应的契约、实现与模块注册。
    /// 不需要的服务从一开始就不生成，而不是生成整套再让开发者删。
    /// </summary>
    public sealed class ErameProjectGeneratorWindow : EditorWindow{
        private readonly HashSet<string> _selected = new HashSet<string>();
        private Vector2 _scroll;
        private bool _initialized;

        [MenuItem("Erame/示例/生成项目（按需）", priority = 10)]
        public static void Open(){
            var window = GetWindow<ErameProjectGeneratorWindow>(false, "按需生成项目", true);
            window.minSize = new Vector2(460, 560);
            window.Show();
        }

        private void OnEnable(){
            if (_initialized) return;
            ResetTo(ErameCapabilities.AllIds);
            _initialized = true;
        }

        private void ResetTo(IEnumerable<string> ids){
            _selected.Clear();
            foreach (var id in ids) _selected.Add(id);
        }

        private bool IsSelected(ErameCapability capability) =>
            capability.AlwaysOn || _selected.Contains(capability.Id);

        private void OnGUI(){
            EditorGUILayout.LabelField("按需生成项目", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "只勾选这个项目真正需要的能力：契约、实现、模块注册都只生成勾选项。\n" +
                "日志是基础设施，始终生成；勾选的能力会自动补齐它依赖的能力。\n" +
                "生成后这些文件归业务所有，可以继续修改，也可以整块删掉。",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope()){
                if (GUILayout.Button("全选")) ResetTo(ErameCapabilities.AllIds);
                if (GUILayout.Button("最小集")) ResetTo(ErameCapabilities.Minimal);
                if (GUILayout.Button("全不选")) ResetTo(Array.Empty<string>());
            }

            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            foreach (var group in ErameCapabilities.Groups){
                DrawGroup(group);
                EditorGUILayout.Space(6);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);

            var resolved = ErameCapabilities.Resolve(_selected);
            var autoIncluded = ErameCapabilities.AutoIncluded(_selected)
                .Select(id => ErameCapabilities.Find(id))
                .Where(c => c != null)
                .Select(c => c.Title)
                .ToList();

            if (autoIncluded.Count > 0){
                EditorGUILayout.HelpBox("将自动包含：" + string.Join("、", autoIncluded), MessageType.None);
            }

            if (ErameCapabilities.RequiresAddressables(resolved) && !ErameEntryGenerator.HasAddressablesPackage()){
                EditorGUILayout.HelpBox(
                    "选中的能力需要 com.unity.addressables，但工程 Packages/manifest.json 里没有这个包，" +
                    "生成后无法编译。请先在 Package Manager 里安装 Addressables。",
                    MessageType.Warning);
            }

            if (GUILayout.Button($"生成项目（{resolved.Count} 项能力）", GUILayout.Height(32))){
                ErameEntryGenerator.Generate(resolved);
            }
        }

        private void DrawGroup(ErameCapabilityGroup group){
            EditorGUILayout.LabelField(group.ClassName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(group.Summary, EditorStyles.miniLabel);

            foreach (var capability in ErameCapabilities.All.Where(c => c.Group == group.Id)){
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)){
                    var current = IsSelected(capability);
                    using (new EditorGUI.DisabledScope(capability.AlwaysOn)){
                        var next = GUILayout.Toggle(current, " " + capability.Title + (capability.AlwaysOn ? "（基础，必选）" : ""),
                            EditorStyles.boldLabel);
                        if (next != current){
                            if (next) _selected.Add(capability.Id);
                            else _selected.Remove(capability.Id);
                        }
                    }

                    EditorGUILayout.LabelField(capability.Description, EditorStyles.wordWrappedMiniLabel);

                    var missing = capability.DependsOn
                        .Where(id => !_selected.Contains(id) && !(ErameCapabilities.Find(id)?.AlwaysOn ?? false))
                        .Select(id => ErameCapabilities.Find(id)?.Title)
                        .Where(t => t != null)
                        .ToList();

                    if (missing.Count > 0){
                        EditorGUILayout.LabelField("需要一并生成：" + string.Join("、", missing), EditorStyles.miniLabel);
                    }
                }
            }
        }
    }
}
