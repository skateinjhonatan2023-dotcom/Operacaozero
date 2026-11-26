using UnityEngine;
public sealed class OZGrenade:MonoBehaviour{
 OZGame game;float end;Material material;
 public void Launch(OZGame owner,Vector3 origin,Vector3 direction,Material template){
  game=owner;material=template;transform.position=origin+direction*.5f;
  var model=GameObject.CreatePrimitive(PrimitiveType.Sphere);model.transform.SetParent(transform,false);model.transform.localScale=Vector3.one*.16f;
  var col=model.GetComponent<SphereCollider>();col.enabled=false;
  if(template)model.GetComponent<Renderer>().sharedMaterial=template;
  var collider=gameObject.AddComponent<SphereCollider>();collider.radius=.08f;
  var rb=gameObject.AddComponent<Rigidbody>();rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.AddForce(direction*13+Vector3.up*5,ForceMode.VelocityChange);
  end=Time.time+2.5f;
 }
 void Update(){if(Time.time<end)return;if(game)game.GrenadeBlast(transform.position);
  var effect=GameObject.CreatePrimitive(PrimitiveType.Sphere);effect.transform.position=transform.position;effect.transform.localScale=Vector3.one*2.5f;Destroy(effect.GetComponent<Collider>());
  if(material){var renderer=effect.GetComponent<Renderer>();renderer.sharedMaterial=material;var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",new Color(1,.42f,.08f));renderer.SetPropertyBlock(block);}Destroy(effect,.16f);Destroy(gameObject);}
}