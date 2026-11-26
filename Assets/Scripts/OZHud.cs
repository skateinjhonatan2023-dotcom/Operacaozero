using UnityEngine;
public sealed class OZHud:MonoBehaviour {
 public OZGame game; Vector2 stickOrigin; int moveFinger=-1,lookFinger=-1,fireFinger=-1; int panel; string server="",user="",password=""; OZAccount account; bool nativeLobby;
 void Start(){nativeLobby=TryGetComponent<OZLobby>(out _);if(nativeLobby)return;account=gameObject.AddComponent<OZAccount>();server=PlayerPrefs.GetString("OZ_SERVER","");}
 Rect R(float x,float y,float w,float h){var a=Screen.safeArea;return new Rect(a.xMin+x*a.width,Screen.height-a.yMax+y*a.height,w*a.width,h*a.height);}
 bool B(float x,float y,float w,float h,string title)=>GUI.Button(R(x,y,w,h),title);
 static bool Inside(Vector2 p,float x,float y,float w,float h)=>new Rect(x,y,w,h).Contains(p);
 void Update(){
  if(!game.Playing){ClearTouches();return;}
  Rect area=Screen.safeArea;
  if(area.width<=0||area.height<=0){ClearTouches();return;}
  bool moveSeen=false,lookSeen=false,fireSeen=false;
  for(int i=0;i<Input.touchCount;i++){
   Touch t=Input.GetTouch(i);
   Vector2 p=t.position;
   Vector2 n=new Vector2((p.x-area.xMin)/area.width,(area.yMax-p.y)/area.height);
   bool end=t.phase==TouchPhase.Canceled||t.phase==TouchPhase.Ended;
   if(t.phase==TouchPhase.Began){
    if(moveFinger<0&&Inside(n,.02f,.58f,.27f,.39f)){moveFinger=t.fingerId;stickOrigin=p;}
    else if(fireFinger<0&&Inside(n,.83f,.38f,.15f,.30f)){fireFinger=t.fingerId;game.Trigger=true;}
    else if(lookFinger<0&&Inside(n,.30f,.20f,.52f,.66f)
      &&!Inside(n,.68f,.67f,.14f,.10f)&&!Inside(n,.68f,.80f,.14f,.10f)
      &&!Inside(n,.30f,.72f,.32f,.08f)&&!Inside(n,.30f,.81f,.32f,.07f))
     lookFinger=t.fingerId;
   }
   if(t.fingerId==moveFinger){moveSeen=!end;game.Move=end?Vector2.zero:Vector2.ClampMagnitude((p-stickOrigin)/(area.height*.12f),1);if(end)moveFinger=-1;}
   if(t.fingerId==lookFinger){lookSeen=!end;if(!end)game.Look+=new Vector2(t.deltaPosition.x,-t.deltaPosition.y)*(720f/area.height);else lookFinger=-1;}
   if(t.fingerId==fireFinger){fireSeen=!end;if(end){fireFinger=-1;game.Trigger=false;}}
  }
  if(!moveSeen){moveFinger=-1;game.Move=Vector2.zero;}
  if(!lookSeen)lookFinger=-1;
  if(!fireSeen){fireFinger=-1;game.Trigger=false;}
 }
 void ClearTouches(){moveFinger=lookFinger=fireFinger=-1;game.Move=Vector2.zero;game.Look=Vector2.zero;game.Trigger=false;}
 void OnApplicationFocus(bool focus){if(!focus&&game)ClearTouches();}
 void OnApplicationPause(bool paused){if(paused&&game)ClearTouches();}
 void OnGUI(){if(nativeLobby&&!game.Playing)return;GUI.skin.label.fontSize=Mathf.Clamp(Screen.height/35,14,36);GUI.skin.button.fontSize=Mathf.Clamp(Screen.height/40,13,30);if(!game.Player){GUI.Label(R(.1f,.1f,.8f,.15f),game.Result);return;}if(!game.Playing){GUI.Box(R(.02f,.04f,.34f,.9f),"OPERATION ZERO");GUI.Label(R(.04f,.12f,.28f,.07f),"Nível "+(1+game.XP/500)+"   XP "+game.XP);if(game.Result!=""){GUI.Label(R(.04f,.24f,.28f,.1f),game.Result);if(B(.04f,.4f,.28f,.10f,"VOLTAR AO LOBBY"))game.Back();return;}if(B(.04f,.24f,.28f,.10f,"BATTLE ROYALE • BOTS"))game.Begin(OZGame.Mode.BattleRoyale);if(B(.04f,.36f,.28f,.10f,"COMBATE • BOTS"))game.Begin(OZGame.Mode.Combate);if(B(.04f,.48f,.28f,.10f,"TREINAMENTO"))game.Begin(OZGame.Mode.Treino);if(B(.04f,.62f,.28f,.08f,"CONFIGURAÇÕES"))panel=panel==1?0:1;if(B(.04f,.72f,.28f,.08f,"CONTA E RANKING"))panel=panel==2?0:2;
 if(panel==1){GUI.Box(R(.62f,.08f,.36f,.8f),"GRÁFICOS");GUI.Label(R(.65f,.17f,.3f,.08f),"Sensibilidade");game.Sensitivity=GUI.HorizontalSlider(R(.65f,.25f,.3f,.06f),game.Sensitivity,.2f,3);for(int i=0;i<4;i++)if(B(.65f,.34f+i*.11f,.3f,.085f,new[]{"Leve","Média","Alta","Ultra"}[i]))game.SetQuality(i);if(B(.65f,.79f,.14f,.07f,"30 FPS"))game.SetFrameRate(30);if(B(.81f,.79f,.14f,.07f,"60 FPS"))game.SetFrameRate(60);if(B(.65f,.86f,.3f,.07f,"Salvar")){PlayerPrefs.SetFloat("OZ_SENS",game.Sensitivity);PlayerPrefs.Save();panel=0;}}
 if(panel==2){GUI.Box(R(.60f,.08f,.38f,.85f),"SERVIDOR EXPERIMENTAL");GUI.Label(R(.62f,.14f,.34f,.06f),"Endereço HTTPS");server=GUI.TextField(R(.62f,.20f,.34f,.07f),server);user=GUI.TextField(R(.62f,.29f,.34f,.07f),user);password=GUI.PasswordField(R(.62f,.38f,.34f,.07f),password,'*');if(B(.62f,.47f,.16f,.08f,"Entrar"))account.Auth(server,user,password,false);if(B(.80f,.47f,.16f,.08f,"Cadastrar"))account.Auth(server,user,password,true);if(B(.62f,.57f,.34f,.08f,"Ranking do servidor"))account.Rank(server);GUI.Label(R(.62f,.66f,.34f,.24f),string.IsNullOrEmpty(account.Ranking)?account.Status:account.Ranking);}return;}
 GUI.Label(R(.025f,.025f,.42f,.08f),"HP "+Mathf.CeilToInt(game.Player.hp)+"   COLETE "+Mathf.CeilToInt(game.Player.armor)+"   CAP "+Mathf.CeilToInt(game.Player.Helmet)+"   K "+game.Kills);GUI.Label(R(.67f,.025f,.32f,.08f),game.WeaponName+"  "+game.Ammo+" / "+game.Reserve);GUI.Label(R(.43f,.46f,.14f,.08f),"       +");if(game.mode==OZGame.Mode.BattleRoyale)GUI.Label(R(.39f,.03f,.28f,.08f),game.ZoneSeconds>0?"Zona fecha em "+Mathf.CeilToInt(game.ZoneSeconds)+"s":"Zona "+Mathf.RoundToInt(game.Zone)+"m");
 if(nativeLobby&&game.mode==OZGame.Mode.Treino&&B(.19f,.13f,.12f,.075f,"VOLTAR")){game.Back();return;}
 if(game.Deploying)GUI.Label(R(.36f,.31f,.32f,.08f),"POUSO • DIRECIONE A QUEDA");
 GUI.Box(R(.02f,.13f,.15f,.24f),"MAPA");foreach(var bot in game.bots)if(bot&&!bot.Dead){var p=bot.transform.position;GUI.Label(R(.095f+p.x/140*.06f,.24f-p.z/140*.08f,.03f,.04f),"·");}var pp=game.Player.transform.position;GUI.Label(R(.095f+pp.x/140*.06f,.24f-pp.z/140*.08f,.04f,.05f),"▲");
 if(game.NearbyLoot!=""&&B(.30f,.72f,.32f,.08f,"COLETAR "+game.NearbyLoot))game.LootRequested=true;
 if(B(.30f,.81f,.32f,.07f,"KIT MÉDICO ×"+game.Medkits))game.HealRequested=true;
 GUI.Box(R(.045f,.62f,.22f,.28f),"ANALÓGICO");GUI.Label(R(.13f+game.Move.x*.07f,.73f-game.Move.y*.08f,.07f,.08f),"●");GUI.Box(R(.83f,.38f,.15f,.30f),"ATIRAR");if(B(.83f,.24f,.15f,.09f,"MIRA"))game.Aim=!game.Aim;if(B(.68f,.67f,.14f,.10f,"PULAR"))game.Jump=true;if(B(.83f,.70f,.15f,.10f,"RECARREGAR"))game.Reload=true;if(B(.68f,.80f,.14f,.10f,"AGACHAR"))game.Crouch=!game.Crouch;if(B(.83f,.82f,.15f,.10f,"DESLIZAR"))game.Slide=true;if(B(.04f,.47f,.18f,.09f,"CORRER"))game.Sprint=!game.Sprint;if(B(.83f,.13f,.15f,.08f,"FPS / TPS"))game.FPS=!game.FPS;if(!game.FPS&&B(.67f,.13f,.15f,.08f,"OMBRO"))game.LeftShoulder=!game.LeftShoulder;for(int i=0;i<4;i++)if(B(.30f+i*.09f,.89f,.085f,.09f,new[]{"AR","SMG","SR","HG"}[i]))game.SetWeapon(i);
 }
}
