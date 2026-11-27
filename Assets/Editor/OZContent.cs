using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class OZContent
{
    const string Pack = "Assets/Content/Quaternius/";
    const string Model = Pack + "Superhero_Male_FullBody.fbx";
    const string Motions = Pack + "UAL1_Standard.fbx";
    const string Material = "Assets/Content/Generated/Operative.mat";
    const string Weapon = "Assets/Content/Weapon/";

    public static void Import()
    {
        Humanoid(Model);
        Humanoid(Motions);
        ConfigureTexture(Pack + "T_Superhero_Male_Dark.png", 1024);
        ConfigureTexture(Pack + "T_Eye_Brown.png", 512);
        ConfigureTexture(Pack + "T_Superhero_Male_Normal.png", 1024, TextureImporterType.NormalMap);
        ConfigureTexture("Assets/Content/Raw/asphalt.jpg", 1024);
        ConfigureTexture("Assets/Content/Raw/concrete.jpg", 1024);
        ConfigureTexture("Assets/Content/Raw/grass.jpg", 1024);
        foreach (var surface in new[] { "asphalt", "concrete", "grass" })
            ConfigureTexture("Assets/Content/Raw/" + surface + "-normal.jpg", 1024, TextureImporterType.NormalMap);
        ConfigureTexture(Weapon + "M4A1_Base_Color.png", 512);
        ConfigureTexture(Weapon + "M4A1_Normal.png", 512, TextureImporterType.NormalMap);
        ConfigureTexture(Weapon + "M4A1_MetalSmooth.png", 512, linear: true);
        ConfigureTexture("Assets/Content/Coast/coast_rocks_01_diff_1k.jpg", 1024);
        ConfigureTexture("Assets/Content/Coast/coast_rocks_01_nor_gl_1k.jpg", 1024, TextureImporterType.NormalMap);
    }

    static void Humanoid(string path)
    {
        if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
            throw new InvalidOperationException("FBX ausente: " + path);
        if (importer.animationType == ModelImporterAnimationType.Human &&
            importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel) return;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.SaveAndReimport();
    }

    internal static void ConfigureTexture(string path, int size, TextureImporterType type = TextureImporterType.Default, bool linear = false)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            throw new InvalidOperationException("Textura ausente: " + path);
        var android = importer.GetPlatformTextureSettings("Android");
        if (android.overridden && android.maxTextureSize == size &&
            android.format == TextureImporterFormat.ASTC_6x6 && importer.textureType == type &&
            importer.mipmapEnabled && importer.sRGBTexture == (!linear && type != TextureImporterType.NormalMap)) return;
        importer.textureType = type;
        importer.mipmapEnabled = true;
        importer.sRGBTexture = !linear && type != TextureImporterType.NormalMap;
        android.overridden = true;
        android.maxTextureSize = size;
        android.format = TextureImporterFormat.ASTC_6x6;
        importer.SetPlatformTextureSettings(android);
        importer.SaveAndReimport();
    }

    public static AnimationClip Animation(string name)
    {
        return AssetDatabase.LoadAllAssetsAtPath(Motions).OfType<AnimationClip>()
            .FirstOrDefault(clip => clip.name == name || clip.name.EndsWith("|" + name, StringComparison.Ordinal)) ??
            throw new InvalidOperationException("Clipe FBX ausente: " + name);
    }

    public static GameObject CreateActor()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        var actor = new GameObject("Operador");
        var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
        body.transform.SetParent(actor.transform, false);
        var animator = body.GetComponentInChildren<Animator>(true);
        if (!animator || !animator.avatar || !animator.avatar.isValid || !animator.avatar.isHuman)
            throw new InvalidOperationException("Avatar humanoide inválido: " + Model);
        var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (!hand) throw new InvalidOperationException("Osso da mão direita ausente.");

        var material = AssetDatabase.LoadAssetAtPath<Material>(Material);
        if (!material)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("Shader URP Lit indisponível.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, Material);
        }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Pack + "T_Superhero_Male_Dark.png");
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Pack + "T_Superhero_Male_Normal.png"));
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_Smoothness", .22f);
        EditorUtility.SetDirty(material);
        string eyesPath = "Assets/Content/Generated/Eyes.mat";
        var eyes = AssetDatabase.LoadAssetAtPath<Material>(eyesPath);
        if (!eyes)
        {
            eyes = new Material(material.shader);
            AssetDatabase.CreateAsset(eyes, eyesPath);
        }
        eyes.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Pack + "T_Eye_Brown.png");
        eyes.SetFloat("_Smoothness", .65f);
        EditorUtility.SetDirty(eyes);
        foreach (var renderer in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.name == "SuperHero_Male")
                renderer.sharedMaterial = material;
            else if (renderer.name == "Eyes")
                renderer.sharedMaterials = Enumerable.Repeat(eyes, renderer.sharedMaterials.Length).ToArray();
        }

        var grip = new GameObject("WeaponGrip").transform;
        grip.SetParent(hand, false);
        var forearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        if (forearm)
            grip.rotation = Quaternion.LookRotation(hand.position - forearm.position, actor.transform.up);
        return actor;
    }

    public static GameObject CreateRifle(Transform grip)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Weapon + "M4A1-Mobile.fbx");
        if (!source) throw new InvalidOperationException("Modelo de rifle ausente.");
        var rifle = (GameObject)PrefabUtility.InstantiatePrefab(source);
        rifle.transform.SetParent(grip, false);
        rifle.transform.localPosition = new Vector3(0, .045f, .05f);
        rifle.transform.localScale = Vector3.one * .87f;
        string path = "Assets/Content/Generated/Rifle.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("Shader URP Lit indisponível.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Weapon + "M4A1_Base_Color.png");
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Weapon + "M4A1_Normal.png"));
        material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Weapon + "M4A1_MetalSmooth.png"));
        material.SetFloat("_Smoothness", .55f);
        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        foreach (var renderer in rifle.GetComponentsInChildren<MeshRenderer>())
            renderer.sharedMaterial = material;
        EditorUtility.SetDirty(material);
        return rifle;
    }

    public static Material EffectsMaterial()
    {
        const string path = "Assets/Content/Generated/Effects.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) throw new InvalidOperationException("Shader URP Unlit indisponível.");
        material = new Material(shader);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
