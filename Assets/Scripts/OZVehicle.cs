using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public sealed class OZVehicle:MonoBehaviour {
 public string Title="Veículo";public float MaxSpeed=15;float speed,vertical;
 CharacterController body;
 public Vector3 Seat=>transform.position+Vector3.up*.85f;
 void Awake(){body=GetComponent<CharacterController>();}
 public void BeginDrive(){speed=0;vertical=0;}
 public void EndDrive(){speed=0;vertical=0;}
 public void Tick(Vector2 input,float dt){
  float target=input.y>=0?input.y*MaxSpeed:input.y*MaxSpeed*.4f;
  speed=Mathf.MoveTowards(speed,target,dt*(Mathf.Abs(target)<.1f?12:7));
  transform.Rotate(0,input.x*95f*dt*Mathf.Clamp01(Mathf.Abs(speed)/3f)*(speed<0?-1:1),0);
  vertical=body.isGrounded?-2:vertical-18*dt;
  var collision=body.Move((transform.forward*speed+Vector3.up*vertical)*dt);
  if((collision&CollisionFlags.Sides)!=0)speed=0;
 }
}