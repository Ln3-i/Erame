using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Erame.Editor{
    /// <summary>
    /// 最小入口生成器：生成标准入口（GameEntry）+ 入口提供者（GameEntryProvider）+
    /// 提供者资产 + 挂好 ErameBootstrap 的场景（Game.unity）。
    /// 默认生成到 Assets/Game；目标已存在时提示覆盖或取消。
    /// </summary>
    public static class ErameEntryGenerator{
        private const string GameRoot = "Assets/Game";
        private const string BootstrapScriptPath =
            "Packages/com.erame.framework/Runtime/Erame.Runtime/Bootstrap/ErameBootstrap.cs";

        private const string EntryFileName = "GameEntry.cs";
        private const string ProviderFileName = "GameEntryProvider.cs";
        private const string AssetFileName = "GameEntryProvider.asset";
        private const string SceneFileName = "Game.unity";

        // 场景对象 fileID（避开模板中的 1~4 / 70550799x / 96319422x）
        private const long BootstrapGoId = 1948573001;
        private const long BootstrapTransformId = 1948573002;
        private const long BootstrapComponentId = 1948573003;

        private const string EntryTemplate =
@"using Erame.Core;

namespace __NS__{
    /// <summary>
    /// 标准入口：整个项目只需重写这里即可启动。
    /// Configure 负责业务注册（框架默认服务已由 ErameRuntime 注册），
    /// OnStart / OnUpdate / OnQuit 负责生命周期收尾。
    /// </summary>
    public sealed class GameEntry : ErameEntry{
        public override void Configure(ServiceContainer container){
            // 业务自定义服务在此注册：
            // container.RegisterSingleton<IMyService>(p => new MyService(...));
        }

        public override void OnStart(){
            // 启动完成后的业务初始化：
        }
    }
}
";

        private const string ProviderTemplate =
@"using Erame.Core;
using Erame.Runtime.Bootstrap;
using Erame.Runtime.Config;
using UnityEngine;

namespace __NS__{
    /// <summary>
    /// 入口提供者资产：持有配置并创建 GameEntry。
    /// 场景中的 ErameBootstrap 引用本资产来启动整个项目（不使用反射）。
    /// </summary>
    [CreateAssetMenu(fileName = ""GameEntryProvider"", menuName = ""Erame/Entry Provider"")]
    public sealed class GameEntryProvider : ErameEntryProvider{
        [Tooltip(""可留空，留空时使用框架默认配置"")]
        [SerializeField] private ErameSettings settings;

        public override ErameEntry Create() => new GameEntry();

        public override void Configure(ErameBuilder builder){
            builder.WithSettings(settings);
        }
    }
}
";

        [MenuItem("Erame/生成最小入口", priority = 14)]
        public static void Generate(){
            // 1. 已存在 → 提示覆盖 / 取消
            if (Directory.Exists(GameRoot)){
                var existing = CollectExistingOutputs();
                if (existing.Count > 0){
                    var confirmed = EditorUtility.DisplayDialog(
                        "Erame 生成最小入口",
                        "目标已存在，以下文件将被覆盖：\n\n" + string.Join("\n", existing) +
                        "\n\n其他文件不受影响。是否覆盖？",
                        "覆盖",
                        "取消");
                    if (!confirmed) return;
                }
            }

            // 2. 目录架构
            EnsureFolder(GameRoot);
            EnsureFolder(GameRoot + "/Scripts");
            EnsureFolder(GameRoot + "/Scripts/App");
            EnsureFolder(GameRoot + "/Scenes");

            var rootName = Path.GetFileName(GameRoot);
            var ns = ToNamespace(rootName) + ".Scripts.App";

            var entryPath = GameRoot + "/Scripts/App/" + EntryFileName;
            var providerPath = GameRoot + "/Scripts/App/" + ProviderFileName;
            var assetPath = GameRoot + "/" + AssetFileName;
            var scenePath = GameRoot + "/Scenes/" + SceneFileName;

            // 3. 写脚本（连同自造的 meta，保证 GUID 确定，便于资产/场景引用）
            var providerGuid = WriteTextWithMeta(providerPath, ProviderTemplate.Replace("__NS__", ns), NewGuid());
            WriteTextWithMeta(entryPath, EntryTemplate.Replace("__NS__", ns), NewGuid());

            // 4. 提供者资产
            var assetGuid = NewGuid();
            WriteTextWithMeta(assetPath, BuildAssetYaml(providerGuid, Path.GetFileNameWithoutExtension(AssetFileName)), assetGuid);

            // 5. 场景（内嵌 ErameBootstrap + Provider 引用）
            var bootstrapGuid = ReadPackageGuid(BootstrapScriptPath);
            if (string.IsNullOrEmpty(bootstrapGuid)){
                Debug.LogError("[Erame] 无法读取 ErameBootstrap 脚本 GUID，生成中止。");
                return;
            }

            WriteTextWithMeta(scenePath, BuildSceneYaml(bootstrapGuid, assetGuid), NewGuid());

            AssetDatabase.Refresh();

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset != null){
                Selection.activeObject = sceneAsset;
                EditorGUIUtility.PingObject(sceneAsset);
            }

            Debug.Log(
                "[Erame] 已生成最小入口：\n" +
                $"  入口: {entryPath}\n" +
                $"  提供者: {providerPath}\n" +
                $"  资产: {assetPath}\n" +
                $"  场景: {scenePath}（ErameBootstrap 已挂载并引用提供者资产）");
        }

        private static List<string> CollectExistingOutputs(){
            var list = new List<string>();
            string[] paths = {
                GameRoot + "/Scripts/App/" + EntryFileName,
                GameRoot + "/Scripts/App/" + ProviderFileName,
                GameRoot + "/" + AssetFileName,
                GameRoot + "/Scenes/" + SceneFileName
            };
            foreach (var path in paths){
                if (File.Exists(path)) list.Add(path);
            }

            return list;
        }

        private static void EnsureFolder(string path){
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string WriteTextWithMeta(string assetPath, string content, string guid){
            File.WriteAllText(assetPath, content, new UTF8Encoding(false));

            var metaPath = assetPath + ".meta";
            var importer = GetImporterBlock(assetPath);
            File.WriteAllText(metaPath,
                $"fileFormatVersion: 2\nguid: {guid}\n{importer}", new UTF8Encoding(false));
            return guid;
        }

        private static string GetImporterBlock(string assetPath){
            if (assetPath.EndsWith(".cs")){
                return "MonoImporter:\n" +
                       "  externalObjects: {}\n" +
                       "  serializedVersion: 2\n" +
                       "  defaultReferences: []\n" +
                       "  executionOrder: 0\n" +
                       "  icon: {instanceID: 0}\n" +
                       "  userData: \n" +
                       "  assetBundleName: \n" +
                       "  assetBundleVariant: \n";
            }

            if (assetPath.EndsWith(".asset")){
                return "NativeFormatImporter:\n" +
                       "  externalObjects: {}\n" +
                       "  mainObjectFileID: 11400000\n" +
                       "  userData: \n" +
                       "  assetBundleName: \n" +
                       "  assetBundleVariant: \n";
            }

            return "DefaultImporter:\n" +
                   "  externalObjects: {}\n" +
                   "  userData: \n" +
                   "  assetBundleName: \n" +
                   "  assetBundleVariant: \n";
        }

        private static string BuildAssetYaml(string providerScriptGuid, string assetName){
            return "%YAML 1.1\n" +
                   "%TAG !u! tag:unity3d.com,2011:\n" +
                   "--- !u!114 &11400000\n" +
                   "MonoBehaviour:\n" +
                   "  m_ObjectHideFlags: 0\n" +
                   "  m_CorrespondingSourceObject: {fileID: 0}\n" +
                   "  m_PrefabInstance: {fileID: 0}\n" +
                   "  m_PrefabAsset: {fileID: 0}\n" +
                   "  m_GameObject: {fileID: 0}\n" +
                   "  m_Enabled: 1\n" +
                   "  m_EditorHideFlags: 0\n" +
                   $"  m_Script: {{fileID: 11500000, guid: {providerScriptGuid}, type: 3}}\n" +
                   $"  m_Name: {assetName}\n" +
                   "  m_EditorClassIdentifier: \n" +
                   "  settings: {fileID: 0}\n";
        }

        private static string BuildSceneYaml(string bootstrapScriptGuid, string providerAssetGuid){
            var sb = new StringBuilder(4096);
            sb.Append(
                "%YAML 1.1\n" +
                "%TAG !u! tag:unity3d.com,2011:\n" +
                "--- !u!29 &1\n" +
                "OcclusionCullingSettings:\n" +
                "  m_ObjectHideFlags: 0\n" +
                "  serializedVersion: 2\n" +
                "  m_OcclusionBakeSettings:\n" +
                "    smallestOccluder: 5\n" +
                "    smallestHole: 0.25\n" +
                "    backfaceThreshold: 100\n" +
                "  m_SceneGUID: 00000000000000000000000000000000\n" +
                "  m_OcclusionCullingData: {fileID: 0}\n" +
                "--- !u!104 &2\n" +
                "RenderSettings:\n" +
                "  m_ObjectHideFlags: 0\n" +
                "  serializedVersion: 9\n" +
                "  m_Fog: 0\n" +
                "  m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}\n" +
                "  m_FogMode: 3\n" +
                "  m_FogDensity: 0.01\n" +
                "  m_LinearFogStart: 0\n" +
                "  m_LinearFogEnd: 300\n" +
                "  m_AmbientSkyColor: {r: 0.212, g: 0.227, b: 0.259, a: 1}\n" +
                "  m_AmbientEquatorColor: {r: 0.114, g: 0.125, b: 0.133, a: 1}\n" +
                "  m_AmbientGroundColor: {r: 0.047, g: 0.043, b: 0.035, a: 1}\n" +
                "  m_AmbientIntensity: 1\n" +
                "  m_AmbientMode: 0\n" +
                "  m_SubtractiveShadowColor: {r: 0.42, g: 0.478, b: 0.627, a: 1}\n" +
                "  m_SkyboxMaterial: {fileID: 10304, guid: 0000000000000000f000000000000000, type: 0}\n" +
                "  m_HaloStrength: 0.5\n" +
                "  m_FlareStrength: 1\n" +
                "  m_FlareFadeSpeed: 3\n" +
                "  m_HaloTexture: {fileID: 0}\n" +
                "  m_SpotCookie: {fileID: 10001, guid: 0000000000000000e000000000000000, type: 0}\n" +
                "  m_DefaultReflectionMode: 0\n" +
                "  m_DefaultReflectionResolution: 128\n" +
                "  m_ReflectionBounces: 1\n" +
                "  m_ReflectionIntensity: 1\n" +
                "  m_CustomReflection: {fileID: 0}\n" +
                "  m_Sun: {fileID: 0}\n" +
                "  m_IndirectSpecularColor: {r: 0, g: 0, b: 0, a: 1}\n" +
                "  m_UseRadianceAmbientProbe: 0\n" +
                "--- !u!157 &3\n" +
                "LightmapSettings:\n" +
                "  m_ObjectHideFlags: 0\n" +
                "  serializedVersion: 12\n" +
                "  m_GIWorkflowMode: 1\n" +
                "  m_GISettings:\n" +
                "    serializedVersion: 2\n" +
                "    m_BounceScale: 1\n" +
                "    m_IndirectOutputScale: 1\n" +
                "    m_AlbedoBoost: 1\n" +
                "    m_EnvironmentLightingMode: 0\n" +
                "    m_EnableBakedLightmaps: 1\n" +
                "    m_EnableRealtimeLightmaps: 0\n" +
                "  m_LightmapEditorSettings:\n" +
                "    serializedVersion: 12\n" +
                "    m_Resolution: 2\n" +
                "    m_BakeResolution: 40\n" +
                "    m_AtlasSize: 1024\n" +
                "    m_AO: 0\n" +
                "    m_AOMaxDistance: 1\n" +
                "    m_CompAOExponent: 1\n" +
                "    m_CompAOExponentDirect: 0\n" +
                "    m_ExtractAmbientOcclusion: 0\n" +
                "    m_Padding: 2\n" +
                "    m_LightmapParameters: {fileID: 0}\n" +
                "    m_LightmapsBakeMode: 1\n" +
                "    m_TextureCompression: 1\n" +
                "    m_FinalGather: 0\n" +
                "    m_FinalGatherFiltering: 1\n" +
                "    m_FinalGatherRayCount: 256\n" +
                "    m_ReflectionCompression: 2\n" +
                "    m_MixedBakeMode: 2\n" +
                "    m_BakeBackend: 1\n" +
                "    m_PVRSampling: 1\n" +
                "    m_PVRDirectSampleCount: 32\n" +
                "    m_PVRSampleCount: 500\n" +
                "    m_PVRBounces: 2\n" +
                "    m_PVREnvironmentSampleCount: 500\n" +
                "    m_PVREnvironmentReferencePointCount: 2048\n" +
                "    m_PVRFilteringMode: 2\n" +
                "    m_PVRDenoiserTypeDirect: 0\n" +
                "    m_PVRDenoiserTypeIndirect: 0\n" +
                "    m_PVRDenoiserTypeAO: 0\n" +
                "    m_PVRFilterTypeDirect: 0\n" +
                "    m_PVRFilterTypeIndirect: 0\n" +
                "    m_PVRFilterTypeAO: 0\n" +
                "    m_PVREnvironmentMIS: 0\n" +
                "    m_PVRCulling: 1\n" +
                "    m_PVRFilteringGaussRadiusDirect: 1\n" +
                "    m_PVRFilteringGaussRadiusIndirect: 5\n" +
                "    m_PVRFilteringGaussRadiusAO: 2\n" +
                "    m_PVRFilteringAtrousPositionSigmaDirect: 0.5\n" +
                "    m_PVRFilteringAtrousPositionSigmaIndirect: 2\n" +
                "    m_PVRFilteringAtrousPositionSigmaAO: 1\n" +
                "    m_ExportTrainingData: 0\n" +
                "    m_TrainingDataDestination: TrainingData\n" +
                "    m_LightProbeSampleCountMultiplier: 4\n" +
                "  m_LightingDataAsset: {fileID: 0}\n" +
                "  m_LightingSettings: {fileID: 0}\n" +
                "--- !u!196 &4\n" +
                "NavMeshSettings:\n" +
                "  serializedVersion: 2\n" +
                "  m_ObjectHideFlags: 0\n" +
                "  m_BuildSettings:\n" +
                "    serializedVersion: 2\n" +
                "    agentTypeID: 0\n" +
                "    agentRadius: 0.5\n" +
                "    agentHeight: 2\n" +
                "    agentSlope: 45\n" +
                "    agentClimb: 0.4\n" +
                "    ledgeDropHeight: 0\n" +
                "    maxJumpAcrossDistance: 0\n" +
                "    minRegionArea: 2\n" +
                "    manualCellSize: 0\n" +
                "    cellSize: 0.16666667\n" +
                "    manualTileSize: 0\n" +
                "    tileSize: 256\n" +
                "    accuratePlacement: 0\n" +
                "    debug:\n" +
                "      m_Flags: 0\n" +
                "  m_NavMeshData: {fileID: 0}\n");

            // ErameBootstrap 启动对象
            sb.Append($"--- !u!1 &{BootstrapGoId}\n" +
                      "GameObject:\n" +
                      "  m_ObjectHideFlags: 0\n" +
                      "  m_CorrespondingSourceObject: {fileID: 0}\n" +
                      "  m_PrefabInstance: {fileID: 0}\n" +
                      "  m_PrefabAsset: {fileID: 0}\n" +
                      "  serializedVersion: 6\n" +
                      "  m_Component:\n" +
                      $"  - component: {{fileID: {BootstrapTransformId}}}\n" +
                      $"  - component: {{fileID: {BootstrapComponentId}}}\n" +
                      "  m_Layer: 0\n" +
                      "  m_Name: ErameBootstrap\n" +
                      "  m_TagString: Untagged\n" +
                      "  m_Icon: {fileID: 0}\n" +
                      "  m_NavMeshLayer: 0\n" +
                      "  m_StaticEditorFlags: 0\n" +
                      "  m_IsActive: 1\n");

            sb.Append($"--- !u!4 &{BootstrapTransformId}\n" +
                      "Transform:\n" +
                      "  m_ObjectHideFlags: 0\n" +
                      "  m_CorrespondingSourceObject: {fileID: 0}\n" +
                      "  m_PrefabInstance: {fileID: 0}\n" +
                      "  m_PrefabAsset: {fileID: 0}\n" +
                      $"  m_GameObject: {{fileID: {BootstrapGoId}}}\n" +
                      "  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n" +
                      "  m_LocalPosition: {x: 0, y: 0, z: 0}\n" +
                      "  m_LocalScale: {x: 1, y: 1, z: 1}\n" +
                      "  m_Children: []\n" +
                      "  m_Father: {fileID: 0}\n" +
                      "  m_RootOrder: 0\n" +
                      "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n");

            sb.Append($"--- !u!114 &{BootstrapComponentId}\n" +
                      "MonoBehaviour:\n" +
                      "  m_ObjectHideFlags: 0\n" +
                      "  m_CorrespondingSourceObject: {fileID: 0}\n" +
                      "  m_PrefabInstance: {fileID: 0}\n" +
                      "  m_PrefabAsset: {fileID: 0}\n" +
                      $"  m_GameObject: {{fileID: {BootstrapGoId}}}\n" +
                      "  m_Enabled: 1\n" +
                      "  m_EditorHideFlags: 0\n" +
                      $"  m_Script: {{fileID: 11500000, guid: {bootstrapScriptGuid}, type: 3}}\n" +
                      "  m_Name: \n" +
                      "  m_EditorClassIdentifier: \n" +
                      $"  provider: {{fileID: 11500000, guid: {providerAssetGuid}, type: 2}}\n" +
                      "  dontDestroyOnLoad: 1\n");

            return sb.ToString();
        }

        private static string ReadPackageGuid(string packageFilePath){
            var fullPath = Path.GetFullPath(packageFilePath);
            if (!File.Exists(fullPath)) return null;

            var meta = File.ReadAllText(fullPath + ".meta");
            var match = Regex.Match(meta, @"guid:\s*([0-9a-f]{32})");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string NewGuid() => Guid.NewGuid().ToString("N");

        private static string ToNamespace(string folderName){
            var clean = Regex.Replace(folderName, "[^A-Za-z0-9_]", "");
            if (string.IsNullOrEmpty(clean)) return "MyGame";
            if (char.IsDigit(clean[0])) clean = "_" + clean;
            return clean;
        }
    }
}
