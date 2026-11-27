using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Linq;

internal static class OZCoastalProps
{
    const string Folder = "Assets/Content/Harbor/";
    const string Generated = "Assets/Content/Generated/";
    public static void Import()
    {
        foreach (string part in new[] {"objects","plaster","trim_01"})
        {
            OZContent.ConfigureTexture(Folder+"Facade_"+part+"_diff.jpg",512);
            OZContent.ConfigureTexture(Folder+"Facade_"+part+"_nor_gl.jpg",512,TextureImporterType.NormalMap);
        }
        foreach (var name in new[] {"Bark", "Leaves", "Branches", "ChestColor"})
            OZContent.ConfigureTexture(Folder + name + ".png", 512);
        OZContent.ConfigureTexture(Folder + "ChestNormal.png", 512, TextureImporterType.NormalMap);
        OZContent.ConfigureTexture(Folder + "ChestMask.png", 512, linear:true);
    }

    static Material Surface(string name, string image)
    {
        string path = Generated + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + image + ".png"));
        material.SetFloat("_Smoothness", .24f);
        material.enableInstancing = true;
        if (image == "Leaves")
        {
            material.SetFloat("_AlphaClip", 1);
            material.SetFloat("_Cutoff", .32f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;
        }
        if (image == "ChestColor")
        {
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "ChestNormal.png"));
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "ChestMask.png");
            material.SetTexture("_MetallicGlossMap", mask);
            material.SetTexture("_OcclusionMap", mask);
            material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_OCCLUSIONMAP");
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static GameObject Mesh(Transform parent, string file)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + file + ".fbx");
        return (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
    }

    public static void Chest(Transform parent, Vector3 local, float angle, bool solid)
    {
        var crate = Mesh(parent, "FreightChest");
        crate.transform.localPosition = local;
        crate.transform.localRotation = Quaternion.Euler(0, angle, 0);
        var renderers = crate.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers) r.sharedMaterial = Surface("FreightWood", "ChestColor");
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        crate.transform.position += Vector3.up * (parent.TransformPoint(local).y - bounds.min.y);
        if (solid)
        {
            foreach (var f in crate.GetComponentsInChildren<MeshFilter>())
                f.gameObject.AddComponent<BoxCollider>();
        }
    }

    public static void Plant(Transform parent, Vector3 position, float scale, float yaw)
    {
        var root = new GameObject("Árvore costeira").transform;
        root.SetParent(parent, false);
        root.position = position;
        root.localScale = Vector3.one * scale;
        root.rotation = Quaternion.Euler(0, yaw, 0);
        var levels = new LOD[2];
        for (int i = 0; i < 2; i++)
        {
            var mesh = Mesh(root, i == 0 ? "IslandTree" : "IslandTreeFar");
            var renderers = mesh.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
                r.sharedMaterials = r.sharedMaterials.Select(m => m.name.Contains("leaves") ? Surface("IslandLeaves","Leaves") :
                    m.name.Contains("branches") ? Surface("IslandBranches","Branches") : Surface("IslandBark","Bark")).ToArray();
            var bounds = renderers[0].bounds;
            mesh.transform.position += Vector3.up*(position.y-bounds.min.y);
            levels[i] = new LOD(i == 0 ? .16f : .016f, renderers);
        }
        var group = root.gameObject.AddComponent<LODGroup>();
        group.SetLODs(levels);
        group.RecalculateBounds();
        var trunk = root.gameObject.AddComponent<CapsuleCollider>();
        trunk.height = 2.7f;
        trunk.center = Vector3.up * 1.35f;
        trunk.radius = .2f;
    }

    public static void Vehicle(Transform parent,int type,Material body,Material rubber)
    {
        string file=type==0 ? "Patrulha" : type==1 ? "Buggy" : "Moto";
        var model=Mesh(parent,file);
        var front=model.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Direcao");
        if(front)
        {
            Vector3 direction=Vector3.ProjectOnPlane(front.position-model.transform.position,Vector3.up);
            if(direction.sqrMagnitude>.001f)
                model.transform.rotation=Quaternion.FromToRotation(direction.normalized,parent.forward)*model.transform.rotation;
        }
        foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())
        {
            Material Resolve(Material source)
            {
                if(source.name=="Pintura")return body;
                if(source.name=="Borracha")return rubber;
                return source.name switch {
                    "Vidro"=>OZIsland.Solid("Vidro automotivo",new Color(.19f,.31f,.34f),.85f),
                    "Farol"=>OZIsland.Solid("Lente de farol",new Color(.87f,.84f,.64f),.72f),
                    "Liga"=>OZIsland.Solid("Liga automotiva",new Color(.47f,.48f,.45f),.59f),
                    _=>OZIsland.Solid("Chassi",new Color(.13f,.14f,.14f),.38f)
                };
            }
            renderer.sharedMaterials=renderer.sharedMaterials.Select(Resolve).ToArray();
        }
        var renderers=model.GetComponentsInChildren<MeshRenderer>();
        float bottom=renderers.Min(r=>r.bounds.min.y);
        model.transform.position+=Vector3.up*(parent.position.y-bottom);
        var lod=parent.gameObject.AddComponent<LODGroup>();
        lod.SetLODs(new[]{new LOD(.009f,renderers)});lod.RecalculateBounds();
    }

    public static void Hangar(Transform parent,Vector3 position,Material siding,Material frame,Material floor)
    {
        var hangar=Mesh(parent,"Hangar");
        hangar.transform.position=position;
        hangar.name="Hangar de manutenção";
        foreach(var r in hangar.GetComponentsInChildren<MeshRenderer>())
            r.sharedMaterials=r.sharedMaterials.Select(m=>m.name=="Concreto" ? floor : m.name=="Longarina" ? frame : siding).ToArray();
        foreach(var f in hangar.GetComponentsInChildren<MeshFilter>())
            f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
        var lootPos=hangar.transform.InverseTransformPoint(position+new Vector3(-3,.06f,2));
        Chest(hangar.transform,lootPos,25,true);
    }

    public static void Market(Transform parent,Vector3 position,float yaw)
    {
        var site=new GameObject("Mercado municipal").transform;
        site.SetParent(parent,false);
        site.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        var levels=new LOD[2];
        for(int index=0;index<2;index++)
        {
            var model=Mesh(site,index==0 ? "Mercado" : "MercadoFar");
            var renderers=model.GetComponentsInChildren<MeshRenderer>();
            foreach(var r in renderers) r.sharedMaterials=r.sharedMaterials.Select(m=>Facade(m.name)).ToArray();
            levels[index]=new LOD(index==0 ? .11f : .008f,renderers);
        }
        var lod=site.gameObject.AddComponent<LODGroup>();
        lod.SetLODs(levels);
        lod.RecalculateBounds();
        var shell=Mesh(site,"MercadoCollision");
        foreach(var f in shell.GetComponentsInChildren<MeshFilter>())
        {
            f.GetComponent<MeshRenderer>().enabled=false;
            f.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
        }
        Chest(site,new Vector3(-2.6f,.08f,1.5f),83,true);
    }

    static Material Facade(string name)
    {
        string path=Generated+name+".mat";
        var result=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(result) return result;
        result=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        result.enableInstancing=true;
        if(name=="Facade_glass")
        {
            result.SetColor("_BaseColor",new Color(.23f,.33f,.37f));
            result.SetFloat("_Smoothness",.84f);
            result.SetFloat("_Metallic",.55f);
        }
        else
        {
            result.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+name+"_diff.jpg"));
            result.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+name+"_nor_gl.jpg"));
            result.EnableKeyword("_NORMALMAP");
            result.SetFloat("_Smoothness",.25f);
            result.SetFloat("_Cull",(float)CullMode.Off);
        }
        AssetDatabase.CreateAsset(result,path);
        return result;
    }
}
