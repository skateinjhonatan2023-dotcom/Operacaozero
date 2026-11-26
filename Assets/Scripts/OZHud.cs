using UnityEngine;
using System.Collections.Generic;
public sealed class OZHud:MonoBehaviour {
 public OZGame game;
 Vector2 stickOrigin; int moveFinger=-1,lookFinger=-1,fireFinger=-1; int panel;
 string server="",user="",password=""; OZAccount account; bool nativeLobby,inventory,settings,expanded;
 Texture2D disc,ring,mini; readonly Dictionary<string,Texture2D> icons=new();
 GUIStyle label; float nextMap;
 void Start(){nativeLobby=TryGetComponent<OZLobby>(out _);if(!nativeLobby){account=gameObject.AddComponent<OZAccount>();server=PlayerPrefs.GetString("OZ_SERVER","");}
  disc=Circle(false);ring=Circle(true);
  foreach(var key in new[]{"run","jump","crouch","slide","bullet","aim","bag","heal","grenade","reload","gear","rifle","pistol","smg","sniper","shotgun","player","skull","armor","arrow","triangle"})Icon(key);
 }
 Rect R(float x,float y,float w,float h){var a=Screen.safeArea;return new Rect(a.xMin+x*a.width,Screen.height-a.yMax+y*a.height,w*a.width,h*a.height);}
 Rect C(float x,float y,float d){var a=Screen.safeArea;float size=d*a.height;return new Rect(a.xMin+x*a.width-size*.5f,Screen.height-a.yMax+y*a.height-size*.5f,size,size);}
 bool B(float x,float y,float w,float h,string title)=>GUI.Button(R(x,y,w,h),title);
 bool Hit(Rect rect,Vector2 p)=>rect.Contains(p);
 Vector2 GuiPoint(Vector2 p)=>new Vector2(p.x,Screen.height-p.y);
 bool InCircle(Rect rect,Vector2 p)=>(p-rect.center).sqrMagnitude<=rect.width*rect.width*.25f;
 Rect Stick=>C(.117f,.815f,.275f);
 Rect Fire=>C(.857f,.735f,.238f);
 Rect Map=>C(.077f,.178f,.252f);
 void Update(){
  if(!game||!game.Playing){ClearTouches();return;}
  if(!mini)MakeMap();
  if(inventory||settings){ClearTouches();return;}
  var area=Screen.safeArea;if(area.height<=0)return;
  bool moveSeen=false,lookSeen=false,fireSeen=false;
  for(int i=0;i<Input.touchCount;i++){
   Touch t=Input.GetTouch(i);Vector2 p=GuiPoint(t.position);bool end=t.phase==TouchPhase.Canceled||t.phase==TouchPhase.Ended;
   if(t.phase==TouchPhase.Began){
    if(expanded){expanded=false;ClearTouches();return;}
    if(moveFinger<0&&InCircle(Stick,p)){moveFinger=t.fingerId;stickOrigin=Stick.center;}
    else if(fireFinger<0&&InCircle(Fire,p)){fireFinger=t.fingerId;game.Trigger=true;}
    else if(Activate(p)){}
    else if(lookFinger<0&&p.x>area.xMin+area.width*.25f){lookFinger=t.fingerId;}
   }
   if(t.fingerId==moveFinger){moveSeen=!end;game.Move=end?Vector2.zero:Vector2.ClampMagnitude(new Vector2(p.x-stickOrigin.x,stickOrigin.y-p.y)/(area.height*.09f),1);if(end)moveFinger=-1;}
   if(t.fingerId==lookFinger){lookSeen=!end;if(!end)game.Look+=new Vector2(t.deltaPosition.x,-t.deltaPosition.y)*(720f/area.height);else lookFinger=-1;}
   if(t.fingerId==fireFinger){fireSeen=!end;if(end)fireFinger=-1;else game.Trigger=true;}
  }
  if(!moveSeen){moveFinger=-1;game.Move=Vector2.zero;}
  if(!lookSeen)lookFinger=-1;
  if(!fireSeen){fireFinger=-1;game.Trigger=false;}
  if(!Application.isMobilePlatform&&Input.GetMouseButtonDown(0))Activate(GuiPoint(Input.mousePosition));
 }
 bool Activate(Vector2 p){
  if(Hit(Map,p)){expanded=!expanded;return true;}
  if(expanded){expanded=false;return true;}
  if(InCircle(C(.955f,.55f,.115f),p)){game.Aim=!game.Aim;return true;}
  if(InCircle(C(.955f,.73f,.115f),p)){game.Jump=true;return true;}
  if(InCircle(C(.955f,.90f,.115f),p)){game.Slide=true;return true;}
  if(InCircle(C(.858f,.93f,.105f),p)){game.Crouch=!game.Crouch;return true;}
  if(InCircle(C(.755f,.93f,.105f),p)){game.Reload=true;return true;}
  if(InCircle(C(.19f,.62f,.105f),p)){game.Sprint=!game.Sprint;return true;}
  if(InCircle(C(.047f,.48f,.105f),p)){inventory=true;return true;}
  if(InCircle(C(.047f,.61f,.105f),p)){game.HealRequested=true;return true;}
  if(InCircle(C(.975f,.038f,.057f),p)){settings=true;return true;}
  if(Hit(R(.93f,.19f,.05f,.075f),p)){game.GrenadeRequested=true;return true;}
  if(Hit(R(.80f,.14f,.15f,.045f),p)){game.SingleShot=!game.SingleShot;return true;}
  if(Hit(R(.80f,.075f,.18f,.065f),p)||Hit(R(.95f,.14f,.03f,.045f),p)){game.Reload=true;return true;}
  if(Hit(R(.80f,.19f,.125f,.075f),p)){game.SetWeapon(Secondary);return true;}
  if(Hit(R(.34f,.67f,.30f,.07f),p)&&game.NearbyInteraction!=""){game.InteractRequested=true;return true;}
  if(Hit(R(.34f,.76f,.30f,.07f),p)&&game.NearbyLoot!=""){game.LootRequested=true;return true;}
  if(game.mode==OZGame.Mode.Treino&&Hit(R(.02f,.34f,.15f,.065f),p)){game.Back();return true;}
  return false;
 }
 void ClearTouches(){moveFinger=lookFinger=fireFinger=-1;if(game){game.Move=Vector2.zero;game.Look=Vector2.zero;game.Trigger=false;}}
 void OnApplicationFocus(bool focus){if(!focus)ClearTouches();}
 void OnApplicationPause(bool paused){if(paused)ClearTouches();}
 void Fill(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
 void Draw(Texture2D t,Rect r,Color tint){if(!t)return;var old=GUI.color;GUI.color=tint;GUI.DrawTexture(r,t,ScaleMode.StretchToFill,true);GUI.color=old;}
 void Text(Rect r,string s,int size=24,TextAnchor anchor=TextAnchor.MiddleCenter,Color? color=null){
  if(label==null){label=new GUIStyle(GUI.skin.label);var lobby=GetComponent<OZLobby>();if(lobby&&lobby.bold)label.font=lobby.bold;else label.fontStyle=FontStyle.Bold;}
  label.fontSize=Mathf.Max(12,Mathf.RoundToInt(Screen.safeArea.height*size/720f));label.alignment=anchor;
  label.normal.textColor=new Color(0,0,0,.85f);GUI.Label(new Rect(r.x+1,r.y+1,r.width,r.height),s,label);
  label.normal.textColor=color??Color.white;GUI.Label(r,s,label);
 }
 void Control(Rect r,string key,bool active=false){
  Draw(disc,r,active?new Color(.90f,.65f,.20f,.48f):new Color(.025f,.035f,.045f,.34f));
  Draw(ring,r,new Color(1,1,1,.73f));float d=r.width*(key=="bullet" ? .72f : .66f);
  Draw(Icon(key),new Rect(r.center.x-d*.5f,r.center.y-d*.5f,d,d),Color.white);
 }
 void OnGUI(){
  if(!game)return;if(nativeLobby&&!game.Playing)return;
  GUI.skin.label.fontSize=Mathf.Clamp(Screen.height/35,14,36);GUI.skin.button.fontSize=Mathf.Clamp(Screen.height/40,13,30);
  if(!game.Player){GUI.Label(R(.1f,.1f,.8f,.15f),game.Result);return;}if(!game.Playing){GUI.Box(R(.02f,.04f,.34f,.9f),"OPERATION ZERO");GUI.Label(R(.04f,.12f,.28f,.07f),"Nível "+(1+game.XP/500)+"   XP "+game.XP);if(game.Result!=""){GUI.Label(R(.04f,.24f,.28f,.1f),game.Result);if(B(.04f,.4f,.28f,.10f,"VOLTAR AO LOBBY"))game.Back();return;}if(B(.04f,.24f,.28f,.10f,"BATTLE ROYALE • BOTS"))game.Begin(OZGame.Mode.BattleRoyale);if(B(.04f,.36f,.28f,.10f,"COMBATE • BOTS"))game.Begin(OZGame.Mode.Combate);if(B(.04f,.48f,.28f,.10f,"TREINAMENTO"))game.Begin(OZGame.Mode.Treino);if(B(.04f,.62f,.28f,.08f,"CONFIGURAÇÕES"))panel=panel==1?0:1;if(B(.04f,.72f,.28f,.08f,"CONTA E RANKING"))panel=panel==2?0:2;
 if(panel==1){GUI.Box(R(.62f,.08f,.36f,.8f),"GRÁFICOS");GUI.Label(R(.65f,.17f,.3f,.08f),"Sensibilidade");game.Sensitivity=GUI.HorizontalSlider(R(.65f,.25f,.3f,.06f),game.Sensitivity,.2f,3);for(int i=0;i<4;i++)if(B(.65f,.34f+i*.11f,.3f,.085f,new[]{"Leve","Média","Alta","Ultra"}[i]))game.SetQuality(i);if(B(.65f,.79f,.14f,.07f,"30 FPS"))game.SetFrameRate(30);if(B(.81f,.79f,.14f,.07f,"60 FPS"))game.SetFrameRate(60);if(B(.65f,.86f,.3f,.07f,"Salvar")){PlayerPrefs.SetFloat("OZ_SENS",game.Sensitivity);PlayerPrefs.Save();panel=0;}}
 if(panel==2){GUI.Box(R(.60f,.08f,.38f,.85f),"SERVIDOR EXPERIMENTAL");GUI.Label(R(.62f,.14f,.34f,.06f),"Endereço HTTPS");server=GUI.TextField(R(.62f,.20f,.34f,.07f),server);user=GUI.TextField(R(.62f,.29f,.34f,.07f),user);password=GUI.PasswordField(R(.62f,.38f,.34f,.07f),password,'*');if(B(.62f,.47f,.16f,.08f,"Entrar"))account.Auth(server,user,password,false);if(B(.80f,.47f,.16f,.08f,"Cadastrar"))account.Auth(server,user,password,true);if(B(.62f,.57f,.34f,.08f,"Ranking do servidor"))account.Rank(server);GUI.Label(R(.62f,.66f,.34f,.24f),string.IsNullOrEmpty(account.Ranking)?account.Status:account.Ranking);}return;}
  DrawGameplay();
 }
 int Secondary=>game.Weapon==3?0:3;
 string WeaponIcon(int slot)=>new[]{"rifle","smg","sniper","pistol","shotgun"}[slot];
 void DrawGameplay(){
  var black=new Color(.025f,.045f,.06f,.78f);var gold=new Color(1,.75f,.22f);
  var player=game.Player;Vector3 pp=player.transform.position;
  // Main strip: count and equipment panels follow the supplied reference.
  Fill(R(.805f,.012f,.15f,.055f),black);
  Draw(Icon("player"),C(.827f,.038f,.040f),Color.white);Text(R(.838f,.012f,.04f,.055f),game.Survivors.ToString(),26);
  Draw(Icon("skull"),C(.897f,.038f,.037f),Color.white);Text(R(.907f,.012f,.04f,.055f),game.Kills.ToString(),26);
  Control(C(.975f,.038f,.057f),"gear");
  Fill(R(.80f,.075f,.18f,.11f),black);Fill(R(.80f,.180f,.18f,.004f),new Color(1,1,1,.35f));
  Text(R(.81f,.079f,.09f,.066f),$"{game.Ammo} / {game.Reserve}",32,TextAnchor.MiddleLeft);
  Draw(Icon(WeaponIcon(game.Weapon)),R(.895f,.077f,.077f,.062f),Color.white);
  Text(R(.81f,.14f,.13f,.04f),game.SingleShot?"Semiautomático":"Automático",18,TextAnchor.MiddleLeft);
  Draw(Icon("reload"),C(.966f,.159f,.026f),Color.white);
  Fill(R(.80f,.19f,.125f,.075f),new Color(.025f,.045f,.06f,.58f));
  Draw(Icon(WeaponIcon(Secondary)),R(.86f,.193f,.06f,.047f),new Color(1,1,1,.75f));
  Text(R(.807f,.222f,.075f,.037f),$"{game.GetAmmo(Secondary)} / {game.GetReserve(Secondary)}",20,TextAnchor.MiddleLeft);
  Fill(R(.93f,.19f,.05f,.075f),black);Draw(Icon("grenade"),C(.949f,.225f,.049f),Color.white);Text(R(.961f,.228f,.02f,.03f),game.Grenades.ToString(),20);
  // Ticked scrolling compass, not a static heading label.
  float heading=Mathf.Repeat(game.Heading,360);
  Fill(R(.32f,.012f,.36f,.055f),new Color(.025f,.045f,.06f,.30f));
  for(int step=-4;step<=4;step++){
   float angle=Mathf.Floor(heading/15)*15+step*15;
   float x=.5f+(angle-heading)/15*.042f;
   if(x<.324f||x>.676f)continue;
   int deg=Mathf.RoundToInt(Mathf.Repeat(angle,360));
   string tick=deg%45==0?new[]{"N","NE","L","SE","S","SO","O","NO"}[deg/45]:deg.ToString();
   Fill(R(x,.015f,.0012f,.012f),new Color(1,1,1,.7f));
   Text(R(x-.022f,.029f,.044f,.03f),tick,deg%45==0?23:18);
  }
  var needle=C(.5f,.018f,.018f);var compassMatrix=GUI.matrix;GUIUtility.RotateAroundPivot(180,needle.center);Draw(Icon("triangle"),needle,Color.white);GUI.matrix=compassMatrix;
  if(game.mode==OZGame.Mode.BattleRoyale){Fill(R(.32f,.075f,.36f,.048f),new Color(.52f,.09f,.06f,.78f));Text(R(.32f,.075f,.36f,.048f),game.Deploying?"POUSO • DIRECIONE A QUEDA":game.ZoneSeconds>0?$"ZONA FECHA EM {Mathf.CeilToInt(game.ZoneSeconds)}s":"ZONA SEGURA DIMINUINDO",20);}
  DrawMap(Map,pp,false);
  if(game.mode==OZGame.Mode.Treino){Fill(R(.02f,.34f,.15f,.065f),black);Text(R(.02f,.34f,.15f,.065f),"VOLTAR",20);}
  Control(C(.047f,.48f,.105f),"bag");Control(C(.047f,.61f,.105f),"heal");Text(R(.066f,.645f,.04f,.036f),game.Medkits.ToString(),18);
  Control(C(.19f,.62f,.105f),"run",game.Sprint);
  Draw(disc,Stick,new Color(.04f,.06f,.07f,.32f));Draw(ring,Stick,new Color(1,1,1,.48f));
  for(int i=0;i<4;i++){float angle=i*90;var dir=Quaternion.Euler(0,0,angle)*Vector3.up;var center=Stick.center-new Vector2(dir.x,dir.y)*Stick.width*.39f;float size=Stick.width*.085f;var ar=new Rect(center.x-size*.5f,center.y-size*.5f,size,size);var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(angle,center);Draw(Icon("triangle"),ar,new Color(1,1,1,.5f));GUI.matrix=matrix;}
  var knob=Stick;knob.width=knob.height=Stick.width*.44f;knob.center=Stick.center+new Vector2(game.Move.x,-game.Move.y)*Stick.width*.27f;
  Draw(disc,knob,new Color(.87f,.89f,.91f,.63f));Draw(ring,knob,Color.white);
  Control(Fire,"bullet",game.Trigger);Control(C(.955f,.55f,.115f),"aim",game.Aim);Control(C(.955f,.73f,.115f),"jump");
  Control(C(.955f,.90f,.115f),"slide");Control(C(.858f,.93f,.105f),"crouch",game.Crouch);Control(C(.755f,.93f,.105f),"reload");
  Fill(R(.285f,.916f,.37f,.073f),new Color(.025f,.045f,.06f,.54f));
  Draw(Icon("armor"),C(.302f,.933f,.028f),Color.white);Text(R(.312f,.920f,.05f,.026f),Mathf.CeilToInt(player.armor).ToString(),18,TextAnchor.MiddleLeft);
  for(int i=0;i<6;i++){float x=.366f+i*.047f;Fill(R(x,.927f,.045f,.013f),new Color(.24f,.26f,.26f,.8f));float value=Mathf.Clamp01(player.armor/50*6-i);Fill(R(x,.927f,.045f*value,.013f),gold);}
  Draw(Icon("heal"),C(.302f,.963f,.029f),Color.white);Text(R(.312f,.948f,.057f,.031f),$"HP {Mathf.CeilToInt(player.hp)}",17,TextAnchor.MiddleLeft);
  Fill(R(.366f,.956f,.28f,.018f),new Color(.25f,.29f,.29f));Fill(R(.366f,.956f,.28f*Mathf.Clamp01(player.hp/100),.018f),Color.white);
  var cross=C(.5f,.5f,.04f);float w=cross.width;
  Fill(new Rect(cross.center.x-w*.5f,cross.center.y-1,w,2),Color.white);Fill(new Rect(cross.center.x-1,cross.center.y-w*.5f,2,w),Color.white);
  if(game.HitUntil>Time.time)Text(R(.47f,.45f,.06f,.07f),"×",40,TextAnchor.MiddleCenter,gold);
  if(game.DamageUntil>Time.time){var local=Quaternion.Euler(0,-game.Heading,0)*(game.DamageSource-pp);string arrow=Mathf.Abs(local.x)>Mathf.Abs(local.z)?local.x>0?"→":"←":local.z>0?"↑":"↓";Text(R(.44f,.35f,.12f,.05f),"DANO "+arrow,24,TextAnchor.MiddleCenter,new Color(1,.3f,.2f));}
  if(game.NearbyInteraction!=""){Fill(R(.34f,.67f,.30f,.07f),black);Text(R(.34f,.67f,.30f,.07f),"INTERAGIR • "+game.NearbyInteraction,20);}
  if(game.NearbyLoot!=""){Fill(R(.34f,.76f,.30f,.07f),black);Text(R(.34f,.76f,.30f,.07f),"COLETAR • "+game.NearbyLoot,20);}
  if(expanded){Fill(R(.22f,.05f,.56f,.86f),black);DrawMap(R(.29f,.09f,.42f,.77f),pp,true);if(B(.66f,.06f,.10f,.07f,"FECHAR"))expanded=false;}
  if(inventory||settings)DrawPanel(black);
 }
 void DrawPanel(Color black){
  Fill(R(.24f,.13f,.52f,.74f),black);Text(R(.26f,.15f,.48f,.07f),inventory?"EQUIPAMENTO":"AJUSTES DE PARTIDA",30);
  if(inventory){
   for(int i=0;i<game.WeaponCount;i++)if(B(.29f,.24f+i*.085f,.42f,.075f,game.GetWeaponName(i)+(i==game.Weapon?"  • EQUIPADA":""))){game.SetWeapon(i);inventory=false;}
   Text(R(.27f,.69f,.46f,.06f),$"Kits {game.Medkits}   Granadas {game.Grenades}",22);
  }else{
   if(B(.29f,.24f,.42f,.08f,game.FPS?"CÂMERA FPS":"CÂMERA TPS"))game.FPS=!game.FPS;
   if(B(.29f,.34f,.42f,.08f,game.LeftShoulder?"OMBRO ESQUERDO":"OMBRO DIREITO"))game.LeftShoulder=!game.LeftShoulder;
   Text(R(.29f,.43f,.42f,.05f),"SENSIBILIDADE",20);game.Sensitivity=GUI.HorizontalSlider(R(.29f,.49f,.42f,.06f),game.Sensitivity,.2f,3);
   for(int i=0;i<4;i++)if(B(.28f+i*.11f,.58f,.105f,.07f,new[]{"LEVE","MÉDIA","ALTA","ULTRA"}[i]))game.SetQuality(i);
   if(B(.29f,.68f,.42f,.07f,"VOLTAR AO LOBBY")){game.Back();return;}
  }
  if(B(.29f,.77f,.42f,.07f,"FECHAR")){inventory=settings=false;PlayerPrefs.SetFloat("OZ_SENS",game.Sensitivity);PlayerPrefs.Save();}
 }
 void MakeMap(){
  var terrain=Terrain.activeTerrain;if(!terrain)return;
  const int n=192;mini=new Texture2D(n,n,TextureFormat.RGBA32,false);mini.wrapMode=TextureWrapMode.Clamp;
  var pixels=new Color32[n*n];var data=terrain.terrainData;
  for(int y=0;y<n;y++)for(int x=0;x<n;x++){
   float u=x/(n-1f),v=y/(n-1f);float h=data.GetInterpolatedHeight(u,v)+terrain.transform.position.y;
   Color c=h<.15f?new Color(.08f,.26f,.36f):Color.Lerp(new Color(.24f,.36f,.20f),new Color(.61f,.59f,.45f),Mathf.Clamp01(h/75));
   float shade=Mathf.Clamp(1+(h-data.GetInterpolatedHeight(Mathf.Clamp01(u+.01f),v)-terrain.transform.position.y)*.018f,.7f,1.2f);
   pixels[y*n+x]=c*shade;pixels[y*n+x].a=255;
  }
  var centers=new[]{new Vector2(-690,-270),new Vector2(0,0),new Vector2(590,430),new Vector2(660,-350),new Vector2(-440,500),new Vector2(-310,-340),new Vector2(330,300),new Vector2(-140,650),new Vector2(330,-570),new Vector2(-500,-690)};
  int[,] roads={{0,5},{5,1},{1,6},{6,2},{2,3},{3,8},{8,5},{5,9},{0,9},{1,4},{4,7},{7,6}};
  for(int i=0;i<roads.GetLength(0);i++){var a=centers[roads[i,0]];var b=centers[roads[i,1]];int count=Mathf.CeilToInt(Vector2.Distance(a,b)/5);for(int j=0;j<=count;j++){var p=Vector2.Lerp(a,b,j/(float)count);int x=Mathf.Clamp(Mathf.RoundToInt((p.x/2000+.5f)*(n-1)),0,n-1),y=Mathf.Clamp(Mathf.RoundToInt((p.y/2000+.5f)*(n-1)),0,n-1);pixels[y*n+x]=new Color32(165,159,134,255);}}
  foreach(var p in centers){int x=Mathf.RoundToInt((p.x/2000+.5f)*(n-1)),y=Mathf.RoundToInt((p.y/2000+.5f)*(n-1));for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)pixels[(y+dy)*n+x+dx]=new Color32(222,190,103,255);}
  mini.SetPixels32(pixels);mini.Apply(false,true);
 }
 void DrawMap(Rect r,Vector3 p,bool large){
  Fill(new Rect(r.x-2,r.y-2,r.width+4,r.height+4),new Color(.85f,.88f,.86f,.8f));Draw(mini,r,Color.white);
  float radius=Mathf.Max(1,game.MapRadius);
  if(game.mode==OZGame.Mode.BattleRoyale){float d=game.Zone/radius;var circle=new Rect(r.center.x-r.width*d*.5f,r.center.y-r.height*d*.5f,r.width*d,r.height*d);Draw(ring,circle,Color.white);}
  Vector2 pt=new Vector2(r.center.x+Mathf.Clamp(p.x/radius,-1,1)*r.width*.5f,r.center.y-Mathf.Clamp(p.z/radius,-1,1)*r.height*.5f);
  var arrow=new Rect(pt.x-9,pt.y-9,18,18);var mat=GUI.matrix;GUIUtility.RotateAroundPivot(game.Heading,pt);Draw(Icon("arrow"),arrow,new Color(.30f,.91f,1));GUI.matrix=mat;
  Fill(new Rect(r.x,r.yMax-r.height*.16f,r.width,r.height*.16f),new Color(0,0,0,.65f));Text(new Rect(r.x,r.yMax-r.height*.16f,r.width,r.height*.16f),"ILHA ZERO",large?23:19);
 }
 static Texture2D Circle(bool outline){const int n=96;var t=new Texture2D(n,n,TextureFormat.RGBA32,false);var p=new Color32[n*n];
  for(int y=0;y<n;y++)for(int x=0;x<n;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(47.5f,47.5f));float a=outline?Mathf.Clamp01(1-Mathf.Abs(d-45)/1.6f):Mathf.Clamp01(46-d);p[y*n+x]=new Color(1,1,1,a);}
  t.SetPixels32(p);t.Apply(false,true);return t;}
 Texture2D Icon(string key){
  if(!icons.TryGetValue(key,out var t)){t=OZHudArt.Create(key);icons[key]=t;}
  return t;
 }
 void OnDestroy(){if(disc)Destroy(disc);if(ring)Destroy(ring);if(mini)Destroy(mini);foreach(var t in icons.Values)if(t)Destroy(t);}
}
