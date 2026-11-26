using UnityEngine;

// Original HUD pictograms, rasterized from filled vector shapes once, not text glyphs.
internal static class OZHudArt
{
    const int Size=128;
    sealed class Canvas
    {
        readonly Color32[] pixels=new Color32[Size*Size];
        internal void Shape(bool erase,params float[] xy)
        {
            int count=xy.Length/2;
            float minX=64,maxX=0,minY=64,maxY=0;
            for(int k=0;k<count;k++){minX=Mathf.Min(minX,xy[k*2]);maxX=Mathf.Max(maxX,xy[k*2]);minY=Mathf.Min(minY,xy[k*2+1]);maxY=Mathf.Max(maxY,xy[k*2+1]);}
            for(int y=Mathf.Max(0,Mathf.FloorToInt(minY*2));y<Mathf.Min(Size,Mathf.CeilToInt(maxY*2));y++)
            for(int x=Mathf.Max(0,Mathf.FloorToInt(minX*2));x<Mathf.Min(Size,Mathf.CeilToInt(maxX*2));x++)
            {
                int covered=0;
                for(int sy=0;sy<2;sy++)for(int sx=0;sx<2;sx++){
                    float px=(x+(sx+.5f)/2)*.5f,py=(y+(sy+.5f)/2)*.5f;
                    bool inside=false;
                    for(int i=0,j=count-1;i<count;j=i++){
                        float ax=xy[i*2],ay=xy[i*2+1],bx=xy[j*2],by=xy[j*2+1];
                        if((ay>py)!=(by>py) && px<(bx-ax)*(py-ay)/(by-ay)+ax)inside=!inside;
                    }
                    if(inside)covered++;
                }
                int index=(Size-1-y)*Size+x;
                byte opacity=(byte)(covered*255/4);
                pixels[index]=new Color32(255,255,255,erase?(byte)Mathf.Min(pixels[index].a,255-opacity):(byte)Mathf.Max(pixels[index].a,opacity));
            }
        }
        internal void P(params float[] xy)=>Shape(false,xy);
        internal void Cut(params float[] xy)=>Shape(true,xy);
        internal void Circle(float x,float y,float radius,bool erase=false)
        {
            var a=new float[80];
            for(int i=0;i<40;i++){float angle=i*Mathf.PI*2/40;a[i*2]=x+Mathf.Cos(angle)*radius;a[i*2+1]=y+Mathf.Sin(angle)*radius;}
            Shape(erase,a);
        }
        internal void Stroke(float ax,float ay,float bx,float by,float thickness)
        {
            var v=new Vector2(bx-ax,by-ay).normalized;var n=new Vector2(-v.y,v.x)*thickness*.5f;
            P(ax+n.x,ay+n.y,bx+n.x,by+n.y,bx-n.x,by-n.y,ax-n.x,ay-n.y);
            Circle(ax,ay,thickness*.5f);Circle(bx,by,thickness*.5f);
        }
        internal Texture2D Finish()
        {
            var t=new Texture2D(Size,Size,TextureFormat.RGBA32,false);
            t.filterMode=FilterMode.Bilinear;t.wrapMode=TextureWrapMode.Clamp;t.SetPixels32(pixels);t.Apply(false,true);return t;
        }
    }
    internal static Texture2D Create(string key)
    {
        var c=new Canvas();
        switch(key)
        {
            case "run":
                c.Circle(41,9,5.4f);
                c.P(34,15,43,17,39,26,32,35,28,39,21,35,24,29,29,19);
                c.P(31,17,27,25,17,29,9,26,8,22,17,23,26,15);
                c.P(40,18,46,26,54,22,57,26,47,33,42,32,35,25);
                c.P(24,33,32,36,25,48,14,57,7,56,8,52,18,44);
                c.P(28,33,37,36,46,43,49,56,44,59,39,48,27,42);break;
            case "jump":
                c.Circle(34,9,5.2f);
                c.P(29,15,39,16,42,30,38,38,27,36,25,25);
                c.P(29,17,22,13,15,4,10,7,19,20,26,25);
                c.P(38,16,44,12,49,4,54,7,48,20,41,25);
                c.P(27,34,34,37,23,46,14,43,11,47,22,54,27,52,39,39);
                c.P(35,34,43,37,47,47,54,51,52,56,43,53,36,43);break;
            case "crouch":
                c.Circle(41,12,5.3f);
                c.P(34,17,43,19,37,29,32,34,39,41,37,46,25,43,20,34,24,26);
                c.P(39,20,46,25,54,25,54,29,45,31,35,26);
                c.P(26,23,24,31,35,33,37,37,21,36,19,30,23,23);
                c.P(25,39,36,41,47,42,51,54,57,54,57,59,44,59,40,49,26,49);
                c.P(25,40,29,48,20,54,10,54,10,59,24,59,36,49);c.P(45,22,63,22,63,25,45,25);break;
            case "slide":
                c.Circle(19,15,5.5f);
                c.P(17,21,25,18,34,27,40,35,36,43,27,38,20,30,10,28,7,23);
                c.P(25,22,34,17,47,18,47,22,36,24,32,30);
                c.P(30,34,39,35,47,40,58,43,61,48,57,51,43,47,32,42);
                c.P(33,40,41,42,47,48,49,56,44,58,39,51,28,47);
                c.Stroke(6,55,29,55,2);c.Stroke(3,48,16,48,2);break;
            case "bullet":
                c.P(8,5,18,7,28,15,48,35,36,47,16,27,9,16);
                c.P(30,19,49,36,53,41,41,53,22,34);c.Cut(26,32,39,45,42,42,29,29);
                c.P(42,54,54,42,58,46,46,58);break;
            case "aim":
                c.Circle(32,32,23);c.Circle(32,32,20,true);
                c.P(30,2,34,2,34,21,30,21);c.P(30,43,34,43,34,62,30,62);
                c.P(2,30,21,30,21,34,2,34);c.P(43,30,62,30,62,34,43,34);c.Circle(32,32,2);break;
            case "bag":
                c.P(17,13,25,10,39,10,47,13,51,25,51,53,46,58,18,58,13,53,13,25);
                c.P(23,8,23,4,41,4,41,11,37,11,37,7,27,7,27,11);
                c.Cut(20,21,44,21,44,34,20,34);c.P(22,23,42,23,42,29,22,29);
                c.Cut(22,40,42,40,42,52,22,52);c.P(24,43,40,43,40,49,24,49);
                c.P(8,24,12,24,12,49,8,49);c.P(52,24,56,24,56,49,52,49);break;
            case "heal":
                c.P(5,18,12,13,52,13,59,18,59,49,52,54,12,54,5,49);
                c.P(22,13,22,6,42,6,42,13,37,13,37,10,27,10,27,13);
                c.Cut(28,21,36,21,36,29,44,29,44,37,36,37,36,45,28,45,28,37,20,37,20,29,28,29);
                c.Cut(10,20,14,20,14,47,10,47);c.Cut(50,20,54,20,54,47,50,47);break;
            case "grenade":
                c.P(24,17,41,17,47,26,48,44,42,54,32,59,22,54,16,44,17,26);
                c.P(23,17,23,9,41,9,45,14,45,19,39,19,39,13,27,13,27,17);
                c.Circle(49,11,7);c.Circle(49,11,4,true);c.P(42,18,48,17,51,40,47,40);
                c.Cut(20,30,44,30,44,32,20,32);c.Cut(19,42,45,42,45,44,19,44);c.Cut(30,22,33,22,33,53,30,53);break;
            case "reload":
                c.P(5,16,43,16,43,8,59,22,43,36,43,28,5,28);
                c.P(59,38,21,38,21,30,5,44,21,58,21,50,59,50);break;
            case "gear":
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var v=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var n=new Vector2(-v.y,v.x)*5;c.P(32+v.x*18+n.x,32+v.y*18+n.y,32+v.x*28+n.x,32+v.y*28+n.y,32+v.x*28-n.x,32+v.y*28-n.y,32+v.x*18-n.x,32+v.y*18-n.y);}
                c.Circle(32,32,21);c.Circle(32,32,9,true);break;
            case "rifle":
                c.P(1,22,11,22,13,27,23,27,23,22,37,22,37,24,50,24,50,27,63,27,63,30,49,30,49,34,37,34,40,45,36,48,32,36,26,36,23,43,19,43,20,35,13,35,5,40,1,40);
                c.P(26,18,31,18,31,23,26,23);c.P(45,19,48,19,48,25,45,25);c.Cut(25,32,32,32,32,34,25,34);break;
            case "pistol":
                c.P(6,16,59,16,59,26,47,26,46,31,32,31,23,53,10,53,17,29,6,27);
                c.P(33,28,43,28,43,37,30,37,30,34,39,34,39,31,33,31);c.Cut(10,19,52,19,52,21,10,21);break;
            case "smg":
                c.P(3,21,12,21,17,26,25,26,25,20,47,20,47,24,61,24,61,29,45,29,45,34,34,34,34,51,28,51,25,34,20,34,16,43,11,43,12,33,3,33);break;
            case "sniper":
                c.P(1,24,12,24,16,28,30,28,30,24,44,24,44,27,63,27,63,30,43,30,43,34,31,34,31,42,25,42,23,34,15,34,4,39,1,39);
                c.P(21,17,38,17,38,22,21,22);c.P(25,22,28,22,28,25,25,25);c.P(34,22,37,22,37,25,34,25);break;
            case "shotgun":
                c.P(1,24,13,24,17,28,34,28,34,24,61,24,61,29,45,29,45,34,20,34,14,40,1,40);c.P(32,31,47,31,47,35,32,35);break;
            case "player":
                c.Circle(32,13,9);c.P(21,24,43,24,49,29,55,51,9,51,15,29);c.Cut(27,24,32,33,37,24);break;
            case "skull":
                c.Circle(32,25,21);c.P(17,36,47,36,44,54,20,54);c.Circle(23,27,6,true);c.Circle(41,27,6,true);c.Cut(32,33,28,41,36,41);c.Cut(25,47,28,47,28,55,25,55);c.Cut(35,47,38,47,38,55,35,55);break;
            case "armor":
                c.P(13,9,24,9,28,21,36,21,40,9,51,9,57,35,48,56,16,56,7,35);c.Cut(20,32,44,32,44,36,20,36);c.Cut(20,43,44,43,44,47,20,47);break;
            case "arrow":
                c.P(32,5,10,55,32,42,54,55);c.Cut(32,17,32,42,48,49);break;
            case "triangle":
                c.P(32,9,54,54,10,54);break;
        }
        return c.Finish();
    }
}
