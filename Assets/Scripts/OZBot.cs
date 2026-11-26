using UnityEngine;
public sealed class OZBot:MonoBehaviour {
 OZGame game; OZActor actor; CharacterController body;
 float next,vertical,decision,detourUntil,reloadUntil,lastHp,moveSpeed;
 int rounds=12; bool visible;
 Vector3 direction,detour,patrol;
 readonly RaycastHit[] hits=new RaycastHit[24];
 public void Init(OZGame g,OZActor a){game=g;actor=a;body=GetComponent<CharacterController>();next=Time.time+Random.Range(1f,3f);lastHp=a.hp;patrol=transform.position;}
 bool CanSee(Vector3 eye,Vector3 target){
  Vector3 d=target-eye;int n=Physics.RaycastNonAlloc(eye,d.normalized,hits,d.magnitude+.3f,~0,QueryTriggerInteraction.Ignore);
  if(n==hits.Length)return false;
  float best=float.PositiveInfinity;OZActor nearest=null;
  for(int i=0;i<n;i++){var h=hits[i];var a=h.collider.GetComponentInParent<OZActor>();if(a==actor||h.distance>=best)continue;best=h.distance;nearest=a;}
  return nearest==game.Player;
 }
 void Update(){
  if(!actor||!game||!game.Playing||actor.Dead||!game.Player||game.Player.Dead||game.Deploying){if(actor)actor.Pose(0,false);return;}
  if(game.mode==OZGame.Mode.Treino){actor.Pose(0,false);return;}
  Vector3 delta=game.Player.transform.position-transform.position;delta.y=0;float distance=delta.magnitude;
  Vector3 eye=transform.position+Vector3.up*1.55f;
  if(Time.time>=decision){
   decision=Time.time+Random.Range(.15f,.28f);
   visible=distance<80&&CanSee(eye,game.Player.transform.position+Vector3.up*1.1f);
   bool outside=game.mode==OZGame.Mode.BattleRoyale&&new Vector2(transform.position.x,transform.position.z).magnitude>game.Zone*.92f;
   if(outside){direction=-new Vector3(transform.position.x,0,transform.position.z).normalized;moveSpeed=5;}
   else if(visible&&distance<24){direction=delta.normalized;moveSpeed=distance>15?3:0;
    if(actor.hp<lastHp||Random.value<.12f){detour=Quaternion.Euler(0,Random.value>.5f?90:-90,0)*direction;detourUntil=Time.time+Random.Range(.5f,1.3f);}
   }else if(distance<110||game.Zone<100){direction=delta.normalized;moveSpeed=4;}
   else{if((patrol-transform.position).sqrMagnitude<9||Random.value<.035f){var p=Random.insideUnitCircle*35;patrol=transform.position+new Vector3(p.x,0,p.y);}var pdir=patrol-transform.position;pdir.y=0;direction=pdir.normalized;moveSpeed=3;}
   if(moveSpeed>0&&Physics.SphereCast(eye,.28f,direction,out var h,2.2f,~0,QueryTriggerInteraction.Ignore)&&h.collider.GetComponentInParent<OZActor>()!=actor&&Time.time>=detourUntil){
    detour=Quaternion.Euler(0,Random.value>.5f?80:-80,0)*direction;detourUntil=Time.time+1.2f;
   }
   lastHp=actor.hp;
  }
  Vector3 dir=Time.time<detourUntil?detour:direction;float speed=Time.time<detourUntil?3.4f:moveSpeed;
  if(dir.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(dir),Time.deltaTime*8);
  if(OZGame.RecoverGround(body))vertical=0;vertical=body.isGrounded?-2:vertical-18*Time.deltaTime;body.Move((dir*speed+Vector3.up*vertical)*Time.deltaTime);if(OZGame.RecoverGround(body))vertical=0;
  if(reloadUntil>0&&Time.time>=reloadUntil){reloadUntil=0;rounds=12;}
  actor.Pose(speed,visible&&speed==0,false,!body.isGrounded,reloadUntil>0);
  if(visible&&distance<36&&Time.time>=next&&reloadUntil==0&&CanSee(eye,game.Player.transform.position+Vector3.up*1.1f)){
   next=Time.time+Random.Range(.65f,1.5f);actor.Fire();rounds--;float chance=Mathf.Clamp01((.34f+.15f*game.BotDifficulty)*(1-distance/75f));
   if(Random.value<chance)game.BotHit(7+game.BotDifficulty*2,transform.position);
   if(rounds<=0)reloadUntil=Time.time+1.8f;
  }
 }
}