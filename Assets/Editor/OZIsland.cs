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
        var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(Generated + "IlhaZeroTerrain.asset");
        if (!terrainData)
        {
            terrainData = new TerrainData { heightmapResolution = 513, size = new Vector3(2000, 100, 2000) };
            var heights = new float[513, 513];
            for (int z = 0; z < 513; z++)
            for (int x = 0; x < 513; x++)
            {
                float wx = x * 2000f / 512f - 1000f, wz = z * 2000f / 512f - 1000f;
                var p = new Vector2(wx, wz);
                float hills = Mathf.PerlinNoise(wx * .004f + 13, wz * .004f + 27) * 24f +
                    Mathf.PerlinNoise(wx * .014f + 41, wz * .014f + 62) * 5f;
                float mountain = Mathf.Exp(-((p-new Vector2(-440,500)).sqrMagnitude)/85000f)*45f;
                float y = 12f + hills + mountain;
                foreach (var site in Districts)
                {
                    float flat = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(95, 160, Vector2.Distance(p, site)));
                    float level = 12f + Mathf.PerlinNoise(site.x*.004f+13, site.y*.004f+27)*24f +
                        Mathf.Exp(-((site-new Vector2(-440,500)).sqrMagnitude)/85000f)*45f;
                    y = Mathf.Lerp(y, level, flat);
                }
                float coast = Mathf.SmoothStep(0,1,Mathf.InverseLerp(850,990,Mathf.Max(Mathf.Abs(wx),Mathf.Abs(wz))));
                heights[z,x] = Mathf.Lerp(y, 1f, coast)/100f;
            }
            terrainData.SetHeights(0, 0, heights);
            AssetDatabase.CreateAsset(terrainData, Generated + "IlhaZeroTerrain.asset");
        }

        var root = new GameObject("Ilha Zero — 2 km");
        var terrain = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
        terrain.transform.SetParent(root.transform);
        terrain.transform.position = new Vector3(-1000, -5, -1000);
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
        layer.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Content/Raw/grass-normal.jpg");
        layer.normalScale = .68f;
        layer.tileSize = new Vector2(6, 6);
        terrainData.terrainLayers = new[] { layer };
        EditorUtility.SetDirty(layer);
        EditorUtility.SetDirty(terrainData);
        terrain.heightmapPixelError = 12;
        terrain.drawInstanced = true;

        BuildDistricts(root.transform, terrain, concrete, asphalt);
        PlaceRocks(root.transform, terrain);
        var water = Block(root.transform, "Mar", new Vector3(0, 0, 0), new Vector3(2050, .15f, 2050),
            Solid("Ocean", new Color(.055f,.24f,.33f), .7f));
        UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
        return root;
    }

    static readonly string[] DistrictNames = { "Porto Fantasma", "Nova Esperança", "Base Ômega", "Aeroporto Zero",
        "Serra Negra", "Vila Abandonada", "Complexo Industrial", "Floresta Raven", "Estação Nexus", "Costa Sul" };
    static readonly Vector2[] Districts = { new(-690,-270), new(0,0), new(590,430), new(660,-350),
        new(-440,500), new(-310,-340), new(330,300), new(-140,650), new(330,-570), new(-500,-690) };

    static Material Solid(string name, Color color, float smoothness = .15f)
    {
        string path = Generated + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
        m.color=color; m.SetFloat("_Smoothness",smoothness); m.enableInstancing=true; EditorUtility.SetDirty(m); return m;
    }

    static void BuildDistricts(Transform parent, Terrain terrain, Material concrete, Material asphalt)
    {
        var metal=Solid("IlhaMetal",new Color(.27f,.32f,.35f),.42f);
        var wood=Solid("IlhaWood",new Color(.34f,.24f,.16f));
        var leaf=Solid("IlhaLeaf",new Color(.14f,.29f,.13f));
        var random=new System.Random(707);
        // Connected route graph. Road surfaces follow sampled ground rather than floating on slopes.
        int[,] edges={{0,5},{5,1},{1,6},{6,2},{2,3},{3,8},{8,5},{5,9},{0,9},{1,4},{4,7},{7,6}};
        for(int i=0;i<edges.GetLength(0);i++)
            Road(parent,terrain,Districts[edges[i,0]],Districts[edges[i,1]],asphalt);
        for(int i=0;i<Districts.Length;i++)
        {
            var d=Districts[i]; var center=new Vector3(d.x,0,d.y); center.y=Height(terrain,center);
            var region=new GameObject(DistrictNames[i]).transform; region.SetParent(parent,false); region.position=center;
            // Four clear spawn points per district; the player and bots are selected by mode.
            for(int k=0;k<4;k++)
            {
                var point=center+new Vector3((k%2==0?-1:1)*18,0,(k<2?-1:1)*18);
                point.y=Height(terrain,point)+.2f;
                var spawn=new GameObject($"Spawn_{(i==1?0:i==0?1:i):00}_{k:00}").transform;
                spawn.SetParent(region,false); spawn.position=point;
            }
            if(i==7 || i==4)
            {
                for(int k=0;k<34;k++)
                {
                    var p=center+new Vector3(random.Next(-90,91),0,random.Next(-90,91));
                    if(Mathf.Abs(p.x-center.x)<24 && Mathf.Abs(p.z-center.z)<24)continue;
                    Tree(region,terrain,p,wood,leaf,3f+(float)random.NextDouble()*3f);
                }
            }
            else if(i==3)
            {
                var runway=center+new Vector3(0,0,45); runway.y=Height(terrain,runway)+.08f;
                Block(region,"Pista",runway,new Vector3(160,.14f,19),asphalt);
                Warehouse(region,terrain,center+new Vector3(-55,0,-40),concrete);
                Warehouse(region,terrain,center+new Vector3(55,0,-40),concrete);
                Block(region,"Torre de controle",center+new Vector3(65,5,-12),new Vector3(6,10,6),metal);
            }
            else
            {
                int n=i==1?8:i==5||i==9?3:5;
                for(int k=0;k<n;k++)
                {
                    float angle=k*Mathf.PI*2/n;
                    var site=center+new Vector3(Mathf.Cos(angle)*58,0,Mathf.Sin(angle)*58);
                    Warehouse(region,terrain,site,concrete);
                }
                for(int k=0;k<10;k++)
                {
                    var p=center+new Vector3(random.Next(-35,36),0,random.Next(-35,36));
                    if(Mathf.Abs(p.x)<0.01f || (Mathf.Abs(Mathf.Abs(p.x-center.x)-18)<6 && Mathf.Abs(Mathf.Abs(p.z-center.z)-18)<6))continue;
                    p.y=Height(terrain,p)+.7f;
                    Block(region,i==0?"Contentor":"Cobertura",p,new Vector3(i==0?5:3,1.4f,1.6f),metal);
                }
            }
        }
    }

    static void Road(Transform parent,Terrain terrain,Vector2 a,Vector2 b,Material material)
    {
        var root=new GameObject("Estrada").transform; root.SetParent(parent,false);
        int steps=Mathf.CeilToInt(Vector2.Distance(a,b)/8f);
        var v=new Vector3[(steps+1)*2]; var uv=new Vector2[v.Length]; var tris=new int[steps*6];
        Vector2 side=new Vector2(-(b-a).y,(b-a).x).normalized*3.7f;
        for(int i=0;i<=steps;i++)
        {
            var p=Vector2.Lerp(a,b,(float)i/steps);
            for(int k=0;k<2;k++)
            {
                var q=p+(k==0?-side:side); var point=new Vector3(q.x,0,q.y);
                point.y=Height(terrain,point)+.10f;
                v[i*2+k]=point; uv[i*2+k]=new Vector2(k*2.5f,i*2.6f);
            }
            if(i==steps)continue;
            int t=i*6,j=i*2; tris[t]=j;tris[t+1]=j+2;tris[t+2]=j+1;tris[t+3]=j+1;tris[t+4]=j+2;tris[t+5]=j+3;
        }
        var mesh=new Mesh {name="Estrada conformada",vertices=v,uv=uv,triangles=tris};mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=AssetDatabase.GenerateUniqueAssetPath(Generated+"IlhaRoad.asset");AssetDatabase.CreateAsset(mesh,path);
        root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        // Terrain provides collision underneath.
    }

    static void Tree(Transform parent,Terrain terrain,Vector3 p,Material bark,Material leaf,float h)
    {
        p.y=Height(terrain,p);
        var tree=new GameObject("Árvore procedural").transform;tree.SetParent(parent,false);tree.position=p;
        var trunk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);trunk.transform.SetParent(tree,false);
        trunk.transform.localPosition=Vector3.up*h*.42f;trunk.transform.localScale=new Vector3(.32f,h*.42f,.32f);
        trunk.GetComponent<Renderer>().sharedMaterial=bark;
        var crown=GameObject.CreatePrimitive(PrimitiveType.Sphere);crown.transform.SetParent(tree,false);
        crown.transform.localPosition=Vector3.up*h*.86f;crown.transform.localScale=new Vector3(h*.65f,h*.65f,h*.65f);
        crown.GetComponent<Renderer>().sharedMaterial=leaf;UnityEngine.Object.DestroyImmediate(crown.GetComponent<Collider>());
        var lod=tree.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.015f,new[]{trunk.GetComponent<Renderer>(),crown.GetComponent<Renderer>()})});lod.RecalculateBounds();
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
        string normalPath = "Assets/Content/Raw/" + System.IO.Path.GetFileNameWithoutExtension(textureName) + "-normal.jpg";
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
        material.SetFloat("_BumpScale", .72f);
        material.EnableKeyword("_NORMALMAP");
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
        var hinge = new GameObject("Porta interativa").transform;
        hinge.SetParent(root.transform,false); hinge.position=site+new Vector3(-2.05f,0,-4f);
        var door=Block(hinge,"Folha",hinge.position+new Vector3(1.05f,1.15f,0),new Vector3(2.1f,2.3f,.12f),concrete);
        hinge.gameObject.AddComponent<OZDoor>();
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
            float radius = 210 + (i % 4) * 145;
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
        // Size the UVs in metres. A road slab and a warehouse wall share texel density.
        var filter = block.GetComponent<MeshFilter>();
        var meshKey = FormattableString.Invariant($"Block_{scale.x:G6}_{scale.y:G6}_{scale.z:G6}");
        var meshPath = Generated + meshKey + ".asset";
        var surface = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (!surface)
        {
            surface = UnityEngine.Object.Instantiate(filter.sharedMesh);
            surface.name = meshKey;
            var uv = surface.uv;
            var normals = surface.normals;
            var tangents = surface.tangents;
            for (int i = 0; i < uv.Length; i++)
            {
                Vector3 tangent = tangents[i];
                Vector3 bitangent = Vector3.Cross(normals[i], tangent);
                uv[i] = Vector2.Scale(uv[i], new Vector2(Vector3.Scale(tangent, scale).magnitude,
                    Vector3.Scale(bitangent, scale).magnitude) / 3f);
            }
            surface.uv = uv;
            AssetDatabase.CreateAsset(surface, meshPath);
        }
        filter.sharedMesh = surface;
        block.GetComponent<Renderer>().sharedMaterial = material;
        GameObjectUtility.SetStaticEditorFlags(block, StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
        return block.transform;
    }
}
