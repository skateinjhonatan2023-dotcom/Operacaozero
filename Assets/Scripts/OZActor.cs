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
 bool alignFeet,groundedPose;
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
   field?field.jog:null,field?field.aimWalk:null,field?field.back:null,field?field.left:null,field?field.right:null};
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
  for(int i=0;i<clips.Length;i++)Connect(mix,i,clips[i]);
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
 public void Pose(float speed,bool aiming,bool low=false,bool inAir=false,bool reloading=false,Vector2 direction=default)
 {
  if(!graph.IsValid())return;
  groundedPose=!inAir && !Dead;
  bool shooting=Time.time<fireUntil;
  if(field){field.airborne=inAir || Dead;field.reloading=reloading;}
  bool overlay=upper.IsValid();
  upperWeight=overlay && !Dead && Time.time>=hitUntil && (aiming || shooting || reloading) ? 1 : 0;
  int action=reloading ? 2 : shooting ? 1 : 0;
  if(overlay && action!=upperState){upperState=action;upper.GetInput(action).SetTime(0);}
  int next=Dead ? 5 : Time.time<hitUntil && hit ? 10 : !overlay && reloading && reload ? 9 :
   !overlay && shooting ? 4 : inAir && airborne ? 8 : low && speed>.12f && crouchWalk ? 7 :
   low && crouch ? 6 : speed<.12f ? (aiming && !overlay ? 3 : 0) :
   field && direction.y<-.65f ? 13 : field && Mathf.Abs(direction.x)>.70f ? (direction.x<0 ? 14 : 15) :
   field && aiming ? 12 : field ? 1 : speed>3.1f ? 2 : 1;
  if(!clips[next])next=0;
  System.Array.Clear(target,0,target.Length);
  target[next]=1;
  if(field && next==1)
  {
   float jog=Mathf.InverseLerp(1.3f,2.45f,speed), sprint=Mathf.InverseLerp(2.6f,3.8f,speed);
   target[1]=1-jog;target[11]=jog*(1-sprint);target[2]=sprint;
  }
  if(state!=next && next!=1 && next!=2 && next!=7 && next<11)mix.GetInput(next).SetTime(0);
  state=next;
  if(field)
  {
   for(int i=0;i<clips.Length;i++)
   {
    if(target[i]<=0)continue;
    float stride=i switch {1=>1.29f,2=>3.15f,7=>1.02f,11=>2.28f,12=>1.29f,13=>.8f,14=>.79f,15=>.73f,_=>0};
    if(stride>0)mix.GetInput(i).SetSpeed(Mathf.Clamp(speed/stride,.28f,2.1f));
   }
  }
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
   if(!clips[i])continue;
   mix.SetInputWeight(i,Mathf.Lerp(mix.GetInputWeight(i),target[i],blend));
   var motion=mix.GetInput(i);
   if(motion.GetTime()<clips[i].length)continue;
   bool once=i==4 || i==5 || i==9 || i==10;
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
