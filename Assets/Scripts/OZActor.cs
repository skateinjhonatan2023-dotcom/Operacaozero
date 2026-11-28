using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
public sealed class OZActor : MonoBehaviour {
 public AnimationClip idle,walk,run,aim,fire,death,crouch,crouchWalk,airborne,reload,hit;
 public float hp=100,armor=50; public bool Dead=>hp<=0;
 public float Helmet { get; private set; }
 public void EquipHelmet() => Helmet = 40;
 public void RestoreArmor() => armor = 50;
 public void Heal() => hp = Mathf.Min(100, hp + 45);
 public Vector3 FloorNormal { get; private set; }=Vector3.up;
 int floorFrame=-10;
 public bool HasFloor => Time.frameCount-floorFrame<=1;
 void OnControllerColliderHit(ControllerColliderHit hit)
 {
  if(hit.normal.y>=.70f){FloorNormal=hit.normal;floorFrame=Time.frameCount;}
 }
 Transform visualRoot,leftFoot,rightFoot;
 Vector3 baseVisual;
 bool alignFeet,groundedPose,wasAir;
 float landingUntil,airTime;
 AnimatorControllerPlayable groundTree;
 static readonly int Lateral=Animator.StringToHash("Lateral"),Forward=Animator.StringToHash("Forward");
 float visualOffset;
 OZFieldRig field;
 PlayableGraph graph;
 AnimationMixerPlayable mix,upper;
 AnimationLayerMixerPlayable layers;
 AnimationClip[] clips;
 float[] target;
 int state,upperState;
 float fireUntil,hitUntil,upperWeight;

 public void Setup()
 {
  var animator=GetComponentInChildren<Animator>();
  animator.applyRootMotion=false;
  field=GetComponent<OZFieldRig>();
  if(!field && animator.isHuman && animator.transform!=transform)
  {
   visualRoot=animator.transform;baseVisual=visualRoot.localPosition;
   leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
   alignFeet=leftFoot && rightFoot;
  }
  clips=new[]{idle,walk,run,aim,fire,death,crouch,crouchWalk,airborne,reload,hit,
   field?field.jog:null,field?field.aimWalk:null,field?field.back:null,field?field.left:null,field?field.right:null,field?field.takeoff:null,field?field.land:null};
  target=new float[clips.Length];target[0]=1;
  graph=PlayableGraph.Create(name);
  mix=AnimationMixerPlayable.Create(graph,clips.Length);
  void Connect(AnimationMixerPlayable mixer,int index,AnimationClip clip)
  {
   if(!clip)return;
   var motion=AnimationClipPlayable.Create(graph,clip);
   motion.SetApplyFootIK(animator.isHuman && (clip==walk || clip==run || clip==crouchWalk));
   graph.Connect(motion,0,mixer,index);
  }
  for(int i=0;i<clips.Length;i++)
  {
   if(field && field.groundedController && (i==0 || i==1 || i==2 || i==11 || i>=13 && i<=15))continue;
   Connect(mix,i,clips[i]);
  }
  if(field && field.groundedController)
  {
   groundTree=AnimatorControllerPlayable.Create(graph,field.groundedController);
   graph.Connect(groundTree,0,mix,0);
  }
  mix.SetInputWeight(0,1);
  var output=AnimationPlayableOutput.Create(graph,"Corpo",animator);
  if(field && field.upperMask)
  {
   layers=AnimationLayerMixerPlayable.Create(graph,2);
   upper=AnimationMixerPlayable.Create(graph,3);
   Connect(upper,0,aim);Connect(upper,1,fire);Connect(upper,2,reload);
   graph.Connect(mix,0,layers,0);graph.Connect(upper,0,layers,1);
   layers.SetInputWeight(0,1);layers.SetLayerMaskFromAvatarMask(1,field.upperMask);
   output.SetSourcePlayable(layers);
  }
  else output.SetSourcePlayable(mix);
  graph.Play();
 }
 public void Pose(float speed,bool aiming,bool low=false,bool inAir=false,bool reloading=false,Vector2 direction=default,float verticalSpeed=0)
 {
  if(!graph.IsValid())return;
  groundedPose=!inAir && !Dead;
  bool shooting=Time.time<fireUntil;
  if(field){field.airborne=inAir || Dead;field.reloading=reloading;}
  bool overlay=upper.IsValid();
  upperWeight=overlay && !Dead && Time.time>=hitUntil && (aiming || shooting || reloading) ? 1 : 0;
  int action=reloading ? 2 : shooting ? 1 : 0;
  if(overlay && action!=upperState){upperState=action;upper.GetInput(action).SetTime(0);}
  if(inAir)airTime+=Time.deltaTime;
  else if(wasAir)
  {
   if(airTime>.13f && field && field.land)landingUntil=Time.time+Mathf.Min(.22f,field.land.length);
   airTime=0;
  }
  wasAir=inAir;
  int next=Dead ? 5 : Time.time<hitUntil && hit ? 10 : !overlay && reloading && reload ? 9 :
   !overlay && shooting ? 4 : inAir && field && verticalSpeed>.45f && field.takeoff ? 16 :
   inAir && airborne ? 8 : field && Time.time<landingUntil ? 17 :
   low && speed>.12f && crouchWalk ? 7 : low && crouch ? 6 :
   groundTree.IsValid() ? 0 : speed<.12f ? (aiming && !overlay ? 3 : 0) : speed>3.1f ? 2 : 1;
  if(!clips[next])next=0;
  System.Array.Clear(target,0,target.Length);
  target[next]=1;
  if(groundTree.IsValid())
  {
   Vector2 travel=direction.sqrMagnitude>.01f ? direction.normalized*speed : Vector2.up*speed;
   groundTree.SetFloat(Lateral,travel.x);
   groundTree.SetFloat(Forward,travel.y);
   groundTree.SetSpeed(Mathf.Max(1,travel.y/3.15f));
   if(next==16 || next==17)mix.GetInput(next).SetSpeed(Mathf.Clamp(clips[next].length/.22f,.6f,3f));
   if(next==7)mix.GetInput(7).SetSpeed(Mathf.Clamp(speed/1.02f,.35f,1.65f));
  }
  if(state!=next && next!=0 && mix.GetInput(next).IsValid())mix.GetInput(next).SetTime(0);
  state=next;
 }
 public void Fire() {
  fireUntil=Time.time+.22f;
  if(upper.IsValid())upper.GetInput(1).SetTime(0);
  if(state==4 && graph.IsValid() && mix.GetInput(4).IsValid()) mix.GetInput(4).SetTime(0);
 }
 public bool Damage(float amount, bool headshot = false, bool bypassEquipment = false) {
  if (Dead || amount <= 0) return false;
  float absorbed = 0;
  if (!bypassEquipment) {
   if (headshot) {
    absorbed = Mathf.Min(Helmet, amount * .55f);
    Helmet -= absorbed;
   } else {
    absorbed = Mathf.Min(armor, amount * .5f);
    armor -= absorbed;
   }
  }
  hp = Mathf.Max(0, hp - amount + absorbed);
  if (!bypassEquipment && !Dead) hitUntil = Time.time + .23f;
  if (Dead) {
   Pose(0, false);
   if (TryGetComponent<CharacterController>(out var controller)) controller.enabled = false;
  }
  return Dead;
 }
 void Update()
 {
  if(!graph.IsValid())return;
  float blend=1-Mathf.Exp(-Time.deltaTime*19);
  for(int i=0;i<clips.Length;i++)
  {
   if(!clips[i] || !mix.GetInput(i).IsValid())continue;
   mix.SetInputWeight(i,Mathf.Lerp(mix.GetInputWeight(i),target[i],blend));
   var motion=mix.GetInput(i);
   if((i==0 && groundTree.IsValid()) || motion.GetTime()<clips[i].length)continue;
   bool once=i==4 || i==5 || i==9 || i==10 || i==16 || i==17;
   motion.SetTime(once ? clips[i].length-.001 : motion.GetTime()%clips[i].length);
  }
  if(upper.IsValid())
  {
   layers.SetInputWeight(1,Mathf.Lerp(layers.GetInputWeight(1),upperWeight,blend));
   for(int i=0;i<3;i++)upper.SetInputWeight(i,Mathf.Lerp(upper.GetInputWeight(i),i==upperState?1:0,blend));
   var pose=upper.GetInput(0);
   if(pose.GetTime()>=aim.length)pose.SetTime(pose.GetTime()%aim.length);
  }
 }
 void LateUpdate()
 {
  if(!alignFeet)return;
  float sole=Mathf.Min(leftFoot.position.y,rightFoot.position.y);
  float offset=groundedPose ? Mathf.Clamp(visualOffset+transform.position.y+.065f-sole,-.18f,.18f) : 0;
  visualOffset=Mathf.Lerp(visualOffset,offset,1-Mathf.Exp(-18*Time.deltaTime));
  visualRoot.localPosition=baseVisual+Vector3.up*visualOffset;
 }
 void OnDestroy(){if(graph.IsValid())graph.Destroy();}
}
