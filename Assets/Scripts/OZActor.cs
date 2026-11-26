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
 PlayableGraph graph; AnimationMixerPlayable mix; AnimationClip[] clips; int state; float fireUntil,hitUntil;
 public void Setup(){var a=GetComponentInChildren<Animator>(); if(!a) a=GetComponentInChildren<SkinnedMeshRenderer>().gameObject.AddComponent<Animator>(); a.applyRootMotion=false; clips=new[]{idle,walk,run,aim,fire,death,crouch,crouchWalk,airborne,reload,hit}; graph=PlayableGraph.Create(name); mix=AnimationMixerPlayable.Create(graph,clips.Length); for(int i=0;i<clips.Length;i++){if(!clips[i])continue;var p=AnimationClipPlayable.Create(graph,clips[i]);p.SetApplyFootIK(false);graph.Connect(p,0,mix,i);mix.SetInputWeight(i,i==0?1:0);} AnimationPlayableOutput.Create(graph,"Body",a).SetSourcePlayable(mix);graph.Play();}
 public void Pose(float speed,bool aiming,bool low=false,bool inAir=false,bool reloading=false) {
  if (!graph.IsValid() || !mix.IsValid() || clips==null) return;
  int next = Dead ? 5
   : Time.time < hitUntil && hit ? 10
   : Time.time < fireUntil ? 4
   : reloading && reload ? 9
   : inAir && airborne ? 8
   : low && speed > .1f && crouchWalk ? 7
   : low && crouch ? 6
   : aiming ? 3
   : speed > 4.5f ? 2
   : speed > .1f ? 1 : 0;
  if (!clips[next] || clips[next].length <= 0 || !mix.GetInput(next).IsValid()) next=0;
  if (next==state || !clips[next] || !mix.GetInput(next).IsValid()) return;
  state=next;
  mix.GetInput(state).SetTime(0);
 }
 public void Fire() {
  fireUntil=Time.time+.22f;
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
 void Update(){if(!graph.IsValid())return;for(int i=0;i<clips.Length;i++){if(!clips[i]||clips[i].length<=0||!mix.GetInput(i).IsValid())continue;mix.SetInputWeight(i,Mathf.MoveTowards(mix.GetInputWeight(i),i==state?1:0,Time.deltaTime*12));var p=mix.GetInput(i);bool loop=i<4||(i>=6&&i<=8);if(loop&&p.GetTime()>=clips[i].length)p.SetTime(p.GetTime()%Mathf.Max(.01f,clips[i].length));if(!loop&&p.GetTime()>=clips[i].length)p.SetTime(clips[i].length-.001f);}}
 void OnDestroy(){if(graph.IsValid())graph.Destroy();}
}
