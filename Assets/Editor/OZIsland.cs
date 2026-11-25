using System;
using UnityEditor;
using UnityEngine;

internal static class OZIsland
{
    const string Generated = "Assets/Content/Generated/";

    public static GameObject Create()
    {
        var asphalt = Surface("Asphalt", "asphalt.jpg", Color.white);
        var concrete = Surface("Concrete", "concrete.jpg", Color.white);
        var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(Generated + "IslandTerrain.asset");
        if (!terrainData)
        {
            terrainData = new TerrainData { heightmapResolution = 129, size = new Vector3(320, 20, 320) };
            var heights = new float[129, 129];
            for (int z = 0; z < 129; z++)
            for (int x = 0; x < 129; x++)
            {
                float worldZ = z * 2.5f - 160;
                float worldX = x * 2.5f - 160;
                float distance = Mathf.Abs(worldZ);
                float hills = Mathf.InverseLerp(7, 48, distance);
                float edge = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(130, 155,
                    new Vector2(worldX, worldZ).magnitude));
                heights[z, x] = Mathf.Lerp(.2f + hills * .38f *
                    Mathf.PerlinNoise(x * .095f, z * .095f), .02f, edge);
            }
            terrainData.SetHeights(0, 0, heights);
            AssetDatabase.CreateAsset(terrainData, Generated + "IslandTerrain.asset");
        }

        var root = new GameObject("Setor costeiro");
        var terrain = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
        terrain.transform.SetParent(root.transform);
        terrain.transform.position = new Vector3(-160, -4, -160);
        var terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (!terrainShader) throw new System.InvalidOperationException("Shader URP Terrain Lit indisponível.");
        var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(Generated + "Terrain.mat");
        if (!terrainMaterial)
        {
            terrainMaterial = new Material(terrainShader);
            AssetDatabase.CreateAsset(terrainMaterial, Generated + "Terrain.mat");
        }
        terrain.materialTemplate = terrainMaterial;
        var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(Generated + "Ground.terrainlayer");
        if (!layer)
        {
            layer = new TerrainLayer();
            AssetDatabase.CreateAsset(layer, Generated + "Ground.terrainlayer");
        }
        layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Content/Raw/grass.jpg");
        layer.tileSize = new Vector2(6, 6);
        terrainData.terrainLayers = new[] { layer };
        EditorUtility.SetDirty(layer);
        EditorUtility.SetDirty(terrainData);
        terrain.heightmapPixelError = 12;
        terrain.drawInstanced = true;

        Block(root.transform, "Via de serviço", new Vector3(0, .025f, 0),
            new Vector3(235, .05f, 10), asphalt);

        Warehouse(root.transform, terrain, new Vector3(-57, 0, 12), concrete);
        Warehouse(root.transform, terrain, new Vector3(61, 0, -12), concrete);
        PlaceRocks(root.transform, terrain);
        var layout = new System.Random(712);
        for (int i = 0; i < 13; i++)
        {
            float angle = i * Mathf.PI * 2 / 13;
            float radius = i == 0 ? 22 : 34 + layout.Next(64);
            var point = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            point.y = Height(terrain, point) + .15f;
            var spawn = new GameObject($"Spawn_{i:00}").transform;
            spawn.SetParent(root.transform);
            spawn.position = point;
            spawn.rotation = Quaternion.LookRotation(new Vector3(-point.x, 0, -point.z));

            var cover = point + spawn.forward * 7;
            if (Mathf.Abs(cover.z) < 7) continue;
            cover.y = Height(terrain, cover) + .66f;
            Block(root.transform, "Barreira", cover, new Vector3(4, 1.6f, .6f), concrete)
                .rotation = spawn.rotation;
        }
        return root;
    }

    static float Height(Terrain terrain, Vector3 position) =>
        terrain.SampleHeight(position) + terrain.transform.position.y;

    static Material Surface(string name, string textureName, Color tint)
    {
        string path = Generated + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new System.InvalidOperationException("Shader URP indisponível.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Content/Raw/" + textureName);
        material.color = tint;
        material.SetFloat("_Smoothness", .13f);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    static void Warehouse(Transform parent, Terrain terrain, Vector3 site, Material concrete)
    {
        site.y = Height(terrain, site);
        var root = new GameObject("Depósito");
        root.transform.SetParent(parent, false);
        root.transform.position = site;
        Block(root.transform, "Laje", site + Vector3.up * .15f, new Vector3(11, .3f, 8), concrete);
        Block(root.transform, "Cobertura", site + Vector3.up * 3.65f, new Vector3(11.5f, .24f, 8.45f), concrete);
        Block(root.transform, "Parede de fundo", site + new Vector3(0, 1.8f, 3.9f), new Vector3(11, 3.6f, .2f), concrete);
        foreach (int side in new[] { -1, 1 })
        {
            Block(root.transform, "Parede lateral", site + new Vector3(side * 5.35f, 1.8f, 0),
                new Vector3(.3f, 3.6f, 8), concrete);
            Block(root.transform, "Frente", site + new Vector3(side * 3.7f, 1.8f, -3.9f),
                new Vector3(3.3f, 3.6f, .3f), concrete);
        }
    }

    static void PlaceRocks(Transform parent, Terrain terrain)
    {
        const string source = "Assets/Content/Coast/";
        var near = AssetDatabase.LoadAssetAtPath<GameObject>(source + "Coast_LOD0.fbx");
        var far = AssetDatabase.LoadAssetAtPath<GameObject>(source + "Coast_LOD1.fbx");
        if (!near || !far) throw new InvalidOperationException("Modelos de rocha ausentes.");
        var rockMaterial = AssetDatabase.LoadAssetAtPath<Material>(Generated + "Rock.mat");
        if (!rockMaterial)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("Shader URP Lit indisponível.");
            rockMaterial = new Material(shader);
            AssetDatabase.CreateAsset(rockMaterial, Generated + "Rock.mat");
        }
        rockMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(source + "coast_rocks_01_diff_1k.jpg");
        rockMaterial.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(source + "coast_rocks_01_nor_gl_1k.jpg"));
        rockMaterial.SetFloat("_Smoothness", .14f);
        rockMaterial.EnableKeyword("_NORMALMAP");
        rockMaterial.enableInstancing = true;
        EditorUtility.SetDirty(rockMaterial);
        for (int i = 0; i < 20; i++)
        {
            float angle = i * 2 * Mathf.PI / 20;
            float radius = 106 + (i % 4) * 6;
            Vector3 point = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            point.y = Height(terrain, point) - .3f;
            var root = new GameObject("Afloramento");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(point, Quaternion.Euler(0, i * 43, 0));
            root.transform.localScale = Vector3.one * (1 + (i % 3) * .35f);
            var levels = new LOD[2];
            for (int j = 0; j < 2; j++)
            {
                var mesh = (GameObject)PrefabUtility.InstantiatePrefab(j == 0 ? near : far);
                mesh.transform.SetParent(root.transform, false);
                var renderers = mesh.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var renderer in renderers) renderer.sharedMaterial = rockMaterial;
                levels[j] = new LOD(j == 0 ? .12f : .014f, renderers);
                if (j == 1)
                {
                    var filter = mesh.GetComponentInChildren<MeshFilter>(true);
                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                }
            }
            var group = root.AddComponent<LODGroup>();
            group.SetLODs(levels);
            group.RecalculateBounds();
        }
    }

    static Transform Block(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent);
        block.transform.position = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;
        GameObjectUtility.SetStaticEditorFlags(block, StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
        return block.transform;
    }
}
