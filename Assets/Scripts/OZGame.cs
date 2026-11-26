using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
public sealed class OZGame : MonoBehaviour {
 public Material effectsMaterial;
 public OZActor soldier; public GameObject rifle; public AudioClip shotSound; public Transform[] spawns; public Camera view;
 public enum Mode { BattleRoyale, Combate, Treino } public Mode mode;
 public OZActor Player{get;private set;} public List<OZActor> bots=new(); public bool Playing{get;private set;} public string Result="";public int Kills{get;private set;} public float Zone{get;private set;}=140;
 public float Sensitivity=1, AdsSensitivity=.65f;
 public bool LeftShoulder;
 readonly RaycastHit[] cameraHits = new RaycastHit[32];
 readonly Collider[] headHits = new Collider[24];
 Vector3 slideDirection;
 SkinnedMeshRenderer[] playerBody;
 UnityEngine.Rendering.ShadowCastingMode[] bodyShadowModes;
 bool bodyHidden; public bool Aim, Sprint, Crouch; public Vector2 Move,Look; public bool Trigger,Jump,Reload,Slide; public bool FPS;
 public float MapRadius {get;private set;}=160;
 public float Heading=>yaw;
 public int Survivors {get {int n=Player&&!Player.Dead?1:0;foreach(var bot in bots)if(bot&&!bot.Dead)n++;return n;}}
 public bool AutoSprint {get;set;}
 public int BotDifficulty {get;set;}=1;
 public bool InteractRequested;
 public string NearbyInteraction {get;private set;}="";
 public float HitUntil {get;private set;}
 public float DamageUntil {get;private set;}
 public Vector3 DamageSource {get;private set;}
 OZDoor[] doors; OZDoor nearbyDoor; float nextDoorScan,nextDrop,protectedUntil;
 OZVehicle[] vehicles; OZVehicle nearbyVehicle,driving;
 public bool Driving=>driving!=null;
 public bool Gyro {get;set;}
 public float CameraFov {get;set;}=65;
 public float CameraSmooth {get;set;}=18;
 public bool GrenadeRequested;
 public int Grenades {get;private set;}=2;
 public bool SingleShot {get;set;}
 bool shotHeld;

 public bool HealRequested;
 public bool LootRequested;
 public int Medkits { get; private set; }
 public float ZoneSeconds { get; private set; }
 public string NearbyLoot { get; private set; } = "";
 readonly List<OZLoot> loot = new();
 OZZoneVisual zoneVisual;
 GameObject canopy;
 OZShotEffects shotEffects;
 UniversalRenderPipelineAsset activeQuality;
 UnityEngine.Rendering.RenderPipelineAsset previousQuality;
 readonly OZGraphicsBudget graphicsBudget = new OZGraphicsBudget();
 float nextZoneDraw;
 public bool Deploying { get; private set; }
 public int WeaponCount => names.Length;
 public string GetWeaponName(int index) => names[index];
 public int GetWeaponCapacity(int index) => capacity[index];
 public int GetWeaponDamage(int index) => damage[index];
 public int Weapon { get; private set; } public int Ammo=>ammo[Weapon]; public int Reserve=>reserve[Weapon]; public string WeaponName=>names[Weapon]; public int XP=>PlayerPrefs.GetInt("OZ_XP",0);
 readonly string[] names={"Rifle","SMG","Sniper","Pistola","Escopeta"};readonly int[] capacity={30,24,5,12,6},damage={34,24,95,30,60};readonly float[] rate={.13f,.08f,.85f,.24f,.72f},reach={100,65,220,45,28}; readonly int[] ammo={30,24,5,12,6},reserve={120,144,25,72,30};
 float yaw,pitch=8,vy,nextShot,reloadUntil,slideUntil,startTime; int reloadWeapon; CharacterController cc; AudioSource audioSource; bool finished;
 void Start(){if(!view)view=Camera.main;SetFrameRate(PlayerPrefs.GetInt("OZ_FPS",30));SetQuality(PlayerPrefs.GetInt("OZ_QUALITY",0));Sensitivity=PlayerPrefs.GetFloat("OZ_SENS",1);AdsSensitivity=Mathf.Clamp(PlayerPrefs.GetFloat("OZ_ADS_SENS",.65f),.2f,3f);audioSource=gameObject.AddComponent<AudioSource>();shotEffects=gameObject.AddComponent<OZShotEffects>();shotEffects.Initialize(effectsMaterial);if(!soldier||!rifle||!shotSound||!view||spawns==null||spawns.Length<8){Result="Conteúdo gráfico obrigatório ausente";enabled=false;return;} var terrain=Terrain.activeTerrain;if(terrain)MapRadius=Mathf.Min(terrain.terrainData.size.x,terrain.terrainData.size.z)*.5f;Zone=MapRadius*.9f;doors=FindObjectsByType<OZDoor>(FindObjectsSortMode.None);vehicles=FindObjectsByType<OZVehicle>(FindObjectsSortMode.None);CameraFov=Mathf.Clamp(PlayerPrefs.GetFloat("OZ_FOV",65),55,85);CameraSmooth=Mathf.Clamp(PlayerPrefs.GetFloat("OZ_CAM_SMOOTH",18),6,30);Gyro=SystemInfo.supportsGyroscope&&PlayerPrefs.GetInt("OZ_GYRO",0)==1;if(Gyro)Input.gyro.enabled=true;BotDifficulty=Mathf.Clamp(PlayerPrefs.GetInt("OZ_BOT_DIFFICULTY",1),0,2);AutoSprint=PlayerPrefs.GetInt("OZ_AUTO_SPRINT",0)==1;Player=Spawn(spawns[0].position);cc=Player.GetComponent<CharacterController>();playerBody=Player.GetComponentsInChildren<SkinnedMeshRenderer>(true);bodyShadowModes=new UnityEngine.Rendering.ShadowCastingMode[playerBody.Length];for(int i=0;i<playerBody.Length;i++)bodyShadowModes[i]=playerBody[i].shadowCastingMode;Player.Pose(0,false);UpdateCamera(true);}
 public static bool GroundHeight(Vector3 p,out float y){var t=Terrain.activeTerrain;y=0;if(!t)return false;var o=t.transform.position;var s=t.terrainData.size;float u=(p.x-o.x)/s.x,v=(p.z-o.z)/s.z;if(u<0||u>1||v<0||v>1)return false;y=o.y+t.terrainData.GetInterpolatedHeight(u,v);return true;}
 public static bool RecoverGround(CharacterController c){if(!c||!c.enabled||!GroundHeight(c.transform.position,out float y)||c.transform.position.y>=y-.18f)return false;var p=c.transform.position;c.enabled=false;p.y=y+.08f;c.transform.position=p;c.enabled=true;return true;}
 OZActor Spawn(Vector3 pos){if(GroundHeight(pos,out float ground))pos.y=Mathf.Max(pos.y,ground+.12f);var a=Instantiate(soldier,pos,Quaternion.identity);a.gameObject.SetActive(true);a.Setup();var ctrl=a.GetComponent<CharacterController>()??a.gameObject.AddComponent<CharacterController>();ctrl.enabled=false;ctrl.height=1.8f;ctrl.center=Vector3.up*.9f;ctrl.radius=.33f;ctrl.skinWidth=.035f;ctrl.stepOffset=.28f;ctrl.minMoveDistance=0;ctrl.enabled=true;return a;}
 public void Begin(Mode m){if(Playing||finished||!Player)return;mode=m;Playing=true;startTime=Time.time;protectedUntil=startTime+8;ZoneSeconds=MapRadius>200?55:25;nextDrop=startTime+80;int count=Mathf.Min(spawns.Length-1,m==Mode.BattleRoyale?19:4);for(int i=0;i<count;i++){int index=m==Mode.BattleRoyale?1+i*(spawns.Length-1)/count:i+1;Vector3 p=spawns[index].position;if(m!=Mode.BattleRoyale&&Vector3.Distance(p,spawns[0].position)>130){p=spawns[0].position+Quaternion.Euler(0,i*90,0)*Vector3.forward*35;if(Physics.Raycast(p+Vector3.up*100,Vector3.down,out var ground,200))p=ground.point+Vector3.up*.2f;}var a=Spawn(p);a.gameObject.AddComponent<OZBot>().Init(this,a);bots.Add(a);}
 SpawnLoot();
 if (mode==Mode.BattleRoyale) {
  Vector3 drop = spawns[0].position + Vector3.up * 36;
  cc.enabled=false;
  Player.transform.position=drop;
  cc.enabled=true;
  Deploying=true;
  canopy=OZCanopy.Create(Player.transform);
  zoneVisual=new GameObject("Limite da zona").AddComponent<OZZoneVisual>();
  zoneVisual.Initialize(effectsMaterial);
  UpdateCamera(true);
 }}
 public void Back(){SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);}
 public void SetWeapon(int i){if(i>=0&&i<capacity.Length&&Weapon!=i){Weapon=i;reloadUntil=0;}}
 void Update() {
  if (!Player) return;
  if (!Playing) {
   Player.Pose(0,false);
   if (!finished) {
    view.transform.position=Player.transform.position+new Vector3(0,1.35f,3.7f);
    view.transform.LookAt(Player.transform.position+Vector3.up*.95f);
   }
   return;
  }
  if (Player.Dead) {
   Finish(false);
   return;
  }
  if (activeQuality && graphicsBudget.Sample(Time.unscaledDeltaTime, Application.targetFrameRate))
   activeQuality.renderScale = graphicsBudget.Scale;
  float dt=Time.deltaTime;
  if(Driving)Drive(dt);else {if(RecoverGround(cc))vy=0;MovePlayer(dt);if(RecoverGround(cc))vy=0;}
  if (Player.transform.position.y < -30) Player.Damage(100,bypassEquipment:true);
  if (Player.Dead) {
   Finish(false);
   return;
  }
  UpdateCamera(false);
  UpdateLoot();
  UpdateInteraction();
  if(!Driving)UpdateWeapons();
  else {Trigger=false;Reload=false;GrenadeRequested=false;shotHeld=false;}
  if (mode==Mode.BattleRoyale && !Deploying) UpdateZone(dt);
  if (Player.Dead) Finish(false);
  else if (mode!=Mode.Treino && bots.TrueForAll(bot=>!bot||bot.Dead)) Finish(true);
 }

 void MovePlayer(float dt) {
  Vector2 input=Move+new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
  input=Vector2.ClampMagnitude(input,1);
  Vector2 look=Look;
  Look=Vector2.zero;
  if (!Application.isMobilePlatform && Input.GetMouseButton(1))
   look+=new Vector2(Input.GetAxis("Mouse X"),-Input.GetAxis("Mouse Y"))*12;
  float sensitivity = Aim ? AdsSensitivity : Sensitivity;
  if(Gyro&&SystemInfo.supportsGyroscope){if(!Input.gyro.enabled)Input.gyro.enabled=true;var gyro=Input.gyro.rotationRateUnbiased;look+=new Vector2(-gyro.y,-gyro.x)*dt*250f;}
  yaw=Mathf.Repeat(yaw+look.x*sensitivity*.18f+180f,360f)-180f;
  pitch=Mathf.Clamp(pitch+look.y*sensitivity*.18f,-65,65);
  Quaternion heading=Quaternion.Euler(0,yaw,0);
  Vector3 desired=heading*new Vector3(input.x,0,input.y);
  bool sprint=Sprint||Input.GetKey(KeyCode.LeftShift)||(AutoSprint&&input.y>.88f);
  bool crouch=Crouch||Input.GetKey(KeyCode.LeftControl);
  if (!Deploying && cc.isGrounded && Time.time>=slideUntil && (Slide||Input.GetKeyDown(KeyCode.C))) {
   slideDirection=desired.sqrMagnitude>.01f?desired.normalized:heading*Vector3.forward;
   slideUntil=Time.time+.55f;
  }
  Slide=false;
  bool sliding=Time.time<slideUntil;
  bool low=!Deploying && (crouch||sliding || (cc.height<1.5f && !CanStand()));
  float height=low?1.2f:1.8f;
  if(!Mathf.Approximately(cc.height,height)){cc.height=height;cc.center=Vector3.up*height*.5f;}
  Vector3 facing=sliding?slideDirection:desired;
  Quaternion bodyHeading=(!FPS&&!Aim&&!Trigger&&facing.sqrMagnitude>.01f)
   ? Quaternion.LookRotation(facing) : heading;
  Player.transform.rotation=FPS||Aim||Trigger?bodyHeading:
   Quaternion.Slerp(Player.transform.rotation,bodyHeading,1f-Mathf.Exp(-16f*dt));

  if (Deploying) vy=-5;
  else {
   if (cc.isGrounded) vy=Jump||Input.GetKeyDown(KeyCode.Space) ? 5.8f : -2;
   vy-=18*dt;
  }
  Jump=false;
  float speed=Deploying ? 8 : sliding ? 9 : low ? 2 : sprint && !Aim ? 6 : 3.8f;
  Vector3 movement=sliding ? slideDirection : desired;
  var collision=cc.Move((movement*speed+Vector3.up*vy)*dt);
  if (sliding && (collision&CollisionFlags.Sides)!=0) slideUntil=0;
  if (vy>0 && (collision&CollisionFlags.Above)!=0) vy=0;
  if (Deploying && ((collision&CollisionFlags.Below)!=0 || (GroundHeight(Player.transform.position,out float ground)&&Player.transform.position.y<=ground+.15f))) {
   Deploying=false;
   startTime=Time.time;protectedUntil=startTime+8;nextDrop=startTime+80;
   Destroy(canopy);
  }
  Player.Pose(movement.magnitude*speed,Aim,low,Deploying||!cc.isGrounded,reloadUntil>0);
 }

 bool CanStand() {
  float radius=Mathf.Max(.05f,cc.radius-cc.skinWidth);
  Vector3 feet=Player.transform.position;
  Vector3 bottom=feet+Vector3.up*(cc.height-cc.radius);
  Vector3 top=feet+Vector3.up*(1.8f-cc.radius);
  int count=Physics.OverlapCapsuleNonAlloc(bottom,top,radius,headHits,~0,QueryTriggerInteraction.Ignore);
  if(count==headHits.Length)return false;
  for(int i=0;i<count;i++)
   if(headHits[i]&&headHits[i].GetComponentInParent<OZActor>()!=Player)return false;
  return true;
 }

 void UpdateWeapons() {
  if ((Reload||Input.GetKeyDown(KeyCode.R)) && reloadUntil==0 && Ammo<capacity[Weapon] && Reserve>0) {
   reloadUntil=Time.time+1.5f;
   reloadWeapon=Weapon;
  }
  Reload=false;
  if (reloadUntil>0 && Time.time>=reloadUntil) {
   int loaded=Mathf.Min(capacity[reloadWeapon]-ammo[reloadWeapon],reserve[reloadWeapon]);
   ammo[reloadWeapon]+=loaded;
   reserve[reloadWeapon]-=loaded;
   reloadUntil=0;
  }
  bool shootDesktop=!Application.isMobilePlatform && Input.GetMouseButton(0) &&
   Input.mousePosition.y>Screen.height*.3f;
  bool pressed=Trigger||shootDesktop;
  if (!Deploying && pressed && (!SingleShot||!shotHeld) && reloadUntil==0 && Time.time>=nextShot && Ammo>0) Shoot();
  shotHeld=pressed;
  if(GrenadeRequested&&!Deploying&&Grenades>0){Grenades--;var grenade=new GameObject("Granada").AddComponent<OZGrenade>();grenade.Launch(this,Player.transform.position+Vector3.up*1.3f,view.transform.forward,effectsMaterial);}
  GrenadeRequested=false;
 }

 void UpdateZone(float dt) {
  float elapsed=Time.time-startTime;
  float initial=MapRadius*.9f,grace=MapRadius>200?55:25;
  ZoneSeconds=Mathf.Max(0,grace-elapsed);
  Zone=Mathf.Max(12,initial-Mathf.Max(0,elapsed-grace)*(initial-12)/(MapRadius>200?480f:150f));
  if(Time.time>=nextDrop){nextDrop=Time.time+100;SpawnAirdrop();}
  if (Time.time>=nextZoneDraw) {
   zoneVisual.SetRadius(Zone);
   nextZoneDraw=Time.time+.75f;
  }
  ApplyZoneDamage(Player,dt);
  foreach (var bot in bots)
   if (bot && !bot.Dead) ApplyZoneDamage(bot,dt);
 }

 void SpawnAirdrop(){
  var p=Random.insideUnitCircle*Mathf.Max(8,Zone*.65f);
  if(!Physics.Raycast(new Vector3(p.x,120,p.y),Vector3.down,out var ground,200,~0,QueryTriggerInteraction.Ignore))return;
  for(int i=0;i<4;i++)loot.Add(OZLoot.Create((OZLoot.Kind)i,ground.point+new Vector3(i%2,0,i/2)*1.3f,effectsMaterial));
 }
 void UpdateInteraction(){
  if(Driving){NearbyInteraction="SAIR DO VEÍCULO";if(InteractRequested||Input.GetKeyDown(KeyCode.F))ExitVehicle();InteractRequested=false;return;}
  if(Time.time>=nextDoorScan){
   nextDoorScan=Time.time+.2f;nearbyDoor=null;float best=3.2f*3.2f;
   if(doors!=null)foreach(var door in doors){if(!door)continue;float d=(door.transform.position-Player.transform.position).sqrMagnitude;if(d<best){best=d;nearbyDoor=door;}}
   nearbyVehicle=null;float carBest=4f*4f;if(vehicles!=null)foreach(var car in vehicles){if(!car)continue;float d=(car.transform.position-Player.transform.position).sqrMagnitude;if(d<carBest){carBest=d;nearbyVehicle=car;}}
   NearbyInteraction=!Deploying&&nearbyVehicle?nearbyVehicle.Title:nearbyDoor?"PORTA":"";
  }
  if(InteractRequested||Input.GetKeyDown(KeyCode.F)){if(!Deploying&&nearbyVehicle)EnterVehicle(nearbyVehicle);else if(nearbyDoor)nearbyDoor.Toggle();}
  InteractRequested=false;
 }
 void EnterVehicle(OZVehicle car){driving=car;cc.enabled=false;car.BeginDrive();FPS=false;Aim=false;Player.transform.position=car.Seat;Player.transform.rotation=car.transform.rotation;SetFirstPersonBody(true);}
 void Drive(float dt){Vector2 input=Vector2.ClampMagnitude(Move+new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical")),1);driving.Tick(input,dt);Player.transform.position=driving.Seat;Player.transform.rotation=driving.transform.rotation;yaw=driving.transform.eulerAngles.y;Look=Vector2.zero;Player.Pose(0,false);}
 void ExitVehicle(){
  if(!driving)return;
  Vector3 exit=driving.transform.position+driving.transform.right*3;
  if(!Physics.Raycast(exit+Vector3.up*4,Vector3.down,out var ground,9,~0,QueryTriggerInteraction.Ignore))return;
  exit=ground.point+Vector3.up*.15f;
  if(Physics.CheckCapsule(exit+Vector3.up*.36f,exit+Vector3.up*1.44f,.30f,~0,QueryTriggerInteraction.Ignore))return;
  driving.EndDrive();driving=null;Player.transform.position=exit;cc.enabled=true;SetFirstPersonBody(FPS);
 }
 public void GrenadeBlast(Vector3 center){
  if(!Playing)return;
  foreach(var bot in bots){if(!bot||bot.Dead)continue;float d=Vector3.Distance(center,bot.transform.position);if(d<7&&bot.Damage(90*(1-d/7f)))Kills++;}
  if(Player&&!Player.Dead){float d=Vector3.Distance(center,Player.transform.position);if(d<7)Player.Damage(65*(1-d/7f));}
 }
 public void BotHit(float amount,Vector3 source){if(!Playing||!Player||Player.Dead||Time.time<protectedUntil)return;DamageSource=source;DamageUntil=Time.time+1.2f;Player.Damage(amount);}
 void ApplyZoneDamage(OZActor actor,float dt) {
  Vector3 position=actor.transform.position;
  if (position.x*position.x+position.z*position.z>Zone*Zone)
   actor.Damage(dt*12,bypassEquipment:true);
 }

 Vector3 SafeCameraPosition(Vector3 pivot,Vector3 target) {
  Vector3 offset=target-pivot;
  float distance=offset.magnitude;
  if(distance<.001f)return pivot;
  Vector3 direction=offset/distance;
  int count=Physics.SphereCastNonAlloc(pivot,.18f,direction,cameraHits,distance,~0,QueryTriggerInteraction.Ignore);
  var hits=count==cameraHits.Length?Physics.SphereCastAll(pivot,.18f,direction,distance,~0,QueryTriggerInteraction.Ignore):cameraHits;
  int limit=count==cameraHits.Length?hits.Length:count;
  for(int i=0;i<limit;i++)
   if(hits[i].collider&&hits[i].collider.GetComponentInParent<OZActor>()!=Player)
    distance=Mathf.Min(distance,Mathf.Max(.02f,hits[i].distance-.08f));
  return pivot+direction*distance;
 }
 void UpdateCamera(bool instant) {
  SetFirstPersonBody(FPS||Driving);
  Vector3 pivot=Player.transform.position+Vector3.up*(cc&&cc.height<1.5f?1.1f:1.65f);
  Quaternion q=Quaternion.Euler(pitch,yaw,0);
  Vector3 offset=Driving?q*new Vector3(0,1.7f,-6.5f):FPS?Vector3.zero:q*new Vector3(LeftShoulder?-.65f:.65f,.18f,Aim?-2.3f:-4.3f);
  Vector3 target=SafeCameraPosition(pivot,pivot+offset);
  Vector3 position=instant||FPS?target:SafeCameraPosition(pivot,Vector3.Lerp(view.transform.position,target,1f-Mathf.Exp(-CameraSmooth*Time.deltaTime)));
  view.transform.SetPositionAndRotation(position,q);
  view.fieldOfView=Mathf.Lerp(view.fieldOfView,Aim?Mathf.Min(45,CameraFov):CameraFov,1f-Mathf.Exp(-10f*Time.deltaTime));
 }
 void SetFirstPersonBody(bool hide) {
  if(bodyHidden==hide||playerBody==null)return;
  bodyHidden=hide;
  for(int i=0;i<playerBody.Length;i++)
   if(playerBody[i])playerBody[i].shadowCastingMode=hide?UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly:bodyShadowModes[i];
 }
 void Shoot() {
  ammo[Weapon]--;
  nextShot=Time.time+rate[Weapon];
  Player.Fire();
  audioSource.PlayOneShot(shotSound,.65f);

  Vector2 spread=Random.insideUnitCircle*(Weapon==4 ? .035f : Aim ? .0025f : .009f);
  Ray cameraRay=view.ViewportPointToRay(new Vector3(.5f+spread.x,.5f+spread.y));
  Vector3 target=NearestHit(cameraRay,reach[Weapon],out var sightHit)
   ? sightHit.point : cameraRay.GetPoint(reach[Weapon]);
  Vector3 origin=Player.transform.position+Vector3.up*(cc.height*.82f);
  Vector3 path=target-origin;
  if (path.sqrMagnitude>.0001f && Vector3.Dot(path,cameraRay.direction)>0) {
   var shotRay=new Ray(origin,path.normalized);
   float distance=Mathf.Min(reach[Weapon],path.magnitude+.04f);
   if (NearestHit(shotRay,distance,out var hit)) {
    target=hit.point;
    var actor=hit.collider.GetComponentInParent<OZActor>();
    if (actor) {
     var body=actor.GetComponent<CharacterController>();
     bool headshot=hit.point.y-actor.transform.position.y>body.height*.81f;
     HitUntil=Time.time+.18f;
     if (actor.Damage(damage[Weapon]*(headshot?1.45f:1f),headshot)) Kills++;
    }
   } else target=shotRay.GetPoint(distance);
   shotEffects.Show(origin,target);
  }
  pitch=Mathf.Clamp(pitch-(Weapon==2?2.8f:Weapon==4?2.2f:Weapon==1 ? .65f:1.1f),-65,65);
 }
 bool NearestHit(Ray ray,float range,out RaycastHit nearest) {
  nearest=default;
  float distance=float.PositiveInfinity;
  foreach(var hit in Physics.RaycastAll(ray,range,~0,QueryTriggerInteraction.Ignore)) {
   if (hit.distance>=distance) continue;
   var actor=hit.collider.GetComponentInParent<OZActor>();
   if (actor==Player || (actor&&actor.Dead)) continue;
   nearest=hit;
   distance=hit.distance;
  }
  return nearest.collider;
 }
 void SpawnLoot() {
  for (int i=0; i<spawns.Length; i++) {
   Vector3 point = spawns[i].position + spawns[i].right * 2;
   if (!Physics.Raycast(point + Vector3.up * 3, Vector3.down, out var floor, 8, ~0, QueryTriggerInteraction.Ignore))
    continue;
   loot.Add(OZLoot.Create((OZLoot.Kind)(i % 4), floor.point,effectsMaterial));
  }
 }
 void UpdateLoot() {
  OZLoot nearest = null;
  float best = 2.4f * 2.4f;
  foreach (var item in loot) {
   float distance = (item.transform.position-Player.transform.position).sqrMagnitude;
   if (distance < best) { best=distance; nearest=item; }
  }
  NearbyLoot = nearest ? nearest.Label : "";
  if ((LootRequested || Input.GetKeyDown(KeyCode.E)) && nearest && Collect(nearest.Item)) {
   loot.Remove(nearest);
   Destroy(nearest.gameObject);
   NearbyLoot = "";
  }
  LootRequested=false;
  if ((HealRequested || Input.GetKeyDown(KeyCode.H)) && Medkits>0 && Player.hp<100) {
   Medkits--;
   Player.Heal();
  }
  HealRequested=false;
 }
 bool Collect(OZLoot.Kind item) {
  switch (item) {
   case OZLoot.Kind.Ammo:
    int ceiling = capacity[Weapon] * 8;
    if (reserve[Weapon] >= ceiling) return false;
    reserve[Weapon] = Mathf.Min(ceiling, reserve[Weapon] + capacity[Weapon] * 2);
    return true;
   case OZLoot.Kind.Medkit:
    if (Medkits >= 3) return false;
    Medkits++;
    return true;
   case OZLoot.Kind.Armor:
    if (Player.armor >= 50) return false;
    Player.RestoreArmor();
    return true;
   case OZLoot.Kind.Helmet:
    if (Player.Helmet >= 40) return false;
    Player.EquipHelmet();
    return true;
   default: return false;
  }
 }
 public bool Supports90Fps => Screen.currentResolution.refreshRateRatio.value >= 89.5;
 public void SetFrameRate(int fps) {
  Application.targetFrameRate = fps >= 90 && Supports90Fps ? 90 : fps >= 60 ? 60 : 30;
  graphicsBudget.ResetWindow();
  PlayerPrefs.SetInt("OZ_FPS", Application.targetFrameRate);
 }
 void Finish(bool win){if(finished)return;if(driving){driving.EndDrive();driving=null;}SetFirstPersonBody(false);Player.Pose(0,false);finished=true;Playing=false;Result=win?"VITÓRIA":"ELIMINADO";PlayerPrefs.SetInt("OZ_XP",XP+Kills*100+(win?300:0));PlayerPrefs.Save();}
 public void SetQuality(int level) {
  level=Mathf.Clamp(level,0,3);
  if (!activeQuality && UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset source) {
   previousQuality=QualitySettings.renderPipeline;
   activeQuality=Instantiate(source);
   QualitySettings.renderPipeline=activeQuality;
  }
  graphicsBudget.Reset(level);
  var profile = graphicsBudget.Settings;
  if (activeQuality) {
   activeQuality.renderScale = graphicsBudget.Scale;
   activeQuality.shadowDistance = profile.ShadowDistance;
   activeQuality.mainLightShadowmapResolution = profile.ShadowPixels;
   activeQuality.msaaSampleCount = profile.Samples;
  }
  if (view) {
   view.farClipPlane = profile.ViewDistance;
   view.allowMSAA = profile.Samples > 1;
   view.allowHDR = level >= 2;
   var data = view.GetUniversalAdditionalCameraData();
   data.renderPostProcessing = level >= 2;
   data.SetRenderer(level >= 2 ? 1 : 0);
  }
  QualitySettings.vSyncCount=0;
  PlayerPrefs.SetInt("OZ_QUALITY",level);
 }
 void OnDestroy() {
  if (!activeQuality) return;
  if (QualitySettings.renderPipeline==activeQuality) QualitySettings.renderPipeline=previousQuality;
  Destroy(activeQuality);
 }
}
