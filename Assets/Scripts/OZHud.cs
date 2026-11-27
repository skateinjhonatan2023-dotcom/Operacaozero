using UnityEngine;
using System.Collections.Generic;
[DefaultExecutionOrder(-10)]
public sealed class OZHud:MonoBehaviour {
 public OZGame game;
 Vector2 stickOrigin; int moveFinger=-1,lookFinger=-1,fireFinger=-1; int panel;
 string server="",user="",password=""; OZAccount account; bool nativeLobby,inventory,settings,expanded;
 Texture2D disc,ring,mini; readonly Dictionary<string,Texture2D> icons=new();
 GUIStyle label;
 OZMapChart chart;
 static readonly string[] WeaponIcons={"rifle","smg","sniper","pistol","shotgun"};
 static readonly string[] Bearings={"N","NE","L","SE","S","SO","O","NO"};
 static readonly string[] Presets={"Leve","Médio","Alto","Ultra"};
 Rect Bag=>C(.035f,.52f,.085f);
 Rect Heal=>C(.035f,.626f,.085f);
 Rect Run=>C(.173f,.696f,.083f);
 Rect Aim=>C(.954f,.57f,.093f);
 Rect Jump=>C(.954f,.721f,.093f);
 Rect Slide=>C(.95f,.872f,.098f);
 Rect Crouch=>C(.855f,.90f,.098f);
 Rect Swap=>C(.781f,.891f,.090f);
 void Start(){nativeLobby=TryGetComponent<OZLobby>(out _);if(!nativeLobby){account=gameObject.AddComponent<OZAccount>();server=PlayerPrefs.GetString("OZ_SERVER","");}
  chart=FindAnyObjectByType<OZMapChart>();
  if(chart)mini=chart.image;
  foreach(var texture in Resources.LoadAll<Texture2D>("OZCombatHUD")) icons[texture.name]=texture;
  disc=Icon("disc");ring=Icon("ring");
 }
 Rect R(float x,float y,float w,float h){var a=Screen.safeArea;return new Rect(a.xMin+x*a.width,Screen.height-a.yMax+y*a.height,w*a.width,h*a.height);}
 Rect C(float x,float y,float d){var a=Screen.safeArea;float size=d*a.height;float cx=Mathf.Clamp(x*a.width,size*.5f,a.width-size*.5f);return new Rect(a.xMin+cx-size*.5f,Screen.height-a.yMax+y*a.height-size*.5f,size,size);}
 bool B(float x,float y,float w,float h,string title)
 {
  var rect=R(x,y,w,h);
  Fill(rect,new Color(.13f,.18f,.21f,.86f));
  Fill(new Rect(rect.x,rect.yMax-1,rect.width,1),new Color(.74f,.83f,.86f,.6f));
  Text(rect,title,20);
  return GUI.Button(rect,GUIContent.none,GUIStyle.none);
 }
 bool Hit(Rect rect,Vector2 p)=>rect.Contains(p);
 Vector2 GuiPoint(Vector2 p)=>new Vector2(p.x,Screen.height-p.y);
 bool InCircle(Rect rect,Vector2 p)=>(p-rect.center).sqrMagnitude<=rect.width*rect.width*.25f;
 Rect Stick=>C(.117f,.815f,.19f);
 Rect Fire=>C(.857f,.735f,.16f);
 Rect Map { get {var a=Screen.safeArea;return new Rect(a.xMin+a.height*.008f,Screen.height-a.yMax+a.height*.075f,a.height*.24f,a.height*.202f);} }
 void Update(){
  if(!game||!game.Playing){ClearTouches();return;}
  if(!Input.GetMouseButton(0))game.PointerOverHud=false;

  if(inventory||settings){game.PointerOverHud=true;ClearTouches();return;}
  var area=Screen.safeArea;if(area.height<=0)return;
  bool moveSeen=false,lookSeen=false,fireSeen=false;
  for(int i=0;i<Input.touchCount;i++){
   Touch t=Input.GetTouch(i);Vector2 p=GuiPoint(t.position);bool end=t.phase==TouchPhase.Canceled||t.phase==TouchPhase.Ended;
   if(t.phase==TouchPhase.Began){
    if(expanded){expanded=false;ClearTouches();return;}
    if(!area.Contains(t.position))continue;
    if(moveFinger<0&&InCircle(Stick,p)){moveFinger=t.fingerId;stickOrigin=Stick.center;}
    else if(fireFinger<0&&InCircle(Fire,p)){fireFinger=t.fingerId;game.Trigger=true;}
    else if(Activate(p)){if(inventory||settings||expanded){ClearTouches();return;}}
    else if(lookFinger<0&&p.x>area.xMin+area.width*.25f){lookFinger=t.fingerId;}
   }
   if(t.fingerId==moveFinger){moveSeen=!end;game.Move=end?Vector2.zero:Vector2.ClampMagnitude(new Vector2(p.x-stickOrigin.x,stickOrigin.y-p.y)/(Stick.width*.39f),1);if(end)moveFinger=-1;}
   if(t.fingerId==lookFinger){lookSeen=!end;if(!end)game.Look+=new Vector2(t.deltaPosition.x,-t.deltaPosition.y)*(720f/area.height);else lookFinger=-1;}
   if(t.fingerId==fireFinger){fireSeen=!end;if(end)fireFinger=-1;else game.Trigger=true;}
  }
  if(!moveSeen){moveFinger=-1;game.Move=Vector2.zero;}
  if(!lookSeen)lookFinger=-1;
  if(!fireSeen){fireFinger=-1;game.Trigger=false;}
  if(!Application.isMobilePlatform && Input.GetMouseButtonDown(0))game.PointerOverHud=Activate(GuiPoint(Input.mousePosition));
 }
 bool Activate(Vector2 p){
  if(Hit(Map,p)){expanded=!expanded;return true;}
  if(expanded){expanded=false;return true;}
  if(InCircle(Aim,p)){game.Aim=!game.Aim;return true;}
  if(InCircle(Jump,p)){game.Jump=true;return true;}
  if(InCircle(Slide,p)){game.Slide=true;return true;}
  if(InCircle(Crouch,p)){game.Crouch=!game.Crouch;return true;}
  if(InCircle(Swap,p)){game.SetWeapon(Secondary);return true;}
  if(InCircle(Run,p)){game.Sprint=!game.Sprint;return true;}
  if(InCircle(Bag,p)){inventory=true;return true;}
  if(InCircle(Heal,p)){game.HealRequested=true;return true;}
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
 string WeaponIcon(int slot)=>WeaponIcons[slot];
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
   string tick=deg%45==0?Bearings[deg/45]:deg.ToString();
   Fill(R(x,.015f,.0012f,.012f),new Color(1,1,1,.7f));
   Text(R(x-.022f,.029f,.044f,.03f),tick,deg%45==0?23:18);
  }
  var needle=C(.5f,.018f,.018f);var compassMatrix=GUI.matrix;GUIUtility.RotateAroundPivot(180,needle.center);Draw(Icon("triangle"),needle,Color.white);GUI.matrix=compassMatrix;
  if(game.mode==OZGame.Mode.BattleRoyale){Fill(R(.32f,.075f,.36f,.048f),new Color(.52f,.09f,.06f,.78f));Text(R(.32f,.075f,.36f,.048f),game.Deploying?"POUSO • DIRECIONE A QUEDA":game.ZoneSeconds>0?$"ZONA FECHA EM {Mathf.CeilToInt(game.ZoneSeconds)}s":"ZONA SEGURA DIMINUINDO",20);}
  var header=new Rect(Map.x,Map.y-Screen.safeArea.height*.074f,Screen.safeArea.height*.51f,Screen.safeArea.height*.063f);
  Fill(header,black);
  Text(header,"  OPERATION ZERO",32,TextAnchor.MiddleLeft);
  Text(header,"V0.7  ",29,TextAnchor.MiddleRight,gold);
  DrawMap(Map,pp,false);
  var modeStrip=R(.012f,.948f,.17f,.045f);
  Fill(modeStrip,black);
  Text(modeStrip,"Gráficos  "+Presets[Mathf.Clamp(PlayerPrefs.GetInt("OZ_QUALITY",0),0,3)],17);

  if(game.mode==OZGame.Mode.Treino){Fill(R(.02f,.34f,.15f,.065f),black);Text(R(.02f,.34f,.15f,.065f),"VOLTAR",20);}
  Control(Bag,"bag");Control(Heal,"heal");Text(R(.045f,.64f,.028f,.036f),game.Medkits.ToString(),18);
  Control(Run,"run",game.Sprint);
  Draw(disc,Stick,new Color(.04f,.06f,.07f,.32f));Draw(ring,Stick,new Color(1,1,1,.48f));
  for(int i=0;i<4;i++){float angle=i*90;var dir=Quaternion.Euler(0,0,angle)*Vector3.up;var center=Stick.center-new Vector2(dir.x,dir.y)*Stick.width*.39f;float size=Stick.width*.085f;var ar=new Rect(center.x-size*.5f,center.y-size*.5f,size,size);var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(angle,center);Draw(Icon("triangle"),ar,new Color(1,1,1,.5f));GUI.matrix=matrix;}
  var knob=Stick;knob.width=knob.height=Stick.width*.44f;knob.center=Stick.center+new Vector2(game.Move.x,-game.Move.y)*Stick.width*.27f;
  Draw(disc,knob,new Color(.87f,.89f,.91f,.63f));Draw(ring,knob,Color.white);
  Control(Fire,"bullet",game.Trigger);Control(Aim,"aim",game.Aim);Control(Jump,"jump");
  Control(Slide,"slide");Control(Crouch,"crouch",game.Crouch);Control(Swap,"swap");
  Fill(R(.285f,.916f,.37f,.073f),new Color(.025f,.045f,.06f,.54f));
  Draw(Icon("armor"),C(.302f,.933f,.028f),Color.white);Text(R(.312f,.920f,.05f,.026f),Mathf.CeilToInt(player.armor).ToString(),18,TextAnchor.MiddleLeft);
  for(int i=0;i<6;i++){float x=.366f+i*.047f;Fill(R(x,.927f,.045f,.013f),new Color(.24f,.26f,.26f,.8f));float value=Mathf.Clamp01(player.armor/50*6-i);Fill(R(x,.927f,.045f*value,.013f),gold);}
  Draw(Icon("heal"),C(.302f,.963f,.029f),Color.white);Text(R(.312f,.948f,.057f,.031f),$"HP {Mathf.CeilToInt(player.hp)}",17,TextAnchor.MiddleLeft);
  Fill(R(.366f,.956f,.28f,.018f),new Color(.25f,.29f,.29f));Fill(R(.366f,.956f,.28f*Mathf.Clamp01(player.hp/100),.018f),Color.white);
  var cross=C(.5f,.5f,.04f);float w=cross.width;
  Fill(new Rect(cross.center.x-w*.5f,cross.center.y-1,w,2),Color.white);Fill(new Rect(cross.center.x-1,cross.center.y-w*.5f,2,w),Color.white);
  if(game.HitUntil>Time.time)Draw(Icon("hit"),C(.5f,.5f,.067f),gold);
  if(game.DamageUntil>Time.time)
  {
   Vector3 local=Quaternion.Euler(0,-game.Heading,0)*(game.DamageSource-pp);
   Vector2 dir=new Vector2(local.x,-local.z).normalized;
   var marker=C(.5f,.5f,.035f);
   marker.center+=dir*Screen.safeArea.height*.085f;
   var saved=GUI.matrix;
   GUIUtility.RotateAroundPivot(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,marker.center);
   Draw(Icon("triangle"),marker,new Color(1,.23f,.14f,.9f));
   GUI.matrix=saved;
  }
  if(game.NearbyInteraction!=""){Fill(R(.34f,.67f,.30f,.07f),black);Text(R(.34f,.67f,.30f,.07f),"INTERAGIR • "+game.NearbyInteraction,20);}
  if(game.NearbyLoot!=""){Fill(R(.34f,.76f,.30f,.07f),black);Text(R(.34f,.76f,.30f,.07f),"COLETAR • "+game.NearbyLoot,20);}
  if(expanded){Fill(R(.22f,.05f,.56f,.86f),black);DrawMap(C(.5f,.48f,.75f),pp,true);if(B(.66f,.06f,.10f,.07f,"FECHAR"))expanded=false;}
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
 void DrawMap(Rect r,Vector3 player,bool large)
 {
  if(!mini || !chart)return;
  Fill(new Rect(r.x-1,r.y-1,r.width+2,r.height+2),new Color(1,1,1,.72f));
  Vector2 uv=chart.Coordinates(player);
  float height=large ? 1 : Mathf.Min(1,230f/chart.size.y);
  float width=large ? 1 : Mathf.Min(1,height*r.width/r.height*chart.size.y/chart.size.x);
  var visible=new Rect(Mathf.Clamp(uv.x-width*.5f,0,1-width),Mathf.Clamp(uv.y-height*.5f,0,1-height),width,height);
  GUI.BeginGroup(r);
  GUI.DrawTextureWithTexCoords(new Rect(0,0,r.width,r.height),mini,visible);
  Vector2 Project(Vector2 worldUV)=>new Vector2((worldUV.x-visible.x)/width*r.width,(visible.yMax-worldUV.y)/height*r.height);
  if(game.mode==OZGame.Mode.BattleRoyale)
  {
   Vector2 center=Project(chart.Coordinates(Vector3.zero));
   float dx=game.Zone/chart.size.x/width*r.width,dy=game.Zone/chart.size.y/height*r.height;
   Draw(ring,new Rect(center.x-dx,center.y-dy,dx*2,dy*2),Color.white);
  }
  Vector2 point=Project(uv);
  float size=Screen.safeArea.height*(large ? .031f : .025f);
  var matrix=GUI.matrix;
  GUIUtility.RotateAroundPivot(game.Heading,point);
  Draw(Icon("arrow"),new Rect(point.x-size*.5f,point.y-size*.5f,size,size),new Color(.27f,.94f,.95f));
  GUI.matrix=matrix;
  GUI.EndGroup();
  var footer=new Rect(r.x,r.yMax-r.height*.18f,r.width,r.height*.18f);
  Fill(footer,new Color(0,0,0,.59f));Text(footer,"Ilha Zero",large?23:19);
 }
 Texture2D Icon(string key) => icons.TryGetValue(key,out var texture) ? texture : null;
}
