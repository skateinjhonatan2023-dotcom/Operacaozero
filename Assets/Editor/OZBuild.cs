using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class OZBuild
{
    const string Content = "Assets/Content/";
    const string Scene = "Assets/Scenes/OperationZero.unity";
    const string Apk = "Builds/OperationZero-debug.apk";

    [MenuItem("Operation Zero/Preparar cena")]
    public static void Prepare()
    {
        if (!AssetDatabase.IsValidFolder(Content + "Generated"))
            AssetDatabase.CreateFolder("Assets/Content", "Generated");
        OZContent.Import();
        OZRenderSetup.ConfigurePipeline();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // Always generate the current island; an older prefab must not silently override it.
        var island = OZIsland.Create();
        var ground = island.transform.Find("Solo visível — malhas URP");
        if (!ground || ground.GetComponentsInChildren<MeshRenderer>().Length < 64)
            throw new InvalidOperationException("Malhas visíveis do terreno ausentes.");
        var points = island.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name.StartsWith("Spawn_", StringComparison.Ordinal))
            .OrderBy(t => t.name, StringComparer.Ordinal).ToArray();
        if (points.Length < 40 || !island.GetComponentInChildren<TerrainCollider>(true))
            throw new InvalidOperationException("A ilha precisa de colisões e pelo menos 13 spawns.");

        var template = CreateTemplate(out var rifle);
        var sound = AssetDatabase.LoadAssetAtPath<AudioClip>(Content + "Shot.wav");
        if (!sound) throw new InvalidOperationException("Som de disparo ausente.");
        var camera = CreateCamera();
        SetupLighting();
        OZRenderSetup.Environment();

        var root = new GameObject("Operation Zero");
        var game = root.AddComponent<OZGame>();
        game.soldier = template;
        game.rifle = rifle;
        game.shotSound = sound;
        game.spawns = points;
        game.view = camera;
        game.effectsMaterial = OZContent.EffectsMaterial();
        root.AddComponent<OZHud>().game = game;
        OZLobbyContent.Install(root, game);

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, Scene))
            throw new IOException("Falha ao salvar cena.");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
        PlayerSettings.productName = "Operation Zero";
        PlayerSettings.companyName = "Operation Zero Studio";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        AssetDatabase.SaveAssets();
    }

    static OZActor CreateTemplate(out GameObject rifle)
    {
        var soldier = AssetDatabase.LoadAssetAtPath<GameObject>(Content + "Soldier.prefab");
        var actor = soldier ? (GameObject)PrefabUtility.InstantiatePrefab(soldier) : OZContent.CreateActor();
        actor.name = "Operador (modelo)";
        var template = actor.GetComponent<OZActor>() ?? actor.AddComponent<OZActor>();
        if (!actor.GetComponentInChildren<SkinnedMeshRenderer>(true) ||
            !actor.GetComponentInChildren<Animator>(true))
            throw new InvalidOperationException("O operador precisa de malha skinned e Animator.");

        template.idle = Clip("Idle", "Idle_Loop");
        template.walk = Clip("Walk", "Walk_Loop");
        template.run = Clip("Run", "Sprint_Loop");
        template.aim = Clip("Aim", "Pistol_Aim_Neutral");
        template.fire = Clip("Fire", "Pistol_Shoot");
        template.death = Clip("Death", "Death01");
        if (!soldier)
        {
            template.crouch = OZContent.Animation("Crouch_Idle_Loop");
            template.crouchWalk = OZContent.Animation("Crouch_Fwd_Loop");
            template.airborne = OZContent.Animation("Jump_Loop");
            template.reload = OZContent.Animation("Pistol_Reload");
            template.hit = OZContent.Animation("Hit_Chest");
        }

        var grip = actor.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "WeaponGrip");
        if (!grip) throw new InvalidOperationException("O operador precisa de um ponto WeaponGrip.");
        var weapon = AssetDatabase.LoadAssetAtPath<GameObject>(Content + "Rifle.prefab");
        if (weapon)
        {
            rifle = (GameObject)PrefabUtility.InstantiatePrefab(weapon);
            rifle.transform.SetParent(grip, false);
        }
        else
            rifle = OZContent.CreateRifle(grip);

        if (!rifle.GetComponentInChildren<Renderer>(true))
            throw new InvalidOperationException("O rifle precisa de uma malha.");
        foreach (var collider in actor.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        actor.SetActive(false);
        return template;
    }

    static AnimationClip Clip(string file, string motion)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Content + "Animations/" + file + ".anim")
            ?? OZContent.Animation(motion);
        if (clip.length <= 0) throw new InvalidOperationException("Animação vazia: " + clip.name);
        return clip;
    }

    static Camera CreateCamera()
    {
        var root = new GameObject("Camera");
        root.tag = "MainCamera";
        var camera = root.AddComponent<Camera>();
        camera.nearClipPlane = .05f;
        camera.farClipPlane = 500;
        camera.clearFlags = CameraClearFlags.Skybox;
        root.AddComponent<AudioListener>();
        return camera;
    }

    static void SetupLighting()
    {
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.35f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(48, -35, 0);
        RenderSettings.sun = sun;
        RenderSettings.ambientLight = new Color(.5f, .55f, .6f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(.65f, .75f, .8f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 290;
        RenderSettings.fogEndDistance = 850;
    }

    [MenuItem("Operation Zero/Validar cena nativa salva")]
    public static void ValidateSavedScene()
    {
        var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();
        var game = roots.SelectMany(root => root.GetComponentsInChildren<OZGame>(true)).Single();
        var lobby = game.GetComponent<OZLobby>();
        if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset) ||
            !RenderSettings.skybox || !RenderSettings.customReflectionTexture)
            throw new InvalidOperationException("A cena salva perdeu o URP, o céu ou o reflexo.");
        var volumes = roots.SelectMany(root => root.GetComponentsInChildren<Volume>(true));
        if (!volumes.Any(v => v.isGlobal && v.sharedProfile && v.sharedProfile.TryGet<Tonemapping>(out _)))
            throw new InvalidOperationException("Perfil de cor da ilha ausente.");
        if (!lobby || !lobby.stage || !lobby.stage.animator || !lobby.stage.idle ||
            !game.view || !game.soldier || !game.rifle || !game.shotSound)
            throw new InvalidOperationException("Referência de gameplay ou lobby perdida na serialização.");
        var mesh = game.soldier.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (!mesh.Any(r => r.sharedMesh && r.sharedMesh.vertexCount > 0 && r.bones.Length > 0) ||
            !game.rifle.GetComponentsInChildren<MeshFilter>(true).Any(f => f.sharedMesh && f.sharedMesh.vertexCount > 0))
            throw new InvalidOperationException("Operador ou arma sem geometria importada.");
        var stage = lobby.stage;
        if (!stage.operatorRoot || !stage.cameraView || !stage.rifle ||
            !stage.animator.avatar || !stage.animator.avatar.isValid || stage.idle.legacy || stage.idle.length <= 0 ||
            !stage.operatorRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Any(r => r.sharedMesh && r.sharedMesh.vertexCount > 0 && r.bones.Length > 0))
            throw new InvalidOperationException("Operador do lobby sem geometria, Avatar ou animação válida.");
        if (!stage.rifle.GetComponentsInChildren<MeshRenderer>(true).Any())
            throw new InvalidOperationException("Arma do lobby sem renderer.");
        Debug.Log("Operation Zero: referências do motor, cena, operador e rifle validadas.");
    }

    // Unity Build Automation: configure OZBuild.CloudPrepare as the pre-export method.
    public static void CloudPrepare()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Instale Android Build Support, SDK, NDK e OpenJDK.");
        Prepare();
        ValidateSavedScene();
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.operationzero.game");
        PlayerSettings.bundleVersion = "0.7.1";
        PlayerSettings.Android.bundleVersionCode = 8;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        EditorUserBuildSettings.buildAppBundle = false;
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Operation Zero/Compilar APK de testes")]
    public static void Android()
    {
        CloudPrepare();
        Directory.CreateDirectory("Builds");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = Apk,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded || !File.Exists(Apk))
            throw new InvalidOperationException("Falha na compilação: " + report.summary.result);
    }
}
