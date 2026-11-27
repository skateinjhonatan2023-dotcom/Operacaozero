using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal static class OZChartBaker
{
    const int Resolution=768;
    const string Path="Assets/Content/Generated/IlhaZeroChart.png";

    public static void Bake(GameObject island,Terrain terrain)
    {
        var chart=island.AddComponent<OZMapChart>();
        var data=terrain.terrainData;
        chart.origin=new Vector2(terrain.transform.position.x,terrain.transform.position.z);
        chart.size=new Vector2(data.size.x,data.size.z);
        var heights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);
        var pixels=new Color32[Resolution*Resolution];
        var low=new Color(.26f,.36f,.19f);
        var high=new Color(.62f,.58f,.46f);
        for(int y=0;y<Resolution;y++) for(int x=0;x<Resolution;x++)
        {
            int ix=x*(data.heightmapResolution-1)/(Resolution-1),iz=y*(data.heightmapResolution-1)/(Resolution-1);
            float world=terrain.transform.position.y+heights[iz,ix]*data.size.y;
            float slope=(heights[iz,ix]-heights[iz,Mathf.Min(ix+1,data.heightmapResolution-1)])*data.size.y;
            Color tint=world<.15f ? new Color(.12f,.28f,.35f) :
                world<3 ? new Color(.65f,.59f,.42f) : Color.Lerp(low,high,Mathf.Clamp01(world/75));
            tint*=Mathf.Clamp(1+slope*.09f,.72f,1.2f);
            pixels[y*Resolution+x]=tint;
        }
        Vector2Int Pixel(Vector3 p)
        {
            Vector2 uv=chart.Coordinates(p);
            return new Vector2Int(Mathf.RoundToInt(uv.x*(Resolution-1)),Mathf.RoundToInt(uv.y*(Resolution-1)));
        }
        void Paint(int x,int y,Color32 c)
        {
            if(x>=0 && y>=0 && x<Resolution && y<Resolution) pixels[y*Resolution+x]=c;
        }
        foreach(var road in island.GetComponentsInChildren<MeshFilter>())
        {
            if(road.name!="Estrada" || !road.sharedMesh)continue;
            var v=road.sharedMesh.vertices;
            for(int k=0;k+3<v.Length;k+=2)
            {
                var a=Pixel(road.transform.TransformPoint((v[k]+v[k+1])*.5f));
                var b=Pixel(road.transform.TransformPoint((v[k+2]+v[k+3])*.5f));
                int steps=Mathf.Max(1,Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y));
                for(int s=0;s<=steps;s++)
                {
                    Vector2 point=Vector2.Lerp(a,b,(float)s/steps);
                    int x=Mathf.RoundToInt(point.x),y=Mathf.RoundToInt(point.y);
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)Paint(x+dx,y+dy,new Color32(155,148,123,255));
                }
            }
        }
        var supplies=new List<Vector3>();
        foreach(var t in island.GetComponentsInChildren<Transform>())
        {
            if(t.name!="Casa explorável" && t.name!="Depósito" && t.name!="Mercado municipal" && t.name!="Hangar de manutenção")continue;
            var center=Pixel(t.position);
            for(int y=-1;y<=1;y++)for(int x=-2;x<=2;x++)Paint(center.x+x,center.y+y,new Color32(221,189,128,255));
            if(t.name=="Mercado municipal" || t.name=="Hangar de manutenção")
                supplies.Add(t.TransformPoint(new Vector3(0,.14f,0)));
        }
        chart.supplies=supplies.ToArray();
        var texture=new Texture2D(Resolution,Resolution,TextureFormat.RGBA32,false);
        texture.SetPixels32(pixels);texture.Apply(false);
        System.IO.File.WriteAllBytes(Path,texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(Path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(Path);
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.mipmapEnabled=true;
        importer.isReadable=false;
        var mobile=importer.GetPlatformTextureSettings("Android");
        mobile.overridden=true;mobile.maxTextureSize=1024;mobile.format=TextureImporterFormat.ETC2_RGB4;
        importer.SetPlatformTextureSettings(mobile);
        importer.SaveAndReimport();
        chart.image=AssetDatabase.LoadAssetAtPath<Texture2D>(Path);
    }
}
