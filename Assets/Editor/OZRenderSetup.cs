using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal static class OZRenderSetup
{
    const string Generated = "Assets/Content/Generated/";
    const string SkyPath = "Assets/Content/Environment/CoastalSky.hdr";
    const string Package = "Packages/com.unity.render-pipelines.universal/";
    const string AoType = "UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion";

    static T Asset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    static UniversalRendererData Renderer(string path)
    {
        var data = Asset<UniversalRendererData>(path);
        data.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
            Package + "Runtime/Data/PostProcessData.asset");
        if (!data.postProcessData) throw new InvalidOperationException("Recursos de pós-processamento URP ausentes.");
        EditorUtility.SetDirty(data);
        return data;
    }

    public static void ConfigurePipeline()
    {
        var standard = Renderer("Assets/Content/URPRenderer.asset");
        var detailed = Renderer(Generated + "RendererAO.asset");
        var ao = detailed.rendererFeatures.FirstOrDefault(f => f && f.GetType().FullName == AoType);
        if (!ao)
        {
            var type = typeof(UniversalRendererData).Assembly.GetType(AoType, true);
            ao = (ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
            ao.name = "Oclusão de ambiente";
            AssetDatabase.AddObjectToAsset(ao, detailed);
            detailed.rendererFeatures.Add(ao);
        }
        var config = new SerializedObject(ao);
        var settings = config.FindProperty("m_Settings");
        settings.FindPropertyRelative("Downsample").boolValue = true;
        settings.FindPropertyRelative("Intensity").floatValue = 1.25f;
        config.ApplyModifiedPropertiesWithoutUndo();
        ao.SetActive(true);
        detailed.SetDirty();
        EditorUtility.SetDirty(detailed);

        const string path = "Assets/Content/URP.asset";
        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (!urp)
        {
            urp = UniversalRenderPipelineAsset.Create(standard);
            AssetDatabase.CreateAsset(urp, path);
        }
        var pipeline = new SerializedObject(urp);
        var renderers = pipeline.FindProperty("m_RendererDataList");
        renderers.arraySize = 2;
        renderers.GetArrayElementAtIndex(0).objectReferenceValue = standard;
        renderers.GetArrayElementAtIndex(1).objectReferenceValue = detailed;
        pipeline.FindProperty("m_DefaultRendererIndex").intValue = 0;
        pipeline.FindProperty("m_SoftShadowsSupported").boolValue = true;
        pipeline.ApplyModifiedPropertiesWithoutUndo();
        urp.useSRPBatcher = true;
        urp.supportsHDR = true;
        urp.msaaSampleCount = 4;
        EditorUtility.SetDirty(urp);
        GraphicsSettings.defaultRenderPipeline = urp;
        QualitySettings.renderPipeline = urp;
        PlayerSettings.colorSpace = ColorSpace.Linear;
    }

    public static void Environment()
    {
        var importer = AssetImporter.GetAtPath(SkyPath) as TextureImporter;
        if (!importer) throw new InvalidOperationException("Panorama da ilha ausente.");
        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        if (importer.textureShape != TextureImporterShape.TextureCube ||
            textureSettings.cubemapConvolution != TextureImporterCubemapConvolution.Specular ||
            importer.maxTextureSize != 512)
        {
            importer.textureShape = TextureImporterShape.TextureCube;
            importer.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
            importer.mipmapEnabled = true;
            importer.ReadTextureSettings(textureSettings);
            textureSettings.cubemapConvolution = TextureImporterCubemapConvolution.Specular;
            importer.SetTextureSettings(textureSettings);
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
        }
        var sky = AssetDatabase.LoadAssetAtPath<Cubemap>(SkyPath);
        if (!sky) throw new InvalidOperationException("Falha na importação do cubemap costeiro.");
        string skyMaterial = Generated + "CoastalSky.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(skyMaterial);
        if (!material)
        {
            material = new Material(Shader.Find("Skybox/Cubemap"));
            AssetDatabase.CreateAsset(material, skyMaterial);
        }
        material.SetTexture("_Tex", sky);
        material.SetFloat("_Exposure", .80f);
        EditorUtility.SetDirty(material);
        RenderSettings.skybox = material;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.64f, .73f, .84f);
        RenderSettings.ambientEquatorColor = new Color(.44f, .48f, .48f);
        RenderSettings.ambientGroundColor = new Color(.31f, .29f, .25f);
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = sky;
        RenderSettings.reflectionIntensity = .70f;

        var profile = Asset<VolumeProfile>(Generated + "IslandGrade.asset");
        if (!profile.TryGet<Tonemapping>(out var tone))
        {
            tone = profile.Add<Tonemapping>();
            AssetDatabase.AddObjectToAsset(tone, profile);
        }
        tone.mode.Override(TonemappingMode.ACES);
        if (!profile.TryGet<ColorAdjustments>(out var grade))
        {
            grade = profile.Add<ColorAdjustments>();
            AssetDatabase.AddObjectToAsset(grade, profile);
        }
        grade.contrast.Override(5f);
        grade.saturation.Override(-4f);
        EditorUtility.SetDirty(tone);
        EditorUtility.SetDirty(grade);
        EditorUtility.SetDirty(profile);
        var volume = new GameObject("Cor da Ilha Zero").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
    }
}
