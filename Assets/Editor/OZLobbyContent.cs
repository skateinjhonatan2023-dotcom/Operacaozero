using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

internal static class OZLobbyContent
{
    const int PortraitLayer = 30;
    const int PortraitMask = 1 << PortraitLayer;
    const string Root = "Assets/Content/Lobby/";
    const string Generated = "Assets/Content/Generated/";
    const string Model = Root + "Vigia.fbx";

    internal static void Install(GameObject root, OZGame game)
    {
        ConfigureTexture("Costa.jpg", TextureImporterType.Default, 2048, mipmaps: true);
        ConfigureTexture("Weave.png", TextureImporterType.Default, 128, mipmaps: true);
        ConfigureTexture("Weave-Normal.png", TextureImporterType.NormalMap, 128, mipmaps: true);

        var lobby = root.AddComponent<OZLobby>();
        lobby.game = game;
        lobby.regular = Asset<Font>("Barlow-Regular.ttf");
        lobby.bold = Asset<Font>("Barlow-Bold.ttf");
        lobby.italic = Asset<Font>("Barlow-Title.ttf");
        lobby.credits = Asset<TextAsset>("Creditos.txt");
        lobby.fontLicense = Asset<TextAsset>("OFL-Barlow.txt");
        lobby.panelSprite = Sprite("Panel.png", new Vector4(18, 18, 18, 18));
        lobby.goldSprite = Sprite("Gold.png", new Vector4(18, 18, 18, 18));
        lobby.brand = Sprite("Marca.png");
        lobby.skins = Sprite("Skins.jpg");
        lobby.weapons = Sprite("Armas.jpg");
        lobby.kits = Sprite("Pacotes.jpg");
        lobby.news = Sprite("Costa-Card.jpg");
        lobby.playIcon = Sprite("Jogar.png");
        lobby.stage = CreateStage(root.transform, game.view);
    }

    static OZLobbyStage CreateStage(Transform root, Camera gameCamera)
    {
        var space = new GameObject("Estúdio costeiro");
        space.transform.SetParent(root, false);
        space.transform.localPosition = Vector3.down * 250;
        var stage = space.AddComponent<OZLobbyStage>();

        try
        {
            ConfigureModel();
            CreateVigia(space.transform, stage);
            Debug.Log("OZ_BUILD: lobby Vigia validated.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("OZ_BUILD: Vigia unavailable; using the validated gameplay operator in the lobby. " + ex.Message);
            if (stage.operatorRoot)
                UnityEngine.Object.DestroyImmediate(stage.operatorRoot.gameObject);
            stage.operatorRoot = null;
            stage.animator = null;
            stage.idle = null;
            stage.rifle = null;
            CreateGameplayPortrait(space.transform, stage);
        }
        // Frame the entire skinned operator with room for a full rotation.
        var meshes = stage.operatorRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        var bounds = meshes[0].bounds;
        foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
        float height = Mathf.Clamp(bounds.size.y, 1.5f, 3f);
        float centerY = Mathf.Clamp(bounds.center.y - space.transform.position.y, .65f, 1.65f);
        float distance = Mathf.Max(3.4f, height * 1.75f);
        var cameraOrigin = Child(space.transform, "Câmera de apresentação", new Vector3(0, centerY, distance));
        cameraOrigin.rotation = Quaternion.LookRotation(Vector3.back);
        var camera = stage.cameraView = cameraOrigin.gameObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.04f, .06f, .08f);
        camera.cullingMask = PortraitMask;
        camera.fieldOfView = 38;
        camera.nearClipPlane = .12f;
        camera.farClipPlane = 32;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        gameCamera.cullingMask &= ~PortraitMask;

        var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "Painel panorâmico";
        background.transform.SetParent(space.transform, false);
        background.transform.position = cameraOrigin.position + cameraOrigin.forward * 26;
        background.transform.rotation = cameraOrigin.rotation;
        UnityEngine.Object.DestroyImmediate(background.GetComponent<Collider>());
        stage.backdrop = background.transform;
        stage.backdropTexture = Asset<Texture2D>("Costa.jpg");
        var matte = Material("Lobby-Costa", "Universal Render Pipeline/Unlit");
        matte.SetTexture("_BaseMap", stage.backdropTexture);
        EditorUtility.SetDirty(matte);
        var backgroundRenderer = background.GetComponent<MeshRenderer>();
        backgroundRenderer.sharedMaterial = matte;
        backgroundRenderer.shadowCastingMode = ShadowCastingMode.Off;
        backgroundRenderer.receiveShadows = false;

        var worldSun = RenderSettings.sun;
        if (worldSun) worldSun.cullingMask &= ~PortraitMask;
        var sunlight = Child(space.transform, "Luz principal", new Vector3(-2, 3.8f, 3.1f));
        sunlight.LookAt(space.transform.position + Vector3.up * 1.4f);
        var key = sunlight.gameObject.AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.55f;
        key.color = new Color(1, .85f, .67f);
        key.cullingMask = PortraitMask;
        AddFill(space.transform, "Preenchimento do céu", new Vector3(1.2f, 2f, 2.3f),
            new Color(.60f, .78f, 1f), 2.1f);
        AddFill(space.transform, "Contraluz da costa", new Vector3(-1.3f, 2.4f, -1.8f),
            new Color(1f, .77f, .47f), 2.8f);

        foreach (var child in space.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = PortraitLayer;
        return stage;
    }

    static void CreateVigia(Transform parent, OZLobbyStage stage)
    {
        var model = PrefabUtility.InstantiatePrefab(Asset<GameObject>("Vigia.fbx"), parent) as GameObject;
        if (!model) throw new InvalidOperationException("Não foi possível instanciar Vigia.");
        stage.operatorRoot = model.transform;
        stage.animator = model.GetComponentInChildren<Animator>(true);
        if (!model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(m => m.sharedMesh && m.sharedMesh.vertexCount > 0 && m.bones.Length > 0))
            throw new InvalidOperationException("Vigia sem malha esquelética importada.");
        if (!stage.animator)
            stage.animator = model.AddComponent<Animator>();
        if (!stage.animator.avatar || !stage.animator.avatar.isValid)
        {
            var avatar = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>()
                .FirstOrDefault(a => a.isValid);
            if (!avatar)
                throw new InvalidOperationException("Vigia sem Avatar válido para animação.");
            stage.animator.avatar = avatar;
        }
        if (!stage.animator.avatar.isHuman)
            Debug.LogWarning("OZ_BUILD: Vigia uses its authored Generic rig; Humanoid conversion pending.");
        stage.animator.applyRootMotion = false;
        stage.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        stage.idle = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal) && clip.length > .4f);
        if (!stage.idle || stage.idle.legacy)
            throw new InvalidOperationException("Clipe de apresentação do operador ausente ou inválido.");

        var transforms = model.GetComponentsInChildren<Transform>(true);
        stage.rifle = transforms.FirstOrDefault(t => t.name == "Vigia_Rifle");
        if (!stage.rifle || !stage.rifle.GetComponentInChildren<MeshRenderer>(true))
            throw new InvalidOperationException("Malha de rifle do operador ausente.");
        var toe = transforms.FirstOrDefault(t => t.name.EndsWith("LeftToeBase", StringComparison.Ordinal));
        var foot = transforms.FirstOrDefault(t => t.name.EndsWith("LeftFoot", StringComparison.Ordinal));
        if (!toe || !foot) throw new InvalidOperationException("Rig do operador sem ossos dos pés.");
        Vector3 front = Vector3.ProjectOnPlane(toe.position - foot.position, Vector3.up);
        if (front.sqrMagnitude < .0001f) throw new InvalidOperationException("Orientação do operador inválida.");
        model.transform.rotation = Quaternion.Euler(0, -32, 0) *
            Quaternion.FromToRotation(front.normalized, Vector3.forward);

        Dress(model, stage.rifle);
    }

    static void CreateGameplayPortrait(Transform parent, OZLobbyStage stage)
    {
        // Reuse the same skinned humanoid, materials and animation already validated in gameplay.
        var model = OZContent.CreateActor();
        model.transform.SetParent(parent, false);
        stage.operatorRoot = model.transform;
        stage.animator = model.GetComponentInChildren<Animator>(true);
        stage.idle = OZContent.Animation("Idle_Loop");
        if (!stage.animator || !stage.animator.avatar || !stage.animator.avatar.isValid ||
            !stage.idle || stage.idle.length <= 0 || stage.idle.legacy ||
            !model.GetComponentInChildren<SkinnedMeshRenderer>(true))
            throw new InvalidOperationException("Operador alternativo do lobby sem rig ou animação válida.");
        stage.animator.applyRootMotion = false;
        stage.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        var grip = model.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "WeaponGrip");
        if (!grip) throw new InvalidOperationException("Operador alternativo sem WeaponGrip.");
        stage.rifle = OZContent.CreateRifle(grip).transform;
        if (!stage.rifle.GetComponentInChildren<MeshRenderer>(true))
            throw new InvalidOperationException("Rifle alternativo do lobby sem malha.");
        model.transform.localRotation = Quaternion.Euler(0, -32, 0);
        Debug.Log("OZ_BUILD: gameplay operator fallback validated.");
    }

    static Transform Child(Transform parent, string name, Vector3 position)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        child.localPosition = position;
        return child;
    }

    static void AddFill(Transform group, string name, Vector3 position, Color color, float intensity)
    {
        var socket = Child(group, name, position);
        socket.LookAt(group.position + Vector3.up * 1.32f);
        var light = socket.gameObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.spotAngle = 70;
        light.range = 6;
        light.intensity = intensity;
        light.color = color;
        light.cullingMask = PortraitMask;
    }

    static void Dress(GameObject model, Transform rifle)
    {
        var fabric = Asset<Texture2D>("Weave.png");
        var normal = Asset<Texture2D>("Weave-Normal.png");
        foreach (var mesh in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var materials = mesh.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                if (!materials[index]) throw new InvalidOperationException("Material do operador ausente.");
                string name = materials[index].name;
                var material = Material("Lobby-" + name, "Universal Render Pipeline/Lit");
                var tint = name switch
                {
                    "Soldier_Clothes" => new Color(.28f, .30f, .23f),
                    "Soldier_Fabric" => new Color(.42f, .39f, .28f),
                    "Soldier_Armor" => new Color(.36f, .29f, .18f),
                    "Soldier_Flesh" => new Color(.39f, .31f, .25f),
                    "Soldier_Iron" => new Color(.18f, .19f, .18f),
                    "Soldier_Metal" => new Color(.30f, .31f, .29f),
                    "Soldier_Glass" => new Color(.10f, .14f, .17f),
                    _ => new Color(.15f, .16f, .15f)
                };
                bool textile = name is "Soldier_Clothes" or "Soldier_Fabric" or "Soldier_Armor";
                bool metal = name is "Soldier_Iron" or "Soldier_Metal";
                material.SetColor("_BaseColor", tint);
                material.SetFloat("_Metallic", metal ? .55f : .08f);
                material.SetFloat("_Smoothness", name == "Soldier_Glass" ? .63f : metal ? .43f : .26f);
                if (textile)
                {
                    material.SetTexture("_BaseMap", fabric);
                    material.SetTextureScale("_BaseMap", Vector2.one * 5);
                    material.SetTexture("_BumpMap", normal);
                    material.SetFloat("_BumpScale", .40f);
                    material.EnableKeyword("_NORMALMAP");
                }
                EditorUtility.SetDirty(material);
                materials[index] = material;
            }
            mesh.sharedMaterials = materials;
        }

        var rifleMaterial = AssetDatabase.LoadAssetAtPath<Material>(Generated + "Rifle.mat");
        if (!rifleMaterial)
        {
            var textureRoot = "Assets/Content/Weapon/";
            rifleMaterial = Material("Lobby-Rifle", "Universal Render Pipeline/Lit");
            rifleMaterial.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot + "M4A1_Base_Color.png"));
            rifleMaterial.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot + "M4A1_Normal.png"));
            rifleMaterial.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(rifleMaterial);
        }
        foreach (var mesh in rifle.GetComponentsInChildren<MeshRenderer>(true))
            mesh.sharedMaterial = rifleMaterial;
    }

    static Material Material(string name, string shaderName)
    {
        string path = Generated + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;

        var shader = Shader.Find(shaderName);
        if (!shader) throw new InvalidOperationException("Shader ausente: " + shaderName);
        material = new Material(shader);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void ConfigureModel()
    {
        var importer = AssetImporter.GetAtPath(Model) as ModelImporter;
        if (!importer) throw new InvalidOperationException("FBX de apresentação ausente.");
        if (importer.animationType == ModelImporterAnimationType.Generic && importer.importAnimation &&
            importer.bakeAxisConversion && importer.preserveHierarchy && !importer.optimizeGameObjects &&
            !importer.importCameras && !importer.importLights)
            return;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importAnimation = true;
        importer.bakeAxisConversion = true;
        importer.preserveHierarchy = true;
        importer.optimizeGameObjects = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.SaveAndReimport();
    }

    static Sprite Sprite(string filename, Vector4 border = default)
    {
        ConfigureTexture(filename, TextureImporterType.Sprite, 1024, border);
        return Asset<Sprite>(filename);
    }

    static void ConfigureTexture(string filename, TextureImporterType type, int size, Vector4 border = default,
        bool mipmaps = false)
    {
        var importer = AssetImporter.GetAtPath(Root + filename) as TextureImporter;
        if (!importer) throw new InvalidOperationException("Textura do lobby ausente: " + filename);

        var mobile = importer.GetPlatformTextureSettings("Android");
        var format = type == TextureImporterType.Sprite ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (importer.textureType == type && importer.npotScale == TextureImporterNPOTScale.None &&
            importer.mipmapEnabled == mipmaps && mobile.overridden && mobile.format == format &&
            mobile.maxTextureSize == size && (type != TextureImporterType.Sprite ||
            importer.spriteImportMode == SpriteImportMode.Single && importer.spriteBorder == border &&
            settings.spriteMeshType == SpriteMeshType.FullRect)) return;

        importer.textureType = type;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = mipmaps;
        importer.wrapMode = filename.StartsWith("Weave", StringComparison.Ordinal) ?
            TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        if (type == TextureImporterType.Sprite)
        {
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
        }
        mobile.overridden = true;
        mobile.maxTextureSize = size;
        mobile.format = format;
        importer.SetPlatformTextureSettings(mobile);
        importer.SaveAndReimport();
    }

    static T Asset<T>(string filename) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(Root + filename);
        if (!asset) throw new InvalidOperationException("Recurso do lobby ausente: " + filename);
        return asset;
    }
}
