import * as T from 'three';

const up=new T.Vector3(0,1,0);
const zero=new T.Vector3();
const right=new T.Vector3(), finger=new T.Vector3(), target=new T.Vector3();
const localForward=new T.Vector3();
const basis=new T.Matrix4();
const shoulder=new T.Vector3(), elbow=new T.Vector3(), palm=new T.Vector3();
const axis=new T.Vector3(), pole=new T.Vector3(), bend=new T.Vector3();
const from=new T.Vector3(), to=new T.Vector3();
const rotation=new T.Quaternion(), world=new T.Quaternion(), parent=new T.Quaternion();

export function gripBones(body) {
    const find=name=>{
        let match=null;
        body.traverse(bone=>{if(bone.isBone&&bone.name.endsWith(name))match=bone;});
        return match;
    };
    return {
        hand:find('RightHand'), finger:find('RightHandMiddle1'),
        upper:find('LeftArm'), forearm:find('LeftForeArm'), support:find('LeftHand'),
    };
}

function turn(bone,joint,point,destination) {
    from.copy(joint).sub(point).normalize();
    to.copy(destination).sub(point).normalize();
    if(from.lengthSq()<.1||to.lengthSq()<.1)return;
    rotation.setFromUnitVectors(from,to);
    bone.getWorldQuaternion(world).premultiply(rotation);
    bone.parent.getWorldQuaternion(parent).invert();
    bone.quaternion.copy(parent.multiply(world));
    bone.updateWorldMatrix(false,true);
}

export function restoreGrip(actor) {
    if(!actor.gripApplied)return;
    for(const [bone,base] of actor.gripPose)bone.quaternion.copy(base);
    actor.gripApplied=false;
}

function alignSupport(actor,destination,weight) {
    const {upper,forearm,support}=actor.bones;
    if(!upper||!forearm||!support)return;
    upper.getWorldPosition(shoulder);
    forearm.getWorldPosition(elbow);
    support.getWorldPosition(palm);
    const upperLength=shoulder.distanceTo(elbow), lowerLength=elbow.distanceTo(palm);
    axis.copy(destination).sub(shoulder);
    if(upperLength<.001||lowerLength<.001||axis.lengthSq()<.001)return;
    const distance=axis.length();
    if(distance>(upperLength+lowerLength)*1.025||distance<.04)return;
    axis.normalize();
    pole.copy(elbow).sub(shoulder);
    pole.addScaledVector(axis,-pole.dot(axis));
    if(pole.lengthSq()<.000001)return;
    const reach=T.MathUtils.clamp(distance,Math.abs(upperLength-lowerLength)+.002,upperLength+lowerLength-.006);
    const along=(upperLength*upperLength-lowerLength*lowerLength+reach*reach)/(2*reach);
    bend.copy(shoulder).addScaledVector(axis,along)
        .addScaledVector(pole.normalize(),Math.sqrt(Math.max(0,upperLength*upperLength-along*along)));
    actor.gripPose??=[[upper,new T.Quaternion()],[forearm,new T.Quaternion()]];
    actor.gripPose[0][1].copy(upper.quaternion);
    actor.gripPose[1][1].copy(forearm.quaternion);
    actor.gripApplied=true;
    turn(upper,elbow,shoulder,bend);
    upper.quaternion.slerp(actor.gripPose[0][1],1-weight);
    upper.updateWorldMatrix(false,true);
    forearm.getWorldPosition(elbow);
    support.getWorldPosition(palm);
    turn(forearm,palm,elbow,destination);
    forearm.quaternion.slerp(actor.gripPose[1][1],1-weight);
    forearm.updateWorldMatrix(false,true);
}

export function updateGrip(actor,pitch,recoil,compact,aiming=false) {
    const {root,gun,bones}=actor;
    if(!gun.visible||!bones.hand)return;
    root.updateMatrixWorld(true);
    bones.hand.getWorldPosition(right);
    if(bones.finger){
        bones.finger.getWorldPosition(finger);
        right.lerp(finger,.5);
    }
    gun.position.copy(root.worldToLocal(right));
    const name=actor.clip?.name??'';
    const planted=/^(ready_idle|ready_walk|aim_|shoot|crouch_idle)/.test(name);
    const weaponPitch=T.MathUtils.clamp(pitch,-.40,.40)*(aiming?.85:.44)+recoil;
    localForward.set(0,Math.sin(weaponPitch),Math.cos(weaponPitch));
    if(bones.support&&!compact&&planted){
        bones.support.getWorldPosition(target);
        root.worldToLocal(target);
        target.sub(gun.position);
        if(target.z>.075){
            localForward.x=T.MathUtils.clamp(target.x/target.z,-.38,.38)*(aiming?.2:.62);
            if(!aiming)localForward.y=T.MathUtils.clamp(localForward.y+target.y*.85,-.4,.45);
        }
    }else if(!planted)localForward.y-=name==='reload'?.11:.26;
    gun.quaternion.setFromRotationMatrix(basis.lookAt(zero,localForward,up));
    if(!planted||compact)return;
    target.set(0,.036,-.245*actor.weapon.stock);
    gun.localToWorld(target);
    alignSupport(actor,target,aiming?.90:.68);
}
