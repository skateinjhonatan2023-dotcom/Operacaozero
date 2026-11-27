using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal static class OZIsland
{
    const string Generated = "Assets/Content/Generated/";

    struct Planting
    {
        public Transform parent;
        public Vector3 position;
        public float height;
    }
    static readonly List<Planting> plantings=new();
    static readonly List<(Vector2 start, Vector2 end)> roadLines=new();

    public static GameObject Create()
    {
        plantings.Clear();
        roadLines.Clear();
        OZCoastalProps.Import();
        var asphalt = Surface("Asphalt", "asphalt.jpg", Color.white);
        var concrete = Surface("Concrete", "concrete.jpg", Color.white);
        var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(Generated + "IlhaZeroTerrainV080.asset");
        if (!terrainData)
        {
            terrainData = new TerrainData { heightmapResolution = 513, size = new Vector3(2000, 160, 2000) };
            var heights = new float[513, 513];
            for (int z = 0; z < 513; z++)
            for (int x = 0; x < 513; x++)
            {
                float wx = x * 2000f / 512f - 1000f, wz = z * 2000f / 512f - 1000f;
                var p = new Vector2(wx, wz);
                float hills = Mathf.PerlinNoise(wx * .004f + 13, wz * .004f + 27) * 24f +
                    Mathf.PerlinNoise(wx * .014f + 41, wz * .014f + 62) * 5f;
                float mountain = Mathf.Exp(-((p-new Vector2(-440,500)).sqrMagnitude)/85000f)*45f;
                // Two offset ridgelines keep a readable silhouette from the town and coastal road.
                float ridgeAxis=wz-226f-77f*Mathf.Sin(wx*.0065f);
                float ridge=Mathf.Exp(-ridgeAxis*ridgeAxis/4400f)*Mathf.Exp(-wx*wx/145000f);
                float crags=1f-Mathf.Abs(2*Mathf.PerlinNoise(wx*.016f+7,wz*.016f+13)-1);
                float saddle=Mathf.Exp(-(p-new Vector2(260,-195)).sqrMagnitude/14000f);
                float y = 12f+hills+mountain+ridge*(21+27*crags)+saddle*18;
                foreach (var site in Districts)
                {
                    float flat = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(95, 160, Vector2.Distance(p, site)));
                    float level = 12f + Mathf.PerlinNoise(site.x*.004f+13, site.y*.004f+27)*24f +
                        Mathf.Exp(-((site-new Vector2(-440,500)).sqrMagnitude)/85000f)*45f;
                    y = Mathf.Lerp(y, level, flat);
                }
                float coast = Mathf.SmoothStep(0,1,Mathf.InverseLerp(850,990,Mathf.Max(Mathf.Abs(wx),Mathf.Abs(wz))));
                heights[z,x] = Mathf.Lerp(y, 1f, coast)/terrainData.size.y;
            }
            terrainData.SetHeights(0, 0, heights);
            AssetDatabase.CreateAsset(terrainData, Generated + "IlhaZeroTerrainV080.asset");
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
        terrain.heightmapPixelError = 4;
        terrain.drawHeightmap=true;
        terrain.GetComponent<TerrainCollider>().terrainData=terrainData;
        terrain.drawInstanced = false; // Keep the Android terrain mesh on the standard rendering path.
        terrain.basemapDistance=500;
        terrainData.SyncHeightmap();
        terrain.Flush();

        PaintGround(terrain,layer);
        BuildDistricts(root.transform, terrain, concrete, asphalt);
        var grove=new System.Random(810);
        foreach(var groveCenter in new[]{new Vector2(-124,132),new Vector2(156,164),new Vector2(-170,-140)})
        {
            for(int i=0;i<12;i++)
            {
                float angle=(float)grove.NextDouble()*2*Mathf.PI;
                float radius=8f+36f*Mathf.Sqrt((float)grove.NextDouble());
                Tree(root.transform,terrain,new Vector3(groveCenter.x+radius*Mathf.Cos(angle),0,
                    groveCenter.y+radius*Mathf.Sin(angle)),4.3f+(float)grove.NextDouble()*2.7f);
            }
        }
        BuildTown(root.transform,terrain,concrete,asphalt);
        BuildLandmarks(root.transform,terrain,concrete,asphalt);
        // Use ordinary URP mesh renderers for the surface; retain the matching TerrainCollider.
        BuildGroundMeshes(root.transform,terrain);
        terrain.drawHeightmap=false;
        PlaceRocks(root.transform, terrain);
        BuildVehicles(root.transform,terrain);
        var water = Block(root.transform, "Mar", new Vector3(0, 0, 0), new Vector3(2050, .15f, 2050),
            Solid("Ocean", new Color(.055f,.24f,.33f), .7f));
        UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
        Physics.SyncTransforms();
        foreach(var tree in plantings)
        {
            Vector3 p=tree.position;
            if(Physics.CheckCapsule(p+Vector3.up*1.6f,p+Vector3.up*3,1.05f,~0,QueryTriggerInteraction.Ignore))continue;
            Vector2 xz=new Vector2(p.x,p.z);
            bool road=false;
            foreach(var line in roadLines)
            {
                Vector2 delta=line.end-line.start;
                float t=Mathf.Clamp01(Vector2.Dot(xz-line.start,delta)/Mathf.Max(.01f,delta.sqrMagnitude));
                if((xz-line.start-t*delta).sqrMagnitude<29){road=true;break;}
            }
            if(!road)OZCoastalProps.Plant(tree.parent,p,tree.height/5f,Mathf.Repeat(p.x*17+p.z*29,360));
        }
        plantings.Clear();
        roadLines.Clear();
        OZChartBaker.Bake(root,terrain);
        return root;
    }

    static readonly string[] DistrictNames = { "Porto Fantasma", "Nova Esperança", "Base Ômega", "Aeroporto Zero",
        "Serra Negra", "Vila Abandonada", "Complexo Industrial", "Floresta Raven", "Estação Nexus", "Costa Sul" };
    static readonly Vector2[] Districts = { new(-690,-270), new(0,0), new(590,430), new(660,-350),
        new(-440,500), new(-310,-340), new(330,300), new(-140,650), new(330,-570), new(-500,-690) };

    internal static Material Solid(string name, Color color, float smoothness = .15f)
    {
        string path = Generated + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
        m.color=color; m.SetFloat("_Smoothness",smoothness); m.enableInstancing=true; EditorUtility.SetDirty(m); return m;
    }

    static void BuildDistricts(Transform parent, Terrain terrain, Material concrete, Material asphalt)
    {
        var metal=Solid("IlhaMetal",new Color(.27f,.32f,.35f),.42f);
        var random=new System.Random(707);
        // Connected route graph. Road surfaces follow sampled ground rather than floating on slopes.
        int[,] edges={{0,5},{5,1},{1,6},{6,2},{2,3},{3,8},{8,5},{5,9},{0,9},{1,4},{4,7},{7,6}};
        for(int i=0;i<edges.GetLength(0);i++)
        {
            int from=edges[i,0],to=edges[i,1];
            Vector2 TownGate(Vector2 outside)=>new Vector2(Mathf.Sign(outside.x)*96,Mathf.Sign(outside.y)*17);
            Vector2 a=from==1 ? TownGate(Districts[to]) : Districts[from];
            Vector2 b=to==1 ? TownGate(Districts[from]) : Districts[to];
            Road(parent,terrain,a,b,asphalt);
        }
        for(int i=0;i<Districts.Length;i++)
        {
            var d=Districts[i]; var center=new Vector3(d.x,0,d.y); center.y=Height(terrain,center);
            var region=new GameObject(DistrictNames[i]).transform; region.SetParent(parent,false); region.position=center;
            // Four clear spawn points per district; the player and bots are selected by mode.
            for(int k=0;k<4;k++)
            {
                var point=center+new Vector3((k%2==0?-1:1)*13,0,(k<2?-1:1)*13);
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
                    Tree(region,terrain,p,3f+(float)random.NextDouble()*3f);
                }
            }
            else if(i==3)
            {
                var runway=center+new Vector3(0,0,45); runway.y=Height(terrain,runway)+.08f;
                Block(region,"Pista",runway,new Vector3(160,.14f,19),asphalt);
                Warehouse(region,terrain,center+new Vector3(-55,0,-40),concrete);
                Warehouse(region,terrain,center+new Vector3(55,0,-40),concrete);
                Block(region,"Torre de controle",center+new Vector3(65,5,-12),new Vector3(6,10,6),metal);
                for(int k=0;k<12;k++)Block(region,"Marcação de pista",runway+new Vector3(-72+k*13,.085f,0),new Vector3(5,.025f,.35f),Solid("RunwayPaint",new Color(.85f,.84f,.76f)));
            }
            else
            {
                int n=i==1?8:i==5||i==9?3:5;
                for(int k=0;k<n;k++)
                {
                    float angle=k*Mathf.PI*2/n;
                    var site=center+new Vector3(Mathf.Cos(angle)*58,0,Mathf.Sin(angle)*58);
                    if ((i==1 && k==4) || (i==2 && k%3==0))
                    {
                        site.y=Height(terrain,site);
                        OZCoastalProps.Hangar(region,site,metal,Solid("Estrutura do hangar",new Color(.17f,.21f,.23f)),concrete);
                    }
                    else Warehouse(region,terrain,site,concrete);
                }
                for(int k=0;k<10;k++)
                {
                    var p=center+new Vector3(random.Next(-35,36),0,random.Next(-35,36));
                    if(Mathf.Abs(p.x-center.x)<4 || Mathf.Abs(p.z-center.z)<4 || (Mathf.Abs(Mathf.Abs(p.x-center.x)-13)<5 && Mathf.Abs(Mathf.Abs(p.z-center.z)-13)<5))continue;
                    p.y=Height(terrain,p)+.7f;
                    Block(region,i==0?"Contentor":"Cobertura",p,new Vector3(i==0?5:3,1.4f,1.6f),metal);
                }
            }
        }
    }

    static void PaintGround(Terrain t,TerrainLayer grass)
    {
        var dirt=AssetDatabase.LoadAssetAtPath<TerrainLayer>(Generated+"Dirt071.terrainlayer");
        if(!dirt){dirt=new TerrainLayer();AssetDatabase.CreateAsset(dirt,Generated+"Dirt071.terrainlayer");}
        dirt.diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Content/Raw/concrete.jpg");
        dirt.normalMapTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Content/Raw/concrete-normal.jpg");
        dirt.diffuseRemapMax=new Vector4(.63f,.52f,.39f,1);dirt.tileSize=new Vector2(8,8);
        t.terrainData.terrainLayers=new[]{grass,dirt};t.terrainData.alphamapResolution=256;
        var weights=new float[256,256,2];
        for(int z=0;z<256;z++)for(int x=0;x<256;x++){
            float u=x/255f,v=z/255f;var pos=new Vector2(u*2000-1000,v*2000-1000);
            float d=Mathf.Clamp01((t.terrainData.GetSteepness(u,v)-14)/30);
            d=Mathf.Max(d,Mathf.SmoothStep(0,1,Mathf.InverseLerp(830,980,Mathf.Max(Mathf.Abs(pos.x),Mathf.Abs(pos.y)))));
            d=Mathf.Max(d,Mathf.PerlinNoise(u*35,v*35)*.25f);
            weights[z,x,0]=1-d;weights[z,x,1]=d;
        }
        t.terrainData.SetAlphamaps(0,0,weights);EditorUtility.SetDirty(t.terrainData);EditorUtility.SetDirty(dirt);
    }
    static void BuildTown(Transform parent,Terrain t,Material concrete,Material asphalt)
    {
        var walls=Surface("TownPlaster","concrete.jpg",new Color(.78f,.64f,.49f));
        var roof=Solid("TownRoof071",new Color(.35f,.16f,.12f));
        var metal=Solid("TownMetal071",new Color(.25f,.34f,.36f));
        var wood=Solid("TownWood071",new Color(.32f,.23f,.14f));
        var rng=new System.Random(716);
        // Compact village blocks, alleys and sightline breaks around the first landing area.
        foreach(var d in new[]{new Vector2(0,0),Districts[5],Districts[9]})
        {
            var area=new GameObject("Bairro habitável").transform;area.SetParent(parent,false);
            for(int row=-1;row<=1;row++)for(int col=-1;col<=1;col++)
            {
                if(row==0&&col==0)continue;
                var site=new Vector3(d.x+col*28,0,d.y+row*28);
                site.y=Height(t,site);
                if (d==Districts[1] && row!=0 && col==0)
                    OZCoastalProps.Market(area,site,row>0 ? 0 : 180);
                else
                {
                    var home=House(area,site,concrete,walls,roof,wood);
                    home.rotation=Quaternion.Euler(0, row==0 ? col*90f : row<0 ? 180 : 0,0);
                    if(row*col!=0)
                    {
                        var offset=new Vector3(col*5.7f,0,0);
                        var wall=site+offset;
                        wall.y=Height(t,wall);
                        Block(area,"Muro de quintal",wall+Vector3.up*.52f,new Vector3(.22f,1.04f,6.8f),concrete);
                        Block(area,"Arremate",wall+Vector3.up*1.07f,new Vector3(.31f,.09f,6.9f),walls);
                    }
                }
            }
            for(int k=0;k<20;k++){
                var q=new Vector3(d.x+rng.Next(-85,86),0,d.y+rng.Next(-85,86));
                if(Mathf.Abs(q.x-d.x)<46&&Mathf.Abs(q.z-d.y)<46)continue;
                Tree(area,t,q,4+(float)rng.NextDouble()*4);
            }
        }
        var baseSite=new Vector3(590,0,430);baseSite.y=Height(t,baseSite);
        for(int k=0;k<4;k++){
            var q=baseSite+new Vector3(-37+k*24,0,38);q.y=Height(t,q);
            Block(parent,"Contêiner industrial",q+Vector3.up*1.3f,new Vector3(10,2.6f,2.6f),metal);
            for(int j=0;j<5;j++)Block(parent,"Nervura",q+new Vector3(-4+j*2,1.3f,1.34f),new Vector3(.15f,2.6f,.12f),metal);
        }
        // Cover the spaces between districts with lightweight vegetation clusters.
        for(int k=0;k<220;k++){
            var q=new Vector3(rng.Next(-805,806),0,rng.Next(-805,806));
            bool near=false;foreach(var d in Districts)if(Vector2.Distance(new Vector2(q.x,q.z),d)<110){near=true;break;}
            if(!near && t.terrainData.GetSteepness((q.x+1000)/2000,(q.z+1000)/2000)<30)Tree(parent,t,q,3.5f+(float)rng.NextDouble()*4);
        }
    }
    static Transform House(Transform parent,Vector3 s,Material concrete,Material wall,Material roof,Material wood)
    {
        var h=new GameObject("Casa explorável").transform;h.SetParent(parent,false);h.position=s;
        Block(h,"Fundação",s+Vector3.up*.12f,new Vector3(8,.24f,7),concrete);
        Block(h,"Fundo",s+new Vector3(0,1.65f,3.4f),new Vector3(8,3.3f,.22f),wall);
        foreach(int side in new[]{-1,1}){
            Block(h,"Frente",s+new Vector3(side*2.55f,1.65f,-3.4f),new Vector3(2.9f,3.3f,.22f),wall);
            Block(h,"Peitoril",s+new Vector3(side*3.9f,.6f,0),new Vector3(.22f,1.2f,7),wall);
            Block(h,"Verga",s+new Vector3(side*3.9f,2.85f,0),new Vector3(.22f,.9f,7),wall);
            for(int k=-1;k<=1;k++)Block(h,"Coluna",s+new Vector3(side*3.9f,1.8f,k*3),new Vector3(.22f,1.2f,1.1f),wall);
            var slope=Block(h,"Telha",s+new Vector3(side*2.05f,3.85f,0),new Vector3(4.8f,.18f,7.6f),roof);
            slope.rotation=Quaternion.Euler(0,0,-side*18);
        }
        Block(h,"Verga entrada",s+new Vector3(0,2.8f,-3.4f),new Vector3(2.2f,1,.22f),wall);
        Block(h,"Bancada",s+new Vector3(2,.55f,2),new Vector3(2.4f,1.1f,.75f),wood);
        var hinge=new GameObject("Porta").transform;hinge.SetParent(h,false);hinge.position=s+new Vector3(-1.05f,0,-3.5f);
        var door=Block(hinge,"Madeira",hinge.position+new Vector3(1,1.15f,0),new Vector3(2,2.3f,.12f),wood);
        GameObjectUtility.SetStaticEditorFlags(door.gameObject,0);hinge.gameObject.AddComponent<OZDoor>();
        return h;
    }
    static void BuildGroundMeshes(Transform parent,Terrain terrain)
    {
        const string groundPath=Generated+"CoastalGround.mat";
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Content/Harbor/IslandGround.shader");
        if(!shader)throw new InvalidOperationException("Shader do terreno costeiro ausente.");
        var ground=AssetDatabase.LoadAssetAtPath<Material>(groundPath);
        if(!ground){ground=new Material(shader);AssetDatabase.CreateAsset(ground,groundPath);}
        ground.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Content/Raw/grass.jpg"));
        OZContent.ConfigureTexture("Assets/Content/Raw/earth-track.jpg",512);
        ground.SetTexture("_SoilMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Content/Raw/earth-track.jpg"));
        ground.SetTexture("_GroundNormal",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Content/Raw/grass-normal.jpg"));
        EditorUtility.SetDirty(ground);
        var data=terrain.terrainData;int resolution=data.heightmapResolution;
        var heights=data.GetHeights(0,0,resolution,resolution);
        var root=new GameObject("Solo visível — malhas URP").transform;root.SetParent(parent,false);
        const int cells=64;float step=data.size.x/(resolution-1);
        for(int z0=0;z0<resolution-1;z0+=cells)for(int x0=0;x0<resolution-1;x0+=cells)
        {
            int nx=Mathf.Min(cells,resolution-1-x0),nz=Mathf.Min(cells,resolution-1-z0),stride=nx+1;
            var v=new Vector3[(nx+1)*(nz+1)];var uv=new Vector2[v.Length];var norms=new Vector3[v.Length];
            var triangles=new System.Collections.Generic.List<int>(nx*nz*6);
            var paint=new Color32[v.Length];
            for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++){
                int gx=x0+x,gz=z0+z,index=z*stride+x;
                float wx=gx*step,wz=gz*step;
                v[index]=new Vector3(x*step,heights[gz,gx]*data.size.y,z*step);
                uv[index]=new Vector2(wx/6,wz/6);
                norms[index]=data.GetInterpolatedNormal(gx/(resolution-1f),gz/(resolution-1f));
                float elevation=v[index].y+terrain.transform.position.y;
                float townX=wx+terrain.transform.position.x,townZ=wz+terrain.transform.position.z;
                float yardDistance=Mathf.Min(Mathf.Abs(Mathf.Abs(townX)-28),Mathf.Abs(Mathf.Abs(townZ)-28));
                float yard=(1-Mathf.InverseLerp(32,44,Mathf.Max(Mathf.Abs(townX),Mathf.Abs(townZ))))*
                    (1-Mathf.InverseLerp(2,8,yardDistance));
                float coast=1-Mathf.InverseLerp(1,7,elevation);
                float slope=1-Mathf.InverseLerp(.84f,.97f,norms[index].y);
                float variation=Mathf.PerlinNoise(wx*.018f,wz*.018f);
                paint[index]=new Color(Mathf.Clamp01(Mathf.Max(coast,Mathf.Max(yard,slope))+.16f*variation),variation,0,1);
                if(x==nx||z==nz)continue;
                triangles.Add(index);triangles.Add(index+stride);triangles.Add(index+1);
                triangles.Add(index+1);triangles.Add(index+stride);triangles.Add(index+stride+1);
            }
            var mesh=new Mesh {name=$"Solo_{x0}_{z0}",vertices=v,uv=uv,normals=norms,colors32=paint};
            mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            string path=Generated+$"Ground072_{x0}_{z0}.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}
            else AssetDatabase.CreateAsset(mesh,path);
            var chunk=new GameObject($"Setor {x0}-{z0}");chunk.transform.SetParent(root,false);
            chunk.transform.position=terrain.transform.position+new Vector3(x0*step,0,z0*step);
            chunk.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=chunk.AddComponent<MeshRenderer>();renderer.sharedMaterial=ground;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            // Collider stays on the original heightfield, not duplicated on every mesh tile.
        }
    }
    static void BuildLandmarks(Transform parent,Terrain t,Material concrete,Material asphalt)
    {
        var brick=Surface("LandmarkBrick","concrete.jpg",new Color(.72f,.51f,.38f));
        var plaster=Surface("LandmarkPlaster","concrete.jpg",new Color(.84f,.82f,.72f));
        var roof=Solid("LandmarkRoof",new Color(.32f,.15f,.09f));
        var steel=Solid("LandmarkSteel",new Color(.24f,.31f,.32f));
        var crate=Solid("LandmarkCrate",new Color(.44f,.33f,.19f));
        var paint=Solid("LandmarkPaint",new Color(.92f,.81f,.52f));
        // The first landing is inside a compact settlement, with sightline breaks every 15–35 metres.
        var town=new GameObject("Nova Esperança — praça e comércio").transform;town.SetParent(parent,false);
        for(int side=-1;side<=1;side+=2){
            Road(town,t,new Vector2(-96,side*17),new Vector2(96,side*17),asphalt);
            Road(town,t,new Vector2(side*17,-75),new Vector2(side*17,75),asphalt);
            for(int k=-2;k<=2;k++){
                var q=new Vector3(k*24,0,side*82);q.y=Height(t,q);
                House(town,q,concrete,k%2==0?brick:plaster,roof,crate);
            }
            for(int k=0;k<6;k++){var q=new Vector3(side*6,0,-45+k*18);q.y=Height(t,q);Block(town,"Banco e cobertura",q+Vector3.up*.5f,new Vector3(2.7f,1,.65f),concrete);}
        }
        for(int i=0;i<4;i++)
        {
            var loc=new Vector3(i<2 ? -10 : 10,0,i%2==0 ? 24 : -24);
            loc.y=Height(t,loc);
            OZCoastalProps.Chest(town,loc,i*33,true);
        }
        var square=new Vector3(0,0,0);square.y=Height(t,square);
        Block(town,"Praça",square+Vector3.up*.04f,new Vector3(10,.08f,10),concrete);
        Block(town,"Monumento",square+Vector3.up*1.5f,new Vector3(1.2f,3,1.2f),concrete);
        // Separate silhouettes for a military outpost, port, and industrial yard.
        foreach(int index in new[]{0,2,6,8})
        {
            var d=Districts[index];var zone=new GameObject(DistrictNames[index]+" — ponto tático").transform;zone.SetParent(parent,false);
            for(int k=0;k<5;k++){
                var q=new Vector3(d.x-38+k*19,0,d.y+88);q.y=Height(t,q);
                var mat=k%2==0?steel:brick;
                Block(zone,"Contêiner",q+Vector3.up*1.35f,new Vector3(10,2.7f,2.7f),mat);
                for(int j=0;j<5;j++)Block(zone,"Friso",q+new Vector3(-4+j*2,1.35f,1.39f),new Vector3(.13f,2.5f,.08f),mat);
                var supply=q+new Vector3(3,0,-4);
                supply.y=Height(t,supply);
                OZCoastalProps.Chest(zone,zone.InverseTransformPoint(supply),k*17,true);
            }
            var tower=new Vector3(d.x+90,0,d.y-65);tower.y=Height(t,tower);
            Watchtower(zone,tower,steel,concrete);
            for(int k=0;k<4;k++){var q=new Vector3(d.x-80,0,d.y-48+k*18);q.y=Height(t,q);Block(zone,"Muro defensivo",q+Vector3.up*.9f,new Vector3(.45f,1.8f,12),concrete);}
            var sign=new Vector3(d.x,0,d.y-75);sign.y=Height(t,sign);
            Block(zone,"Poste de sinalização",sign+Vector3.up*1.6f,new Vector3(.17f,3.2f,.17f),steel);
            Block(zone,"Placa",sign+Vector3.up*2.75f,new Vector3(3.8f,.8f,.12f),paint);
        }
    }
    static void Watchtower(Transform parent,Vector3 p,Material steel,Material concrete)
    {
        var tower=new GameObject("Torre de observação acessível").transform;tower.SetParent(parent,false);tower.position=p;
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
            Block(tower,"Pilar",p+new Vector3(x*1.8f,2.4f,z*1.8f),new Vector3(.25f,4.8f,.25f),steel);
        Block(tower,"Plataforma",p+Vector3.up*4.8f,new Vector3(4.2f,.25f,4.2f),concrete);
        Block(tower,"Cobertura",p+Vector3.up*7.3f,new Vector3(4.5f,.18f,4.5f),steel);
        foreach(int side in new[]{-1,1})Block(tower,"Parapeito",p+new Vector3(side*2,5.45f,0),new Vector3(.18f,1.1f,4.2f),steel);
        Block(tower,"Parapeito",p+new Vector3(0,5.45f,2),new Vector3(4.2f,1.1f,.18f),steel);
        // Ramp access with steps small enough for the character controller.
        for(int k=0;k<24;k++)Block(tower,"Degrau",p+new Vector3(0,(k+1)*.1f,-14+k*.52f),new Vector3(1.6f,(k+1)*.2f,.55f),concrete);
    }
    static void BuildVehicles(Transform parent,Terrain terrain)
    {
        var body=Solid("VehicleMetal",new Color(.25f,.30f,.22f),.4f);
        var tire=Solid("VehicleRubber",new Color(.045f,.05f,.05f));
        for(int i=0;i<6;i++)
        {
            var site=Districts[i<3?1:i==3?2:i==4?3:5];
            var pos=i<3 ? new Vector3(-39-i*5,0,-9) : new Vector3(site.x+30+i*5,0,site.y+30);
            pos.y=Height(terrain,pos);
            var car=new GameObject(i%3==0?"Jipe":i%3==1?"Buggy":"Motocicleta");car.transform.SetParent(parent,false);car.transform.position=pos;
            float width=i%3==2 ? .7f : 1.7f;
            OZCoastalProps.Vehicle(car.transform,i%3,body,tire);
            var ctrl=car.AddComponent<CharacterController>();ctrl.radius=width*.48f;ctrl.height=1.5f;ctrl.center=Vector3.up*.75f;ctrl.stepOffset=.25f;
            var vehicle=car.AddComponent<OZVehicle>();vehicle.Title=car.name;vehicle.MaxSpeed=i%3==0?15:i%3==1?19:21;
        }
    }

    static void Road(Transform parent,Terrain terrain,Vector2 a,Vector2 b,Material material)
    {
        roadLines.Add((a,b));
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
            int t=i*6,j=i*2; tris[t]=j;tris[t+1]=j+1;tris[t+2]=j+2;tris[t+3]=j+1;tris[t+4]=j+3;tris[t+5]=j+2;
        }
        var mesh=new Mesh {name="Estrada conformada",vertices=v,uv=uv,triangles=tris};mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
        string path=Generated+FormattableString.Invariant($"IlhaRoad_{a.x}_{a.y}_{b.x}_{b.y}.asset");
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}
        else AssetDatabase.CreateAsset(mesh,path);
        root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        // Terrain provides collision underneath.
        root.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void Tree(Transform parent,Terrain terrain,Vector3 p,float h)
    {
        p.y=Height(terrain,p);
        plantings.Add(new Planting {parent=parent,position=p,height=h});
    }

    static float Height(Terrain terrain, Vector3 position) =>
        terrain.terrainData.GetInterpolatedHeight(Mathf.Clamp01((position.x-terrain.transform.position.x)/terrain.terrainData.size.x), Mathf.Clamp01((position.z-terrain.transform.position.z)/terrain.terrainData.size.z)) + terrain.transform.position.y;

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
            // Separate sections leave two real window apertures per wall.
            Block(root.transform, "Peitoril", site + new Vector3(side * 5.35f,.55f,0),new Vector3(.3f,1.1f,8),concrete);
            Block(root.transform, "Verga", site + new Vector3(side * 5.35f,3f,0),new Vector3(.3f,1.2f,8),concrete);
            for(int k=-1;k<=1;k++)Block(root.transform,"Pilar",site+new Vector3(side*5.35f,1.9f,k*3.6f),new Vector3(.3f,1.6f,k==0?1.6f:.8f),concrete);
            Block(root.transform, "Frente", site + new Vector3(side * 3.7f, 1.8f, -3.9f),
                new Vector3(3.3f, 3.6f, .3f), concrete);
        }
        var hinge = new GameObject("Porta interativa").transform;
        hinge.SetParent(root.transform,false); hinge.position=site+new Vector3(-2.05f,0,-4f);
        var door=Block(hinge,"Folha",hinge.position+new Vector3(1.05f,1.15f,0),new Vector3(2.1f,2.3f,.12f),concrete);
        GameObjectUtility.SetStaticEditorFlags(door.gameObject,0);
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
        for (int i = 0; i < 34; i++)
        {
            float angle = i * 2 * Mathf.PI / 20;
            float radius = 110 + (i % 4) * 145;
            Vector3 point = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            if(i>=20)
            {
                float x=-230f+(i-20)*27;
                point=new Vector3(x,0,221f+77f*Mathf.Sin(x*.0065f)+12f*Mathf.Sin(i));
            }
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
