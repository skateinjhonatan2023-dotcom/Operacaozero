import * as T from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';

function contactShadow() {
    const size=32,pixels=new Uint8Array(size*size*4);
    for(let y=0;y<size;y++)for(let x=0;x<size;x++){
        const distance=Math.hypot((x+.5-size/2)/(size/2),(y+.5-size/2)/(size/2));
        pixels[(y*size+x)*4+3]=Math.round(158*Math.max(0,1-distance)**1.6);
    }
    const map=new T.DataTexture(pixels,size,size);
    map.needsUpdate=true;map.magFilter=T.LinearFilter;
    return map;
}

const contactMap=contactShadow();
export function makeGroundShadow(width,length){
    const shadow=new T.Mesh(new T.PlaneGeometry(width,length),new T.MeshBasicMaterial({
        map:contactMap,transparent:true,depthWrite:false,
    }));
    shadow.rotation.x=-Math.PI/2;
    return shadow;
}

export function makeStudio(concrete,asphalt) {
    const scene=new T.Scene();
    scene.background=new T.Color(0x182630);
    scene.fog=new T.Fog(0x253b46,9,31);
    const standard=(color,roughness=.79,metalness=.12,map=null)=>new T.MeshStandardMaterial({color,roughness,metalness,map});
    const glow=color=>new T.MeshBasicMaterial({color});
    const deckMap=asphalt.clone();deckMap.repeat.set(5,4);deckMap.needsUpdate=true;
    const wallMap=concrete.clone();wallMap.repeat.set(3,2);wallMap.needsUpdate=true;
    const wall=standard(0x66747b,.92,.04,wallMap);
    const deck=standard(0x4c5e64,.70,.18,deckMap);
    const panel=standard(0x283b47,.63,.30);
    const rail=standard(0x708995,.55,.46);
    const rib=standard(0x46616b,.80,.25);
    const crate=standard(0x566f6f,.89);
    const stripe=standard(0xb99a6a,.8);
    const amber=glow(0xedb379);
    const cold=glow(0x9ac6d4);
    const batches=new Map(),transform=new T.Object3D();
    const unit=new T.BoxGeometry(1,1,1);
    function box(mat,x,y,z,w,h,d,rz=0) {
        if(!batches.has(mat))batches.set(mat,[]);
        transform.position.set(x,y,z);transform.rotation.z=rz;transform.scale.set(w,h,d);
        transform.updateMatrix();
        batches.get(mat).push(unit.clone().applyMatrix4(transform.matrix));
    }
    box(wall,0,2.45,-6.3,19,5.2,.26);
    box(panel,0,5.1,-2.3,16,.36,10.8);
    box(deck,0,-.20,0,22,.23,18);
    box(rail,0,.011,0,5.1,.11,3.3);
    box(panel,0,.077,0,4.95,.04,3.13);
    // The rear hangar door and the amber ramps frame the operator.
    box(panel,0,2.18,-6.06,5.6,4.4,.30);
    for(let i=0;i<9;i++){
        const y=.47+i*.43;
        box(rib,0,y,-5.88,5.4,.084,.07);
    }
    box(panel,0,4.54,-5.82,6.09,.42,.22);
    box(cold,0,4.33,-5.69,5.8,.055,.08);
    for(const side of [-1,1]){
        const x=side*3.02;
        box(rail,x,2.28,-5.77,.29,4.56,.35);
        box(amber,x+side*.31,1.72,-5.68,.065,1.25,.085);
        box(rail,side*5.65,2.58,-3.48,.36,5.2,.44);
        box(panel,side*5.66,3.82,-3.48,.30,1.7,.42,-side*.53);
        box(amber,side*1.68,.105,1.31,.95,.025,.14);
        box(stripe,side*2.42,.10,.37,.070,.018,2.31);
        box(cold,side*3.84,4.83,-1.36,.04,.065,6.7);
        for(let i=0;i<4;i++){
            const offset=1.1*i;
            box(panel,side*7.2,1.6,-5.7+offset,1.1,3.4,.18);
            box(rail,side*7.18,1.4,-5.61+offset,.86,.095,.16);
        }
        for(let i=0;i<3;i++){
            const cx=side*(3.89+i*.82),cz=-2.65-i*.78;
            box(crate,cx,.46,cz,.71,.9,1.13);
            for(const end of [-1,1])box(panel,cx+end*.25,.47,cz,.09,.97,1.2);
        }
    }
    for(const [mat,geometries] of batches){
        scene.add(new T.Mesh(mergeGeometries(geometries),mat));
        for(const geometry of geometries)geometry.dispose();
    }
    unit.dispose();
    const shadow=makeGroundShadow(2.45,1.65);
    shadow.position.set(.035,.102,.12);
    scene.add(shadow,new T.HemisphereLight(0xc2dfed,0x8c7c69,1.32));
    const key=new T.DirectionalLight(0xffead2,2.62);
    key.position.set(1.8,4.2,4);
    const rim=new T.DirectionalLight(0x8dd9ee,2.35);
    rim.position.set(-3.8,2.6,-2.35);
    scene.add(key,rim);
    return scene;
}
