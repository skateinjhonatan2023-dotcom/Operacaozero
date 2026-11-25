import * as T from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import botIndices from './assets/soldier-bot-indices.json';

export const operators = [
    {id:'vigia', name:'Vigia', role:'Reconhecimento', ability:'Pulso de radar', cooldown:48, duration:8,
        color:0x67e5d0, cloth:0x36474e, armor:0x506f71, fabric:0x46585a, metal:0x293d42, visor:0x438789,
        description:'Mostra o movimento dos adversários em até 46 m no minimapa por 8 s.'},
    {id:'brasa', name:'Brasa', role:'Linha de frente', ability:'Blindagem reativa', cooldown:54, duration:7,
        color:0xff9155, cloth:0x503932, armor:0x9a6243, fabric:0x694c41, metal:0x524945, visor:0x8f533e,
        description:'Absorve até 34 de dano de combate durante 7 s.'},
    {id:'iris', name:'Íris', role:'Resgate', ability:'Triagem de campo', cooldown:52, duration:6,
        color:0xa9acf9, cloth:0x394758, armor:0x61718c, fabric:0x727c8b, metal:0x3b4551, visor:0x6b6ea8,
        description:'Recupera até 30 de vida ao longo de 6 s dentro da zona segura.'},
];

export function freshAbility() {
    return {until:0,readyAt:0,shield:0};
}
export function activateAbility(state,operator,elapsed) {
    if(elapsed<state.readyAt)return false;
    state.until=elapsed+operator.duration;
    state.readyAt=elapsed+operator.cooldown;
    state.shield=operator.id==='brasa'?34:0;
    return true;
}
export function healingWithAbility(state,operator,elapsed,dt,inside) {
    return operator.id==='iris'&&inside?5*Math.min(dt,Math.max(0,state.until-(elapsed-dt))):0;
}
export function defendWithAbility(state,elapsed,damage) {
    if(elapsed>=state.until||state.shield<=0)return damage;
    const absorbed=Math.min(damage,state.shield);
    state.shield-=absorbed;
    return damage-absorbed;
}

const outfitMaterials=new Map();
const kits=new Map();
const headModels=new Map();
const bodyGeometries=new WeakMap();
const botGeometries=new Map();
const botMaterial=new T.MeshStandardMaterial({vertexColors:true,roughness:.82});

export function equipBot(actor,profile) {
    const parts=[];
    actor.body.traverse(node=>{if(node.isSkinnedMesh)parts.push(node);});
    const source=parts[0];
    if(!source||parts.some(mesh=>mesh.parent!==source.parent||!mesh.bindMatrix.equals(source.bindMatrix)||
        mesh.skeleton.bones.some((bone,i)=>bone!==source.skeleton.bones[i]))){
        equipAppearance(actor,profile,false);
        return;
    }
    if(!botGeometries.has(profile.id)){
        const pieces=parts.map(mesh=>{
            const geometry=new T.BufferGeometry();
            for(const attribute of ['position','normal','skinIndex','skinWeight'])
                geometry.setAttribute(attribute,mesh.geometry.getAttribute(attribute));
            const reduced=botIndices[mesh.material.name];
            geometry.setIndex(reduced?.count===geometry.attributes.position.count
                ?new T.Uint16BufferAttribute(reduced.indices,1):mesh.geometry.index);
            const {r,g,b}=paint(mesh.material,profile).color;
            const colors=new Float32Array(geometry.attributes.position.count*3);
            for(let i=0;i<colors.length;i+=3){colors[i]=r;colors[i+1]=g;colors[i+2]=b;}
            geometry.setAttribute('color',new T.BufferAttribute(colors,3));
            return geometry;
        });
        botGeometries.set(profile.id,mergeGeometries(pieces));
    }
    const geometry=botGeometries.get(profile.id);
    if(!geometry){equipAppearance(actor,profile,false);return;}
    const model=new T.SkinnedMesh(geometry,botMaterial);
    model.position.copy(source.position);model.quaternion.copy(source.quaternion);model.scale.copy(source.scale);
    model.bind(source.skeleton,source.bindMatrix);
    model.bindMode=source.bindMode;
    model.frustumCulled=false;
    source.parent.add(model);
    for(const mesh of parts)mesh.removeFromParent();
}

export function registerOperatorHeads(brasa,iris) {
    headModels.set('brasa',brasa);
    headModels.set('iris',iris);
}

// Cut the helmet off the licensed uniform using the head region of its bind-pose mesh.
function openUniform(geometry) {
    if(bodyGeometries.has(geometry))return bodyGeometries.get(geometry);
    const indices=geometry.index;
    if(!indices)return geometry;
    const positions=geometry.attributes.position;
    const cut=i=>positions.getY(i)>1.69&&Math.abs(positions.getX(i))<.23;
    const keep=[];
    for(let i=0;i<indices.count;i+=3){
        const a=indices.getX(i),b=indices.getX(i+1),c=indices.getX(i+2);
        if(Number(cut(a))+Number(cut(b))+Number(cut(c))<2)keep.push(a,b,c);
    }
    const result=keep.length===indices.count?geometry:geometry.clone().setIndex(keep);
    bodyGeometries.set(geometry,result);
    return result;
}
const armorShape=new T.BoxGeometry(1,1,1);
const rounded=new T.SphereGeometry(1,12,8);
const fastener=new T.CylinderGeometry(1,1,1,8);
const scratchPosition=new T.Vector3();
const scratchRotation=new T.Quaternion();
const rootRotation=new T.Quaternion();
let textile;
let materialDepth=0;
const texturedCloth=new Set();

function fabricTexture() {
    if(textile)return textile;
    const size=128, bytes=new Uint8Array(size*size*4);
    let seed=4319;
    for(let y=0;y<size;y++)for(let x=0;x<size;x++){
        seed=(Math.imul(seed,1664525)+1013904223)>>>0;
        const fiber=(x%4===0?23:0)+(y%4===1?18:0);
        const grain=(seed>>>27)*.46;
        const brightness=218+fiber*.5+grain;
        const i=(y*size+x)*4;
        bytes[i]=bytes[i+1]=bytes[i+2]=brightness;
        bytes[i+3]=255;
    }
    textile=new T.DataTexture(bytes,size,size,T.RGBAFormat);
    textile.colorSpace=T.SRGBColorSpace;
    textile.wrapS=textile.wrapT=T.RepeatWrapping;
    textile.repeat.set(3,3);
    textile.magFilter=T.LinearFilter;
    textile.minFilter=T.LinearMipmapLinearFilter;
    textile.generateMipmaps=true;
    textile.needsUpdate=true;
    return textile;
}

function addFabricDepth(material){
    texturedCloth.add(material);
    material.bumpMap=materialDepth?fabricTexture():null;
    material.bumpScale=materialDepth;
}

export function setFabricDepth(depth){
    if(materialDepth===depth)return;
    materialDepth=depth;
    for(const material of texturedCloth){
        const map=depth?fabricTexture():null;
        if(material.bumpMap!==map){material.bumpMap=map;material.needsUpdate=true;}
        material.bumpScale=depth;
    }
}

function paint(source,profile) {
    const key=(profile.appearanceKey??profile.id)+':'+source.uuid;
    if(outfitMaterials.has(key))return outfitMaterials.get(key);
    const name=source.name.toLowerCase();
    const material=source.clone();
    if(name.includes('cloth')||name.includes('fabric')||name.includes('armor')){
        material.color.setHex(name.includes('armor')?profile.armor:name.includes('fabric')?profile.fabric:profile.cloth);
        material.map=fabricTexture();
        addFabricDepth(material);
        material.roughness=name.includes('armor')?.71:.94;
        material.metalness=.015;
    }else if(name.includes('glass')){
        material.color.setHex(profile.visor);
        material.emissive.setHex(profile.color);
        material.emissiveIntensity=.07;
        material.roughness=.26;
        material.metalness=.33;
    }else if(name.includes('metal')||name.includes('iron')){
        material.color.setHex(profile.metal);
        material.roughness=.49;
        material.metalness=.45;
    }else if(name.includes('elements')){
        material.color.setHex(profile.fabric).multiplyScalar(.72);
    }
    outfitMaterials.set(key,material);
    return material;
}

function createKit(profile) {
    const shell=new T.MeshStandardMaterial({color:profile.armor,roughness:.66,metalness:.18});
    const fabric=new T.MeshStandardMaterial({color:profile.cloth,map:fabricTexture(),roughness:.88});
    const metal=new T.MeshStandardMaterial({color:profile.metal,roughness:.53,metalness:.48});
    const stripe=new T.MeshStandardMaterial({color:profile.color,emissive:profile.color,emissiveIntensity:.19,roughness:.59});
    const slots={back:new T.Group(),head:new T.Group()};
    const face=headModels.get(profile.id);
    if(face){
        const head=face.clone(true);
        head.scale.setScalar(profile.id==='iris'?.91:.97);
        head.traverse(object=>{
            if(!object.isMesh)return;
            object.castShadow=true;object.receiveShadow=true;
            if(profile.id==='iris'&&object.material.name.toLowerCase().includes('hair')){
                object.material=object.material.clone();
                object.material.color.setHex(0x574758);
            }
        });
        slots.head.add(head);
    }
    function part(group,geometry,material,x,y,z,w,h,d) {
        const mesh=new T.Mesh(geometry,material);
        mesh.position.set(x,y,z);mesh.scale.set(w,h,d);
        mesh.castShadow=true;mesh.receiveShadow=true;group.add(mesh);
        return mesh;
    }
    const box=(group,mat,x,y,z,w,h,d)=>part(group,armorShape,mat,x,y,z,w,h,d);
    if(face)part(slots.head,rounded,fabric,0,-.069,-.011,.098,.042,.102);
    const back=slots.back;
    for(const x of [-.108,.108]){
        box(back,fabric,x,-.044,.144,.037,.195,.026);
        box(back,metal,x,-.083,.163,.046,.034,.025);
    }
    box(back,shell,0,-.131,.146,.19,.073,.039);
    box(back,stripe,0,-.127,.169,.105,.012,.009);
    if(profile.id==='vigia'){
        box(back,metal,0,.015,-.218,.255,.273,.126);
        box(back,shell,0,.068,-.29,.234,.137,.042);
        box(back,stripe,0,.084,-.317,.151,.017,.009);
        box(back,metal,.102,.21,-.251,.022,.23,.024).rotation.z=-.16;
        part(slots.head,rounded,shell,.114,-.026,-.015,.032,.043,.038);
        box(slots.head,stripe,.140,-.025,-.018,.014,.027,.049);
    }else if(profile.id==='brasa'){
        box(back,metal,0,.013,-.269,.31,.38,.114);
        for(const x of [-.074,.074]){
            box(back,shell,x,.02,-.333,.119,.286,.065);
            box(back,stripe,x,.058,-.372,.071,.021,.012);
        }
        for(const side of [-1,1])
            box(back,shell,side*.115,-.014,.148,.055,.146,.035).rotation.z=side*.12;
    }else{
        part(back,rounded,fabric,0,-.027,-.242,.185,.21,.104);
        box(back,shell,0,.042,-.33,.248,.172,.064);
        box(back,stripe,0,.042,-.368,.108,.026,.009);
        box(back,stripe,0,.042,-.369,.026,.102,.009);
        for(const x of [-.132,.132])
            part(back,fastener,metal,x,-.12,-.266,.026,.179,.026);
        part(slots.head,rounded,shell,-.115,-.024,-.015,.029,.04,.038);
        box(slots.head,stripe,-.137,-.024,-.015,.013,.022,.05);
    }
    return slots;
}

export function equipAppearance(actor,profile,accessories=true) {
    if(actor.appearance)for(const attachment of actor.appearance)actor.root.remove(attachment.mesh);
    const exposedFace=accessories&&headModels.has(profile.id);
    const stature=actor.body.userData.uniformScale??=actor.body.scale.clone();
    const width=accessories?(profile.id==='iris'?.91:profile.id==='brasa'?1.025:1):1;
    actor.body.scale.set(stature.x*width,stature.y,stature.z*(profile.id==='iris'&&accessories?.96:1));
    actor.body.traverse(object=>{
        if(!object.isSkinnedMesh)return;
        const geometry=object.userData.originalGeometry??=object.geometry;
        object.geometry=exposedFace?openUniform(geometry):geometry;
        const source=object.userData.originalMaterial??=object.material;
        object.material=Array.isArray(source)?source.map(mat=>paint(mat,profile)):paint(source,profile);
    });
    actor.appearance=[];
    if(!accessories)return;
    const kitKey=profile.appearanceKey??profile.id;
    if(!kits.has(kitKey))kits.set(kitKey,createKit(profile));
    const slots=kits.get(kitKey);
    const sockets={back:'Spine2',head:'Head'};
    const bones={};
    actor.body.traverse(object=>{
        if(!object.isBone)return;
        for(const [key,name] of Object.entries(sockets))if(object.name.endsWith(name))bones[key]=object;
    });
    for(const [key,source] of Object.entries(slots)){
        if(!source.children.length||!bones[key])continue;
        const mesh=source.clone(true);
        actor.root.add(mesh);
        actor.appearance.push({mesh,bone:bones[key]});
    }
}

export function updateAppearance(actor) {
    if(!actor.appearance?.length)return;
    actor.root.updateWorldMatrix(true,false);
    actor.root.getWorldQuaternion(rootRotation).invert();
    for(const {mesh,bone} of actor.appearance){
        bone.getWorldPosition(scratchPosition);
        mesh.position.copy(actor.root.worldToLocal(scratchPosition));
        bone.getWorldQuaternion(scratchRotation);
        mesh.quaternion.copy(rootRotation).multiply(scratchRotation);
    }
}
