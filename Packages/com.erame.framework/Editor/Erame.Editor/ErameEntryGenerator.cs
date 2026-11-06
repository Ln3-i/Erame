using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Erame.Editor{
    /// <summary>
    /// 一键生成最小项目示例：一个 GameEntry（继承 ErameMonoEntry）+ 一个空场景（已挂好组件）。
    /// 全程只写文件与 meta，不使用反射。
    /// </summary>
    public static class ErameEntryGenerator{
        private const string GameRoot = "Assets/Game";
        private const string EntryFileName = "GameEntry.cs";
        private const string SceneFileName = "Game.unity";

        // 场景对象 fileID（避开场景默认的 1~4）
        private const long EntryGoId = 1948573001;
        private const long EntryTransformId = 1948573002;
        private const long EntryComponentId = 1948573003;

        private const string EntryTemplate =
@"using Erame.Core;
using Erame.Runtime.Bootstrap;

namespace __NS__{
    /// <summary>
    /// 项目入口：把本组件挂到场景对象上即可启动整个项目。
    /// 业务只需在 Configure 里注册服务/模块，可选重写 OnStart / OnUpdate / OnQuit。
    /// </summary>
    public sealed class GameEntry : ErameMonoEntry{
        public override void Configure(ErameBuilder builder){
            // builder.RegisterSingleton<IMyService>(p => new MyService());
            // builder.AddModule<MyModule>();
        }
    }
}
";

        [MenuItem("Erame/示例/生成项目示例", priority = 11)]
        public static void Generate(){
            var entryPath = GameRoot + "/" + EntryFileName;
            var scenePath = GameRoot + "/Scenes/" + SceneFileName;

            var existing = new List<string>();
            if (File.Exists(entryPath)) existing.Add(entryPath);
            if (File.Exists(scenePath)) existing.Add(scenePath);

            if (existing.Count > 0){
                var confirmed = EditorUtility.DisplayDialog(
                    "Erame 生成项目示例",
                    "目标已存在，以下文件将被覆盖：\n\n" + string.Join("\n", existing) +
                    "\n\n其他文件不受影响。是否覆盖？",
                    "覆盖",
                    "取消");
                if (!confirmed) return;
            }

            EnsureFolder(GameRoot);
            EnsureFolder(GameRoot + "/Scenes");

            var ns = ToNamespace(Path.GetFileName(GameRoot));
            var entryGuid = WriteTextWithMeta(entryPath, EntryTemplate.Replace("__NS__", ns));

            if (!File.Exists(scenePath + ".meta") || File.Exists(scenePath)){
                WriteTextWithMeta(scenePath, BuildSceneYaml(entryGuid));
            }

            AssetDatabase.Refresh();

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset != null){
                Selection.activeObject = sceneAsset;
                EditorGUIUtility.PingObject(sceneAsset);
            }

            Debug.Log(
                "[Erame] 已生成项目示例：\n" +
                $"  入口: {entryPath}\n" +
                $"  场景: {scenePath}（已挂载 GameEntry 组件）");
        }

        private static void EnsureFolder(string path){
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string WriteTextWithMeta(string assetPath, string content){
            var guid = Guid.NewGuid().ToString("N");
            File.WriteAllText(assetPath, content, new UTF8Encoding(false));

            var importer = assetPath.EndsWith(".cs")
                ? "MonoImporter:\n" +
                  "  externalObjects: {}\n" +
                  "  serializedVersion: 2\n" +
                  "  defaultReferences: []\n" +
                  "  executionOrder: 0\n" +
                  "  icon: {instanceID: 0}\n" +
                  "  userData: \n" +
                  "  assetBundleName: \n" +
                  "  assetBundleVariant: \n"
                : "DefaultImporter:\n" +
                  "  externalObjects: {}\n" +
                  "  userData: \n" +
                  "  assetBundleName: \n" +
                  "  assetBundleVariant: \n";

            File.WriteAllText(assetPath + ".meta",
                $"fileFormatVersion: 2\nguid: {guid}\n{importer}", new UTF8Encoding(false));
            return guid;
        }

        private static string BuildSceneYaml(string entryScriptGuid){
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

            sb.Append($"--- !u!1 &{EntryGoId}\n" +
                      "GameObject:\n" +
                      "  m_ObjectHideFlags: 0\n" +
                      "  m_CorrespondingSourceObject: {fileID: 0}\n" +
                      "  m_PrefabInstance: {fileID: 0}\n" +
                      "  m_PrefabAsset: {fileID: 0}\n" +
                      "  serializedVersion: 6\n" +
                      "  m_Component:\n" +
                      $"  - component: {{fileID: {EntryTransformId}}}\n" +
                      $"  - component: {{fileID: {EntryComponentId}}}\n" +
                      "  m_Layer: 0\n" +
                      "  m_Name: GameEntry\n" +
                      "  m_TagString: Untagged\n" +
                      "  m_Icon: {fileID: 0}\n" +
                      "  m_NavMeshLayer: 0\n" +
                      "  m_StaticEditorFlags: 0\n" +
                      "  m_IsActive: 1\n");

            sb.Append($"--- !u!4 &{EntryTransformId}\n" +
                      "Transform:\n" +
                      "  m_ObjectHideFlags: 0\n" +
                      "  m_CorrespondingSourceObject: {fileID: 0}\n" +
                      "  m_PrefabInstance: {fileID: 0}\n" +
                      "  m_PrefabAsset: {fileID: 0}\n" +
                      $"  m_GameObject: {{fileID: {EntryGoId}}}\n" +
                      "  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n" +
                      "  m_LocalPosition: {x: 0, y: 0, z: 0}\n" +
                      "  m_LocalScale: {x: 1, y: 1, z: 1}\n" +
                      "  m_Children: []\n" +
                      "  m_Father: {fileID: 0}\n" +
                      "  m_RootOrder: 0\n" +
                      "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n");

            sb.Append($"--- !u!114 &{EntryComponentId}\n" +
                      "MonoBehaviour:\n" +
                      "  m_ObjectHideFlags: 0\n" +
                      "  m_CorrespondingSourceObject: {fileID: 0}\n" +
                      "  m_PrefabInstance: {fileID: 0}\n" +
                      "  m_PrefabAsset: {fileID: 0}\n" +
                      $"  m_GameObject: {{fileID: {EntryGoId}}}\n" +
                      "  m_Enabled: 1\n" +
                      "  m_EditorHideFlags: 0\n" +
                      $"  m_Script: {{fileID: 11500000, guid: {entryScriptGuid}, type: 3}}\n" +
                      "  m_Name: \n" +
                      "  m_EditorClassIdentifier: \n");

            sb.Append("--- !u!1660057539 &9223372036854775807\n" +
                      "SceneRoots:\n" +
                      "  m_ObjectHideFlags: 0\n" +
                      "  m_Roots:\n" +
                      $"  - {{fileID: {EntryTransformId}}}\n");

            return sb.ToString();
        }

        private static string ToNamespace(string folderName){
            var clean = new string(Array.FindAll(folderName.ToCharArray(), c => char.IsLetterOrDigit(c) || c == '_'));
            if (string.IsNullOrEmpty(clean)) return "MyGame";
            if (char.IsDigit(clean[0])) clean = "_" + clean;
            return clean;
        }
    }
}
