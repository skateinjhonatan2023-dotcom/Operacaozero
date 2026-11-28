using System;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

internal static class OZFieldChecks
{
    static void Require(bool value,string message)
    {
        if(!value) throw new InvalidOperationException("OZ09: "+message);
    }

    internal static void Validate(OZActor actor)
    {
        var rig=actor.GetComponent<OZFieldRig>();
        var animator=actor.GetComponentInChildren<Animator>(true);
        Require(rig && animator && animator.avatar && animator.avatar.isHuman && animator.avatar.isValid,
            "Avatar de campo precisa ser Humanoid válido na cena salva.");
        Require(!animator.applyRootMotion,"Conflito com a locomoção do CharacterController.");
        var clips=new[]{actor.idle,actor.walk,actor.run,actor.aim,actor.fire,actor.death,actor.crouch,
            actor.crouchWalk,actor.airborne,actor.reload,actor.hit,rig.jog,rig.aimWalk,rig.back,rig.left,rig.right,rig.takeoff,rig.land};
        Require(clips.All(c=>c && c.humanMotion && c.length>0),"Tomada humana ausente ou inválida.");
        var controller=rig.groundedController as AnimatorController;
        Require(controller && controller.layers[0].stateMachine.defaultState.motion is BlendTree tree && tree.children.Length==7,
            "Blend Tree de marcha ausente.");
        foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            foreach(var m in skin.sharedMaterials)
                Require(m && m.GetTexture("_BaseMap") && m.GetTexture("_BumpMap") && m.GetTexture("_MetallicGlossMap"),
                    "Material PBR incompleto: "+skin.name);
        Require(OZGroundMotion.Joystick(new Vector2(3,0),100)==Vector2.zero &&
            Mathf.Abs(OZGroundMotion.Joystick(Vector2.one*150,100).magnitude-1)<.001f &&
            OZGroundMotion.Joystick(Vector2.up*35,100).y<OZGroundMotion.Joystick(Vector2.up*65,100).y,
            "Resposta radial do joystick inválida.");
        PhysicsFixture();
        Debug.Log("OZ09_PASS: Humanoid, 18 movimentos, Blend Tree, mapas PBR, joystick e colisão/salto a 30 e 60 Hz.");
    }

    static void PhysicsFixture()
    {
        var fixture=new GameObject("Teste de locomoção");
        try
        {
            var origin=new Vector3(1500,150,1500);
            BoxCollider Box(string name,Vector3 offset,Vector3 size)
            {
                var box=new GameObject(name).AddComponent<BoxCollider>();
                box.transform.SetParent(fixture.transform,false);
                box.transform.position=origin+offset;box.size=size;
                return box;
            }
            Box("Piso",Vector3.down*.5f,new Vector3(12,1,12));
            Box("Parede",new Vector3(0,1,2),new Vector3(8,2,.2f));
            foreach(int fps in new[]{30,60})
            {
                var body=new GameObject("Cápsula de teste");
                body.transform.SetParent(fixture.transform,false);
                body.transform.position=origin+Vector3.up*.06f;
                var cc=body.AddComponent<CharacterController>();
                cc.height=1.8f;cc.center=Vector3.up*.9f;cc.radius=.33f;cc.minMoveDistance=0;cc.skinWidth=.035f;
                Physics.SyncTransforms();
                var motor=new OZGroundMotion();
                float dt=1f/fps,vertical=0,apex=0;
                Vector3 velocity=Vector3.zero;
                bool landed=false,wall=false;
                for(int i=0;i<fps*3;i++)
                {
                    bool jump=i==fps/2;
                    vertical=motor.Vertical(vertical,cc.isGrounded,false,jump,i*dt,dt);
                    bool move=i>=fps;
                    velocity=OZGroundMotion.Accelerate(velocity,move?Vector3.forward*2.65f:Vector3.zero,cc.isGrounded,dt);
                    var flags=cc.Move((velocity+Vector3.up*vertical)*dt);
                    apex=Mathf.Max(apex,body.transform.position.y-origin.y);
                    landed |= i>fps && (flags&CollisionFlags.Below)!=0;
                    wall |= (flags&CollisionFlags.Sides)!=0;
                }
                Require(apex>.48f && apex<.86f && landed,"Impulsão/gravidade/aterrissagem a "+fps+" Hz: "+apex);
                Require(wall && body.transform.position.z-origin.z<1.76f && motor.Surface(cc,out var ground) && ground.normal.y>.99f,
                    "Colisão frontal ou sondagem de piso a "+fps+" Hz.");
                float stop=0;
                while(velocity.sqrMagnitude>.0001f && stop<1f)
                {velocity=OZGroundMotion.Accelerate(velocity,Vector3.zero,true,dt);stop+=dt;}
                Require(stop<.20f,"Frenagem do personagem a "+fps+" Hz.");
                var slope=Box("Rampa",new Vector3(3,0,0),new Vector3(2,.2f,3));
                slope.transform.rotation=Quaternion.Euler(18,0,0);
                cc.enabled=false;body.transform.position=origin+new Vector3(3,.13f,0);cc.enabled=true;
                Physics.SyncTransforms();
                Require(motor.Surface(cc,out var rampHit) && rampHit.collider==slope && rampHit.normal.y>.9f &&
                    rampHit.normal.y<.98f,"Normal da rampa a "+fps+" Hz.");
                UnityEngine.Object.DestroyImmediate(slope.gameObject);
                UnityEngine.Object.DestroyImmediate(body);
            }
        }
        finally {UnityEngine.Object.DestroyImmediate(fixture);}
    }
}
