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
 readonly string[] names={"Rifle","SMG","Sniper","Pistola"};readonly int[] capacity={30,24,5,12},damage={34,24,95,30};readonly float[] rate={.13f,.08f,.85f,.24f},reach={100,65,220,45}; readonly int[] ammo={30,24,5,12},reserve={120,144,25,72};
 float yaw,pitch=8,vy,nextShot,reloadUntil,slideUntil,startTime; int reloadWeapon; CharacterController cc; AudioSource audioSource; bool finished;
 void Start(){if(!view)view=Camera.main;SetFrameRate(PlayerPrefs.GetInt("OZ_FPS",30));SetQuality(PlayerPrefs.GetInt("OZ_QUALITY",0));Sensitivity=PlayerPrefs.GetFloat("OZ_SENS",1);AdsSensitivity=Mathf.Clamp(PlayerPrefs.GetFloat("OZ_ADS_SENS",.65f),.2f,3f);audioSource=gameObject.AddComponent<AudioSource>();shotEffects=gameObject.AddComponent<OZShotEffects>();shotEffects.Initialize(effectsMaterial);if(!soldier||!rifle||!shotSound||!view||spawns==null||spawns.Length<8){Result="Conteúdo gráfico obrigatório ausente";enabled=false;return;} Player=Spawn(spawns[0].position);cc=Player.GetComponent<CharacterController>();playerBody=Player.GetComponentsInChildren<SkinnedMeshRenderer>(true);bodyShadowModes=new UnityEngine.Rendering.ShadowCastingMode[playerBody.Length];for(int i=0;i<playerBody.Length;i++)bodyShadowModes[i]=playerBody[i].shadowCastingMode;Player.Pose(0,false);UpdateCamera(true);}
 OZActor Spawn(Vector3 pos){var a=Instantiate(soldier,pos,Quaternion.identity);a.gameObject.SetActive(true);a.Setup();var ctrl=a.GetComponent<CharacterController>()??a.gameObject.AddComponent<CharacterController>();ctrl.enabled=true;ctrl.height=1.8f;ctrl.center=Vector3.up*.9f;ctrl.radius=.33f;return a;}
 public void Begin(Mode m){if(Playing||finished||!Player)return;mode=m;Playing=true;startTime=Time.time;for(int i=1;i<Mathf.Min(spawns.Length,m==Mode.Combate?5:13);i++){var a=Spawn(spawns[i].position);a.gameObject.AddComponent<OZBot>().Init(this,a);bots.Add(a);}
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
  MovePlayer(dt);
  if (Player.transform.position.y < -30) Player.Damage(100,bypassEquipment:true);
  if (Player.Dead) {
   Finish(false);
   return;
  }
  UpdateCamera(false);
  UpdateLoot();
  UpdateWeapons();
  if (mode==Mode.BattleRoyale) UpdateZone(dt);
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
  yaw=Mathf.Repeat(yaw+look.x*sensitivity*.18f+180f,360f)-180f;
  pitch=Mathf.Clamp(pitch+look.y*sensitivity*.18f,-65,65);
  Quaternion heading=Quaternion.Euler(0,yaw,0);
  Vector3 desired=heading*new Vector3(input.x,0,input.y);
  bool sprint=Sprint||Input.GetKey(KeyCode.LeftShift);
  bool crouch=Crouch||Input.GetKey(KeyCode.LeftControl);
  if (!Deploying && cc.isGrounded && Time.time>=slideUntil && (Slide||Input.GetKeyDown(KeyCode.C))) {
   slideDirection=desired.sqrMagnitude>.01f?desired.normalized:heading*Vector3.forward;
   slideUntil=Time.time+.55f;
  }
  Slide=false;
  bool sliding=Time.time<slideUntil;
  bool low=!Deploying && (crouch||sliding || (cc.height<1.5f && !CanStand()));
  cc.height=low ? 1.2f : 1.8f;
  cc.center=Vector3.up*cc.height*.5f;
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
  if (Deploying && (collision&CollisionFlags.Below)!=0) {
   Deploying=false;
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
  if (!Deploying && (Trigger||shootDesktop) && reloadUntil==0 && Time.time>=nextShot && Ammo>0)
   Shoot();
 }

 void UpdateZone(float dt) {
  float elapsed=Time.time-startTime;
  ZoneSeconds=Mathf.Max(0,25-elapsed);
  Zone=Mathf.Max(12,140-Mathf.Max(0,elapsed-25)*.85f);
  if (Time.time>=nextZoneDraw) {
   zoneVisual.SetRadius(Zone);
   nextZoneDraw=Time.time+.75f;
  }
  ApplyZoneDamage(Player,dt);
  foreach (var bot in bots)
   if (bot && !bot.Dead) ApplyZoneDamage(bot,dt);
 }

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
  SetFirstPersonBody(FPS);
  Vector3 pivot=Player.transform.position+Vector3.up*(cc&&cc.height<1.5f?1.1f:1.65f);
  Quaternion q=Quaternion.Euler(pitch,yaw,0);
  Vector3 offset=FPS?Vector3.zero:q*new Vector3(LeftShoulder?-.65f:.65f,.18f,Aim?-2.3f:-4.3f);
  Vector3 target=SafeCameraPosition(pivot,pivot+offset);
  Vector3 position=instant||FPS?target:SafeCameraPosition(pivot,Vector3.Lerp(view.transform.position,target,1f-Mathf.Exp(-18f*Time.deltaTime)));
  view.transform.SetPositionAndRotation(position,q);
  view.fieldOfView=Mathf.Lerp(view.fieldOfView,Aim?45:65,1f-Mathf.Exp(-10f*Time.deltaTime));
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

  Vector2 spread=Random.insideUnitCircle*(Aim ? .0025f : .009f);
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
     if (actor.Damage(damage[Weapon]*(headshot?1.45f:1f),headshot)) Kills++;
    }
   } else target=shotRay.GetPoint(distance);
   shotEffects.Show(origin,target);
  }
  pitch=Mathf.Clamp(pitch-1.1f,-65,65);
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
 void Finish(bool win){if(finished)return;SetFirstPersonBody(false);Player.Pose(0,false);finished=true;Playing=false;Result=win?"VITÓRIA":"ELIMINADO";PlayerPrefs.SetInt("OZ_XP",XP+Kills*100+(win?300:0));PlayerPrefs.Save();}
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
