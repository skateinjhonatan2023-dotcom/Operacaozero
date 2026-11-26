import { collisionIndex } from './collision.js';
import { makeGroundShadow, makeStudio } from './studio.js';
import { adaptiveResolution, createPresentation } from './presentation.js';
import { setupAccount } from './account.js';
import { cosmetics } from './cosmetics.js';
import { createWeaponAudio } from './weapon-audio.js';
import { activateAbility, defendWithAbility, equipAppearance, equipBot, freshAbility, healingWithAbility, operators, registerOperatorHeads, setFabricDepth, updateAppearance } from './operators.js';
import { districts, groundHeight } from './terrain.js';
import { initialRadius, roomSizes, spawnPositions, totalTime, zoneAt, zoneDelay } from './match.js';
import { findVault, floorAt, freeAt, stepTo, vaultPoint } from './traversal.js';
import { automaticOpening, descentStep, firstJump, flightFrom, flightPosition, flightSeconds, flightTo,
    landingFloor, locateZiplines, makeAircraft, makeCanopy, makeMusterPad, makeZiplineScenery,
    musterSeconds, nearestZipline, ziplinePoint } from './insertion.js';
import { distributeSupplies } from './supplies.js';
import { arsenal, families, standardWeapon, weaponModel } from './weapons.js';
import { gripBones, restoreGrip, updateGrip } from './grip.js';
import { presets, setupSettings } from './settings.js';
import * as T from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { clone } from 'three/addons/utils/SkeletonUtils.js';
import { MeshoptDecoder } from 'three/addons/libs/meshopt_decoder.module.js';
import { boundary, buildArena, lightArena } from './arena.js';

const $ = id => document.getElementById(id);
const canvas = $('world');
const scene = new T.Scene();
const camera = new T.PerspectiveCamera(62,1,.07,850);
const keys = new Set();
const stick = new T.Vector2();
const input = { yaw:0, pitch:-.095, aim:false, fire:false };
const ray = new T.Raycaster();
const origin = new T.Vector3();
const forward = new T.Vector3();
const shoulder = new T.Vector3();
const scratch = new T.Vector3();
const idealCamera = new T.Vector3();
const cameraAnchor=new T.Vector3();
const shotOffset=new T.Vector2(),muzzlePoint=new T.Vector3(),shotImpact=new T.Vector3();
const shotHit=new T.Vector3(),shotSphere=new T.Sphere();
const localDirection = new T.Vector2();
const moveInput=new T.Vector2(), groundVelocity=new T.Vector2();
const view=new T.Frustum(),viewProjection=new T.Matrix4(),actorBounds=new T.Sphere();
const map = $('map').getContext('2d');
const bots = [];
const loot = [];
const beams = [];
const deployments=[];
const wallGeometry=new T.BoxGeometry(1,1,1);
const smokeGeometry=new T.SphereGeometry(1,12,8);
const utilityMaterials={
    wall:new T.MeshStandardMaterial({color:0x61acac,metalness:.38,roughness:.46}),
    smoke:new T.MeshBasicMaterial({color:0xa4b5b0,transparent:true,opacity:.48,depthWrite:false}),
    flash:new T.MeshBasicMaterial({color:0xffb75f,transparent:true,opacity:.64,depthWrite:false,blending:T.AdditiveBlending}),
    fire:new T.MeshBasicMaterial({color:0xf98543,transparent:true,opacity:.37,depthWrite:false}),
    freeze:new T.MeshBasicMaterial({color:0x52cbff,transparent:true,opacity:.30,depthWrite:false}),
    sonar:new T.MeshBasicMaterial({color:0xb2fe75,transparent:true,opacity:.2,depthWrite:false,wireframe:true}),
};
let weapon=standardWeapon;
let loadout=[standardWeapon,arsenal.find(item=>item.name==='MP40')];
try {
    const saved=JSON.parse(localStorage.getItem('operation-zero-loadout-v1'));
    if(Array.isArray(saved)&&saved.length===2){
        const picks=saved.map(name=>arsenal.find(item=>item.name===name));
        if(picks.every(Boolean))loadout=picks;
    }
} catch {}
let inventory=[], equipped=0, burstRemaining=0;
let lastCrouch=-Infinity, slideTime=0, vault=null;
const slideDirection=new T.Vector2();
let roomSize=45, lastZoneDraw=-1;
let terrainMap=null;
let operator=operators[0],previewOperator=null, ability=freshAbility(), inspecting=false;
let lighting, abilityRing, rockSurface, stoneColor, stoneDetail, groundShadow;
let loadHdSurfaces, hdSurfaces=null, hdSurfaceRequest=null, hdSurfacesUnavailable=false;
const contactNormal=new T.Vector3(),planeNormal=new T.Vector3(0,0,1);
try {
    const id=localStorage.getItem('operation-zero-operator-v1');
    operator=operators.find(item=>item.id===id)??operator;
} catch {}
const clamp = T.MathUtils.clamp;

let renderer, arena, studio, collision, shell, clips, rifle, botRifle, player, bodyScale, bodyFloor, pickup, zoneRing, hitSound, audio;
const resolutionControl=adaptiveResolution();
let presentation,renderPreference='',renderDirty=true;
let accountProfile=null,collection=null,playWeaponAudio=null;
const throwables={frag:{label:'Granada',name:'fragmentação arremesso',type:'throwable'},smoke:{label:'Fumaça',name:'fumaça arremesso',type:'throwable'}};
const grenadeBlast={name:'fragmentação detonação',type:'grenade'};
const grenadeMesh=new T.SphereGeometry(.075,10,8);
const throwableMaterials={
    frag:new T.MeshStandardMaterial({color:0x9c731d,roughness:.65,metalness:.3}),
    smoke:new T.MeshStandardMaterial({color:0xa6bec8,roughness:.56,metalness:.42}),
};
let grenades={frag:0,smoke:0},selectedThrow='frag';
const airborneItems=[];
const deploymentStages=new Set(['waiting','flight','descent']);
const airDirection=flightTo.clone().sub(flightFrom).normalize();
const airVelocity=new T.Vector3(),airInput=new T.Vector3(),travelPoint=new T.Vector3();
let aircraft,canopy,pad,ziplines=[],zipRide=null,nearZip=null;
let stageTime=0,nextStageHud=0,chuteOpen=false,dropAltitude=0,pausedState='playing';
let state='loading', last=performance.now(), now=0, elapsed=0;
let radius=initialRadius, kills=0, ammo=weapon.capacity, reserve=90, kits=1, reload=0, nextShot=0;
let damageFlash=0, hitUntil=0, nextHud=0, nextShadow=0, aimPunch=0, shotHeat=0, nearby=null;
let settings, activePreset, lastFrame=0, verticalSpeed=0, crouched=false, running=false;
let aimBlend=0, sprintBlend=0;
let lastVisualTime=0, nextMapDraw=0;
let studioResolution=false;
let queuedShot=false, headUntil=0;
let lookPointer=null, lookX=0, lookY=0, movePointer=null, stickBounds=null, firePointer=null, fireX=0,fireY=0;
const lootGeometry = new T.DodecahedronGeometry(.17,0);
const lootBoxGeometry=new T.BoxGeometry(1,1,1);
const kitMarkMaterial=new T.MeshBasicMaterial({color:0xdff7dc});
const ammoMaterial = new T.MeshStandardMaterial({color:0xe3c86c,emissive:0x8a5c16,emissiveIntensity:.6});
const weaponLootMaterial=new T.MeshStandardMaterial({color:0x70ced9,emissive:0x185965,emissiveIntensity:.75});
const kitMaterial = new T.MeshStandardMaterial({color:0x9cf6c9,emissive:0x20734d,emissiveIntensity:.6});

async function boot() {
    renderer = new T.WebGLRenderer({canvas,antialias:false,powerPreference:'high-performance'});
    renderer.outputColorSpace=T.SRGBColorSpace;
    renderer.toneMapping=T.ACESFilmicToneMapping;
    renderer.toneMappingExposure=1.25;
    presentation=createPresentation(renderer);
    renderer.shadowMap.enabled=true;
    renderer.shadowMap.type=T.PCFSoftShadowMap;
    renderer.shadowMap.autoUpdate=false;
    const { concrete, asphalt, grass, stone, soldier, weapon, distantWeapon, outcrop, tree, distantTree, shot, coastalSky,
        concreteNormal,asphaltNormal,grassNormal,stoneNormal,brasaHead,irisHead } = window.ZERO_ASSETS;
    const loader = new GLTFLoader().setMeshoptDecoder(MeshoptDecoder);
    const images = new T.TextureLoader();
    async function loadTexture(url,normal=false) {
        const texture=await images.loadAsync(url);
        texture.colorSpace=normal?T.NoColorSpace:T.SRGBColorSpace;
        texture.wrapS=texture.wrapT=T.RepeatWrapping;
        texture.anisotropy=Math.min(renderer.capabilities.getMaxAnisotropy(),4);
        return texture;
    }
    loadHdSurfaces=async()=>{
        const keys=['concrete','asphalt','grass','stone','concreteNormal','asphaltNormal','grassNormal','stoneNormal'];
        const pairs=await Promise.all(keys.map(async key=>[key,await loadTexture(window.ZERO_ASSETS[key+'HD'],key.endsWith('Normal'))]));
        return Object.fromEntries(pairs);
    };
    const [body,gun,lightGun,rocks,foliage,lightTrees,c,a,g,s,cn,an,gn,sn,brasa,iris,panorama] = await Promise.all([
        loader.loadAsync(soldier), loader.loadAsync(weapon), loader.loadAsync(distantWeapon),loader.loadAsync(outcrop),
        loader.loadAsync(tree),loader.loadAsync(distantTree),
        ...[concrete,asphalt,grass,stone].map(url=>loadTexture(url)),
        ...[concreteNormal,asphaltNormal,grassNormal,stoneNormal].map(url=>loadTexture(url,true)),
        loader.loadAsync(brasaHead),loader.loadAsync(irisHead),
        loadTexture(coastalSky).catch(()=>null),
    ]);
    hitSound=shot;
    shell=body.scene;
    registerOperatorHeads(brasa.scene,iris.scene);
    clips=new Map(body.animations.map(clip=>[clip.name,clip]));
    addDirectionalClips();
    rifle=gun.scene;
    botRifle=lightGun.scene;
    const rifleMaterials=new Map();
    rifle.traverse(part=>{if(part.isMesh)rifleMaterials.set(part.material.name,part.material);});
    botRifle.traverse(part=>{if(part.isMesh)part.material=rifleMaterials.get(part.material.name)??part.material;});
    shell.updateMatrixWorld(true);
    const calibration = new T.AnimationMixer(shell);
    calibration.clipAction(clips.get('ready_idle')).play();
    calibration.update(.1);
    shell.updateMatrixWorld(true);
    const bounds=new T.Box3().setFromObject(shell,true);
    bodyScale=1.81/(bounds.max.y-bounds.min.y);
    bodyFloor=-bounds.min.y*bodyScale;
    if(!Number.isFinite(bodyScale))throw new Error('Escala de personagem inválida.');
    calibration.stopAllAction();
    stoneColor=s;
    rockSurface=new T.MeshStandardMaterial({map:s,color:0xc7c6b6,roughness:.94});
    rockSurface.normalScale.setScalar(.83);
    stoneDetail=sn;
    rocks.scene.traverse(o=>{if(o.isMesh)o.material=rockSurface;});
    lighting=lightArena(scene,panorama);
    arena=buildArena(scene,{concrete:c,asphalt:a,grass:g,concreteNormal:cn,asphaltNormal:an,grassNormal:gn},rocks.scene,foliage.scene,lightTrees.scene);
    studio=makeStudio(c,a);
    groundShadow=makeGroundShadow(1.57,1.36);
    groundShadow.visible=false;
    scene.add(groundShadow);
    collision=collisionIndex(arena.obstacles);
    ziplines=locateZiplines(collision);
    aircraft=makeAircraft();aircraft.plane.visible=false;
    pad=makeMusterPad(0,19);pad.visible=false;
    scene.add(aircraft.plane,pad,makeZiplineScenery(ziplines));
    activePreset=presets[settings.quality];
    renderer.shadowMap.enabled=Boolean(activePreset.shadowSize);
    applyDetail();
    player=createActor(0,16,100);
    canopy=makeCanopy();canopy.visible=false;player.root.add(canopy);
    equipAppearance(player,operatorLook(operator));
    const circle=new T.BufferGeometry().setFromPoints(Array.from({length:40},(_,i)=>{
        const a=i/40*Math.PI*2;
        return new T.Vector3(Math.cos(a),0,Math.sin(a));
    }));
    abilityRing=new T.LineLoop(circle,new T.LineBasicMaterial({color:operator.color,transparent:true,depthWrite:false}));
    abilityRing.visible=false;
    scene.add(abilityRing);
    showLoadoutWeapon();
    player.root.rotation.y=Math.PI;
    const points=Array.from({length:120},(_,i)=>{
        const angle=i/120*Math.PI*2;
        return new T.Vector3(Math.cos(angle),.025,Math.sin(angle));
    });
    zoneRing = new T.LineLoop(new T.BufferGeometry().setFromPoints(points),
        new T.LineBasicMaterial({color:0x9deff2,transparent:true,opacity:.78}));
    zoneRing.visible=false;
    scene.add(zoneRing);
    for(let i=0;i<12;i++){
        const geometry=new T.BufferGeometry().setFromPoints([new T.Vector3(),new T.Vector3()]);
        const material=new T.LineBasicMaterial({color:0xffda88,transparent:true});
        const line=new T.Line(geometry,material);
        line.visible=false;
        line.frustumCulled=false;
        scene.add(line);
        beams.push({line,until:0});
    }
    resize();
    addEventListener('resize',resize);
    $('launch-status').hidden=true;
    $('deploy').disabled=false;
    document.body.dataset.ready='true';
    showEntryOrLobby();
    requestAnimationFrame(frame);
}

function createActor(x,z,hp,y=groundHeight(x,z),profile=null) {
    const body=clone(shell);
    const root=new T.Group();
    root.add(body);
    root.position.set(x,y,z);
    body.scale.multiplyScalar(bodyScale);
    body.position.y+=bodyFloor;
    body.traverse(object=>{
        if(object.isMesh) { object.castShadow=!profile;object.receiveShadow=!profile; }
        if(object.isSkinnedMesh) object.frustumCulled=false;
    });
    const gun=weaponModel(standardWeapon,profile?botRifle:rifle);
    root.add(gun);
    scene.add(root);
    const actor={root,body,gun,hp,bones:gripBones(body),weapon:standardWeapon,mix:new T.AnimationMixer(body),clip:null,action:null,shot:0,
        nextFire:0,strafe:Math.random()<.5?-1:1,animationTime:0};
    if(profile)equipBot(actor,profile);
    pose(actor,'ready_idle');
    return actor;
}

function clearBots(){
    for(const bot of bots.splice(0)){
        bot.mix.stopAllAction();bot.mix.uncacheRoot(bot.body);
        bot.body.traverse(node=>{if(node.isSkinnedMesh)node.skeleton.dispose();});
        bot.root.removeFromParent();
    }
}

function addDirectionalClips() {
    const sourceClips=[...clips.values()];
    for(const [name,gait] of [['ready_walk','walk'],['aim_walk','walk'],
        ['ready_crouch_walk','crouch'],['aim_crouch_walk','crouch']]) {
        const base=clips.get(name);
        if(!base)continue;
        for(const [direction,suffix] of [['left','L'],['right','R'],['back','B']]) {
            const motion=sourceClips.find(clip=>clip.name.startsWith(`mocap_${gait}_${suffix}@`));
            if(!motion)continue;
            const legs=new Map(motion.tracks.filter(track=>/(UpLeg|Leg|Foot)\.quaternion$/.test(track.name))
                .map(track=>[track.name,track.clone().scale(base.duration/motion.duration)]));
            if(legs.size!==6)continue;
            const key=`${name}_${direction}`;
            const tracks=base.tracks.map(track=>legs.get(track.name)??track);
            clips.set(key,new T.AnimationClip(key,base.duration,tracks));
        }
    }
}

function pose(actor,name,cadence=0) {
    const clip=clips.get(name)||clips.get('idle');
    if(!clip)return;
    if(actor.clip===clip){
        actor.action.timeScale=cadence||1;
        return;
    }
    const action=actor.mix.clipAction(clip).reset().play();
    if(cadence&&actor.cadence){
        action.time=(actor.action.time/actor.clip.duration%1)*clip.duration;
    }
    action.timeScale=cadence||1;
    if(name==='death'){action.setLoop(T.LoopOnce,1);action.clampWhenFinished=true;}
    if(actor.action)action.crossFadeFrom(actor.action,.15,false);
    actor.action=action;
    actor.clip=clip;
    actor.cadence=cadence;
}




function start() {
    if(!accountProfile||!player)return;
    if(player.root.parent!==scene)scene.add(player.root);
    clearDeployments();
    clearThrows();
    resetTraversal();
    grenades={frag:0,smoke:0};selectedThrow='frag';
    ability=freshAbility();
    abilityRing.visible=false;
    clearBots();
    for (const item of loot.splice(0)) scene.remove(item.mesh);
    elapsed=0;kills=0;
    inventory=loadout.map(profile=>({profile,ammo:profile.capacity,reserve:profile.capacity*3}));
    equipped=0;
    switchWeapon(0);kits=1;reload=0;nextShot=0;radius=initialRadius;
    nextHud=0;nextShadow=0;nextMapDraw=0;damageFlash=0;hitUntil=0;headUntil=0;aimPunch=0;nearby=null;
    $('flash').style.opacity=0;
    input.fire=false;input.aim=false;input.yaw=0;input.pitch=-.095;stick.set(0,0);
    verticalSpeed=0;crouched=false;slideTime=0;vault=null;lastCrouch=-Infinity;clearInput();
    player.root.position.set(0,0,19);
    player.root.rotation.set(0,Math.PI,0);
    player.hp=100;player.shot=0;
    player.root.visible=true;player.gun.visible=false;
    const spawns=spawnPositions(roomSize-1,(x,y,z)=>canMove(x,z,.55,y,1.8));
    for(const [i,point] of spawns.entries()){
        const angle=i*2.39996,r=4.1+(i%4)*1.55;
        const x=Math.cos(angle)*r,z=19+Math.sin(angle)*r;
        const y=groundHeight(x,z);
        const bot=createActor(x,z,78,y,operators[i%operators.length]);
        bot.landing=point;
        bot.waiting=canMove(x,z,.6,y);
        bot.root.visible=bot.waiting&&i<activePreset.waiting;bot.gun.visible=false;
        bot.root.rotation.y=-angle;
        pose(bot,i%4===0?'walk':'idle');
        bot.nextFire=3+i*.12;
        bot.planInterval=.13+(i%9)*.006;
        bots.push(bot);
    }
    const supplies=distributeSupplies(arena.lootSites,arsenal,
        point=>canMove(point.x,point.z,.34,point.y,1.12));
    for(const item of supplies)placeLoot(item.x,item.z,item.kind,item.y,item.weapon);
    for(const beam of beams)beam.line.visible=false;
    zoneRing.visible=true;
    lastZoneDraw=-1;
    updateZoneRing();
    $('lobby').hidden=true;$('result').hidden=true;$('hud').hidden=false;
    pad.visible=true;
    enterPhase('waiting');
    initAudio();
    updateCamera(1);
}

function enterPhase(phase) {
    stageTime=0;
    nextStageHud=0;
    state=phase;
    document.body.dataset.state=phase;
    document.body.dataset.phase=phase;
    $('deployment').hidden=!deploymentStages.has(phase);
    $('zip-action').hidden=true;
    if(phase==='playing')updateHud();
    else updateDeploymentHud();
}

function resetTraversal(){
    zipRide=null;nearZip=null;chuteOpen=false;
    delete document.body.dataset.zipline;
    airVelocity.set(0,0,0);
    pad.visible=false;canopy.visible=false;aircraft.plane.visible=false;
    $('deployment').hidden=true;$('zip-action').hidden=true;
    delete document.body.dataset.phase;
}

function beginFlight(){
    clearInput();
    pad.visible=false;
    aircraft.plane.visible=true;
    aircraft.plane.rotation.y=Math.atan2(airDirection.x,airDirection.z);
    flightPosition(0,aircraft.plane.position);
    player.root.position.copy(aircraft.plane.position);
    player.root.visible=false;
    for(const bot of bots)bot.root.visible=false;
    enterPhase('flight');
    flightCamera();
}

function leaveAircraft(){
    if(state!=='flight'||stageTime<firstJump)return;
    const p=player.root.position;
    p.copy(aircraft.plane.position).addScaledVector(airDirection,-4);
    p.y-=2.2;
    dropAltitude=p.y-landingFloor(collision,p.x,p.z);
    input.yaw=Math.atan2(airDirection.x,-airDirection.z);input.pitch=-.17;
    airVelocity.copy(airDirection).multiplyScalar(12);
    player.root.visible=true;
    pose(player,'jump_loop');
    for(const bot of bots){
        const wing=canopy.clone(true);
        wing.visible=true;wing.scale.setScalar(.79);bot.root.add(wing);bot.canopy=wing;
        pose(bot,'jump_loop');
    }
    enterPhase('descent');
    descentCamera(1);
}

function openParachute(){
    if(state!=='descent'||chuteOpen)return;
    chuteOpen=true;
    canopy.visible=true;
    $('deployment-action').hidden=true;
}

function beginCombat(){
    canopy.visible=false;aircraft.plane.visible=false;
    player.gun.visible=true;
    verticalSpeed=0;
    pose(player,'jump_land');
    for(const bot of bots){
        const {x,y,z}=bot.landing;
        bot.root.position.set(x,y,z);
        bot.root.remove(bot.canopy);bot.canopy=null;
        bot.gun.visible=true;
        pose(bot,'ready_idle');
    }
    enterPhase('playing');
    updateCamera(1);
}

function inputDirection(out){
    const x=Number(keys.has('KeyD'))-Number(keys.has('KeyA'))+stick.x;
    const z=Number(keys.has('KeyS'))-Number(keys.has('KeyW'))+stick.y;
    const sine=Math.sin(input.yaw),cosine=Math.cos(input.yaw);
    return out.set(x*cosine-z*sine,0,x*sine+z*cosine).clampLength(0,1);
}

function flightCamera(){
    const p=aircraft.plane.position;
    camera.position.copy(p).addScaledVector(airDirection,-31);
    camera.position.x+=airDirection.z*20;camera.position.z-=airDirection.x*20;
    camera.position.y+=18;
    camera.lookAt(travelPoint.copy(p).addScaledVector(airDirection,9).setY(p.y-13));
    camera.fov=camera.aspect<1?67:62;
    camera.updateProjectionMatrix();
}

function descentCamera(weight){
    const p=player.root.position;
    forward.set(Math.sin(input.yaw),0,-Math.cos(input.yaw));
    idealCamera.copy(p).addScaledVector(forward,chuteOpen?-9:-7);
    idealCamera.y+=chuteOpen?5.8:4.4;
    camera.position.lerp(idealCamera,weight);
    camera.lookAt(travelPoint.copy(p).addScaledVector(forward,9).setY(p.y-4+input.pitch*10));
    camera.fov=T.MathUtils.lerp(camera.fov,68,weight);
    camera.updateProjectionMatrix();
}

function animateActor(actor,dt){
    if(!actor.root.visible)return;
    const controlled=actor===player;
    actor.animationTime+=dt;
    const near=controlled||actor.root.position.distanceToSquared(player.root.position)<38**2;
    if(!controlled&&actor.animationTime<(near?1/24:1/12))return;
    if(!controlled&&state==='playing'&&actor.hp>0){
        const fighting=actor.target&&actor.root.position.distanceToSquared(actor.target.root.position)<25**2;
        pose(actor,elapsed<actor.shot?'shoot':fighting?'aim_walk':'ready_run');
    }
    restoreGrip(actor);actor.mix.update(actor.animationTime);actor.animationTime=0;updateAppearance(actor);
    if(actor.hp>0&&actor.gun.visible)updateGrip(actor,controlled?input.pitch:0,
        controlled?aimPunch:elapsed<actor.shot?.06:0,
        ['pistol','melee','grenade'].includes(actor.weapon.type),!controlled||input.aim);
}

function animateActors(dt){
    animateActor(player,dt);
    for(const bot of bots)animateActor(bot,dt);
}

function updateBotView(waiting=false){
    camera.updateMatrixWorld();
    view.setFromProjectionMatrix(viewProjection.multiplyMatrices(camera.projectionMatrix,camera.matrixWorldInverse));
    const p=player.root.position;
    const range=activePreset.bots*(state==='descent'?1.2:1);
    actorBounds.radius=state==='descent'?5:1.6;
    for(const [i,bot] of bots.entries()){
        const q=bot.root.position;
        actorBounds.center.copy(q);actorBounds.center.y+=.92;
        bot.root.visible=(!waiting||bot.waiting&&i<activePreset.waiting)&&
            q.distanceToSquared(p)<range*range&&view.intersectsSphere(actorBounds);
    }
}

function stepDeployment(dt,phaseDt=dt){
    stageTime+=phaseDt;
    if(state==='waiting'){
        if(stageTime>=musterSeconds){beginFlight();return;}
        updatePlayer(dt);
        for(const [i,bot] of bots.entries())if(i<activePreset.waiting&&bot.waiting&&bot.clip.name==='walk'){
            const a=bot.root.rotation.y;
            move(bot,Math.sin(a)*1.08,Math.cos(a)*1.08,dt);
            if(Math.hypot(bot.root.position.x,bot.root.position.z-19)>10)bot.root.rotation.y+=Math.PI;
        }
    }else if(state==='flight'){
        flightPosition(stageTime,aircraft.plane.position);
        for(const prop of aircraft.propellers)prop.rotation.z+=phaseDt*30;
        player.root.position.copy(aircraft.plane.position);
        flightCamera();
        if(stageTime>=flightSeconds){leaveAircraft();return;}
    }else{
        const p=player.root.position;
        const steps=Math.max(1,Math.ceil(phaseDt/.075)),step=phaseDt/steps;
        for(let i=0;i<steps;i++){
            if(p.y-landingFloor(collision,p.x,p.z)<=automaticOpening)openParachute();
            inputDirection(airInput);
            if(airInput.lengthSq()>.02)airInput.multiplyScalar(chuteOpen?14:19);
            else airInput.set(Math.sin(input.yaw),0,-Math.cos(input.yaw)).multiplyScalar(chuteOpen?1.6:5);
            if(descentStep(p,airVelocity,airInput,step,chuteOpen,collision)){beginCombat();return;}
        }
        player.root.rotation.y=Math.PI-input.yaw;
        canopy.rotation.z=T.MathUtils.lerp(canopy.rotation.z,-stick.x*.17,Math.min(1,dt*6));
        if(aircraft.plane.visible){
            aircraft.plane.position.addScaledVector(airDirection,phaseDt*29);
            for(const prop of aircraft.propellers)prop.rotation.z+=phaseDt*30;
            if(aircraft.plane.position.distanceToSquared(p)>250**2)aircraft.plane.visible=false;
        }
        const fraction=T.MathUtils.clamp((p.y-landingFloor(collision,p.x,p.z))/dropAltitude,0,1);
        for(const bot of bots){
            const {x,y,z}=bot.landing;
            const q=bot.root.position;
            q.set(x-21*fraction*airDirection.x,y+fraction*(35+(bot.nextFire%2)*21),z-21*fraction*airDirection.z);
            bot.root.rotation.y=Math.PI-input.yaw;
        }
        descentCamera(1-Math.exp(-5*dt));
    }
    if(stageTime>=nextStageHud){updateDeploymentHud();nextStageHud=stageTime+.08;}
}

function updateDeploymentHud(){
    const action=$('deployment-action');
    $('match-stage').textContent={waiting:'Ponto de embarque',flight:'Sobrevoo',descent:chuteOpen?'Paraquedas aberto':'Queda livre'}[state];
    if(state==='waiting')$('deployment-info').textContent=roomSize+' combatentes · decolagem em '+Math.ceil(musterSeconds-stageTime)+' s';
    else if(state==='flight')$('deployment-info').textContent='Rota no minimapa · '+Math.ceil(flightSeconds-stageTime)+' s para saltar';
    else{
        const p=player.root.position;
        const height=Math.max(0,Math.ceil(p.y-landingFloor(collision,p.x,p.z)));
        $('deployment-info').textContent=height+' m acima do solo · direcione a queda';
    }
    action.hidden=state==='waiting'||(state==='descent'&&chuteOpen);
    action.disabled=state==='flight'&&stageTime<firstJump;
    action.textContent=state==='flight'?'Saltar do avião':'Abrir paraquedas';
    drawMap();
}

function useZipline(){
    if(state!=='playing')return;
    if(zipRide){finishZipline();return;}
    if(vault||slideTime>0||Math.abs(verticalSpeed)>.1)return;
    const link=nearestZipline(ziplines,player.root.position);
    if(!link)return;
    const {route,reverse}=link;
    const from=reverse?route.b:route.a,to=reverse?route.a:route.b;
    zipRide={route,reverse,time:0,entry:player.root.position.clone()};
    clearInput();nearby=null;
    crouched=false;verticalSpeed=0;reload=0;
    input.yaw=Math.atan2(to.x-from.x,from.z-to.z);input.pitch=-.15;
    player.root.rotation.y=Math.PI-input.yaw;
    player.gun.visible=false;pose(player,'jump_loop');
    document.body.dataset.zipline=route.name;
    updateHud();
}

function stepZipline(dt){
    const ride=zipRide;
    ride.time+=dt;
    const progress=T.MathUtils.clamp((ride.time-.24)/ride.route.seconds,0,1);
    ziplinePoint(ride.route,ride.reverse?1-progress:progress,travelPoint);
    travelPoint.y-=1.92;
    if(ride.time<.24)travelPoint.lerp(ride.entry,1-ride.time/.24);
    if(!canMove(travelPoint.x,travelPoint.z,.36,travelPoint.y)){finishZipline();return;}
    player.root.position.copy(travelPoint);
    if(progress>=1)finishZipline();
    updateCamera(1-Math.exp(-11*dt));
}

function finishZipline(){
    zipRide=null;
    delete document.body.dataset.zipline;
    player.gun.visible=true;verticalSpeed=-.1;
    updateHud();
}

function placeLoot(x,z,kind,floor=0,profile=null,stored=null) {
    const mesh=new T.Group();
    let display=null;
    if(kind==='weapon'){
        const marker=new T.Mesh(lootGeometry,weaponLootMaterial);
        marker.position.y=.29;marker.scale.setScalar(.61);mesh.add(marker);
        display=weaponModel(profile,botRifle);
        display.rotation.z=Math.PI/2;display.scale.setScalar(.85);
        display.position.z=.19;mesh.add(display);
    }else if(Object.hasOwn(throwables,kind)){
        const item=new T.Mesh(grenadeMesh,throwableMaterials[kind]);
        item.scale.setScalar(2.4);mesh.add(item);
    }else{
        const box=new T.Mesh(lootBoxGeometry,kind==='kit'?kitMaterial:ammoMaterial);
        box.scale.set(kind==='kit'?.38:.29,.12,.25);mesh.add(box);
        if(kind==='kit')for(const [w,d] of [[.21,.05],[.05,.20]]){
            const mark=new T.Mesh(lootBoxGeometry,kitMarkMaterial);
            mark.position.y=.067;mark.scale.set(w,.014,d);mesh.add(mark);
        }
    }
    mesh.position.set(x,floor+.34,z);
    mesh.visible=false;
    scene.add(mesh);
    loot.push({mesh,kind,floor,profile,stored,display});
}

function canMove(x,z,r=.36,y=0,height=1.8) {
    return freeAt(collision,boundary,x,z,y,height,r);
}

function move(actor,x,z,dt) {
    const p=actor.root.position;
    const dx=x*dt,dz=z*dt;
    if(Math.abs(dx)+Math.abs(dz)<1e-7)return;
    if(actor!==player){
        const side=groundHeight(p.x+dx,p.z);
        if(Math.abs(side-p.y)<.52&&canMove(p.x+dx,p.z,.36,side)){p.x+=dx;p.y=side;}
        const ahead=groundHeight(p.x,p.z+dz);
        if(Math.abs(ahead-p.y)<.52&&canMove(p.x,p.z+dz,.36,ahead)){p.z+=dz;p.y=ahead;}
        return;
    }
    const height=crouched||slideTime>0?1.18:1.8;
    const grounded=Math.abs(verticalSpeed)<.04&&p.y-floorUnderPlayer()<.04;
    const stepHeight=grounded?(slideTime>0?.16:.255):0;
    const steps=Math.max(1,Math.ceil(Math.hypot(dx,dz)/.17));
    for(let i=0;i<steps;i++){
        if(dx)stepTo(collision,boundary,p,p.x+dx/steps,p.z,height,stepHeight);
        if(dz)stepTo(collision,boundary,p,p.x,p.z+dz/steps,height,stepHeight);
    }
}

function updatePlayer(dt) {
    if(dt<=0)return;
    const x=Number(keys.has('KeyD'))-Number(keys.has('KeyA'))+stick.x;
    const z=Number(keys.has('KeyS'))-Number(keys.has('KeyW'))+stick.y;
    const sprinting=!crouched&&!input.aim&&!reload&&z<-.36&&-z>Math.abs(x)*.85&&
        (keys.has('ShiftLeft')||running||stick.length()>.92);
    moveInput.set(x,z).clampLength(0,1);
    const pace=crouched?2.6:input.aim?3.3:sprinting?7.1:4.8;
    const multiplier=1-Math.max(0,moveInput.y)*.26-Math.abs(moveInput.x)*.12;
    const speed=pace*multiplier*weapon.mobility;
    const position=player.root.position;
    if(!vault&&(position.y>groundHeight(position.x,position.z)+.006||verticalSpeed!==0)) {
        const height=crouched?1.18:1.8;
        const floor=floorUnderPlayer();
        const before=verticalSpeed;
        verticalSpeed-=15.8*dt;
        const next=position.y+(before+verticalSpeed)*.5*dt;
        if(verticalSpeed<=0&&next<=floor){position.y=floor;verticalSpeed=0;}
        else if(canMove(position.x,position.z,.36,next,height))position.y=next;
        else verticalSpeed=0;
    }
    const sine=Math.sin(input.yaw),cosine=Math.cos(input.yaw);
    const sliding=slideTime>0;
    const vaulting=Boolean(vault);
    const beforeX=position.x,beforeZ=position.z;
    if(vault){
        vault.elapsed+=dt;
        const progress=Math.min(1,vault.elapsed/.61);
        const next=vaultPoint(vault,progress,{});
        if(canMove(next.x,next.z,.36,next.y))position.set(next.x,next.y,next.z);
        else vault=null;
        if(progress>=1)vault=null;
    }else if(sliding){
        const slideSpeed=4+12*slideTime/.56;
        move(player,slideDirection.x*slideSpeed,slideDirection.y*slideSpeed,dt);
        slideTime=Math.hypot(position.x-beforeX,position.z-beforeZ)<slideSpeed*dt*.32
            ?0:Math.max(0,slideTime-dt);
        if(slideTime===0&&!canMove(position.x,position.z,.36,position.y))crouched=true;
    }else{
        const desiredX=(moveInput.x*cosine-moveInput.y*sine)*speed;
        const desiredZ=(moveInput.x*sine+moveInput.y*cosine)*speed;
        const response=moveInput.lengthSq()>.001?16:23;
        const blend=1-Math.exp(-response*dt);
        groundVelocity.x=T.MathUtils.lerp(groundVelocity.x,desiredX,blend);
        groundVelocity.y=T.MathUtils.lerp(groundVelocity.y,desiredZ,blend);
        if(groundVelocity.lengthSq()<.012)groundVelocity.set(0,0);
        move(player,groundVelocity.x,groundVelocity.y,dt);
    }
    player.root.rotation.y=Math.PI-input.yaw;
    const actualX=(position.x-beforeX)/dt,actualZ=(position.z-beforeZ)/dt;
    localDirection.set(actualX*cosine+actualZ*sine,actualZ*cosine-actualX*sine);
    const actualSpeed=localDirection.length();
    const moving=actualSpeed>.32;
    const shooting=elapsed<player.shot;
    const aimed=input.aim||shooting;
    const sprintPose=sprinting&&!shooting&&actualSpeed>5.1*weapon.mobility;
    sprintBlend=T.MathUtils.lerp(sprintBlend,sprintPose?1:0,1-Math.exp(-11*dt));
    if(vaulting||sliding||Math.abs(verticalSpeed)>.04){
        pose(player,sliding?'slide_loop':'jump_loop');
        if(vaulting||sliding)groundVelocity.set(0,0);
    }else if(shooting&&['melee','grenade'].includes(weapon.type)){
        pose(player,weapon.type==='melee'?'melee':'throw');
    }else if(reload){
        pose(player,'reload');
    }else if(moving){
        const stance=aimed?'aim':'ready';
        const direction=Math.abs(localDirection.x)>Math.abs(localDirection.y)*.82
            ?(localDirection.x<0?'_left':'_right'):localDirection.y>0?'_back':'';
        const gait=stance+(crouched?'_crouch_walk':'_walk');
        const clip=sprintPose?'ready_sprint':clips.has(gait+direction)?gait+direction:gait;
        const reference=crouched?2.2:sprintPose?6.4:aimed?3.25:4.2;
        pose(player,clip,clamp(actualSpeed/reference,.58,1.40));
    }else{
        const stance=aimed?'aim':'ready';
        pose(player,shooting&&!crouched?'shoot':stance+(crouched?'_crouch_idle':'_idle'));
    }
    if(reload&&elapsed>=reload) {
        const count=Math.min(weapon.capacity-ammo,reserve);
        ammo+=count;reserve-=count;reload=0;
    }
    updateCamera(1-Math.exp(-17*dt));
    if(state!=='playing')return;
    if(!vaulting&&(queuedShot||burstRemaining>0||(input.fire&&weapon.automatic))&&elapsed>=nextShot&&!reload){
        if(queuedShot&&weapon.burst)burstRemaining=weapon.burst;
        queuedShot=false;burstRemaining=Math.max(0,burstRemaining-1);
        fire();
    }
    nearby=null;
    let nearest=5.1;
    for(const item of loot) {
        const distance=item.mesh.position.distanceToSquared(position);
        item.mesh.visible=distance<42**2;
        if(!item.mesh.visible)continue;
        if(item.display)item.display.visible=distance<15**2;
        item.mesh.rotation.y+=dt*1.4;
        item.mesh.position.y=item.floor+.34+Math.sin(elapsed*3+item.mesh.position.x)*.075;
        if(Math.abs(item.floor-position.y)>.9)continue;
        if(item.kind==='kit'&&kits>=5)continue;
        if(Object.hasOwn(throwables,item.kind)&&grenades[item.kind]>=3)continue;
        if(item.kind==='ammo'&&(!weapon.capacity||reserve>=weapon.capacity*6))continue;
        if(distance>=nearest)continue;
        origin.copy(position);origin.y+=.72;
        const toItem=scratch.copy(item.mesh.position).sub(origin);
        ray.far=toItem.length();
        ray.set(origin,toItem.normalize());
        if(collision.rayDistance(ray.ray.origin,ray.ray.direction,ray.far)<ray.far)continue;
        nearest=distance;nearby=item;
    }
}

function requestShot(){
    input.fire=true;
    if(!reload||reload-elapsed<=.28)queuedShot=true;
}

function updateCamera(weight) {
    const yaw=input.yaw,pitch=input.pitch+aimPunch;
    forward.set(Math.sin(yaw)*Math.cos(pitch),Math.sin(pitch),-Math.cos(yaw)*Math.cos(pitch));
    shoulder.set(Math.cos(yaw),0,Math.sin(yaw));
    aimBlend=T.MathUtils.lerp(aimBlend,input.aim?1:0,weight);
    cameraAnchor.copy(player.root.position);
    cameraAnchor.y+=slideTime>0?.81:crouched?1.09:T.MathUtils.lerp(1.64,1.46,aimBlend);
    const offset=T.MathUtils.lerp(.89,.70,aimBlend);
    ray.set(cameraAnchor,shoulder);ray.far=offset;
    const side=firstWorldImpact();
    origin.copy(cameraAnchor).addScaledVector(shoulder,side<offset?Math.max(0,side-.15):offset);
    const distance=T.MathUtils.lerp(3.46+sprintBlend*.41,1.94,aimBlend);
    idealCamera.copy(origin).addScaledVector(forward,-distance);
    ray.set(origin,scratch.copy(idealCamera).sub(origin).normalize());
    ray.far=origin.distanceTo(idealCamera);
    const obstruction=firstWorldImpact();
    if(obstruction<ray.far)ray.ray.at(Math.max(0,obstruction-.24),idealCamera);
    camera.position.lerp(idealCamera,weight);
    scratch.copy(camera.position).sub(origin);
    ray.far=scratch.length();
    if(ray.far>.001){
        ray.set(origin,scratch.normalize());
        const blocked=firstWorldImpact();
        if(blocked<ray.far)ray.ray.at(Math.max(0,blocked-.17),camera.position);
    }
    camera.lookAt(scratch.copy(camera.position).add(forward));
    const targetFov=T.MathUtils.lerp(59+4*sprintBlend,weapon.type==='sniper'?37:46,aimBlend);
    const fov=T.MathUtils.lerp(camera.fov,targetFov,weight);
    if(Math.abs(fov-camera.fov)>.015){
        camera.fov=fov;
        camera.updateProjectionMatrix();
    }
}

function shotMuzzle(){
    const muzzle=muzzlePoint.copy(player.root.position).addScaledVector(shoulder,.21);
    muzzle.y+=slideTime>0?.82:crouched?1.01:1.34;
    return muzzle;
}

function coverBlocksAim(){
    if(!input.aim||zipRide||!weapon.capacity||weapon.utility==='wall')return false;
    camera.updateMatrixWorld();
    ray.setFromCamera(shotOffset.set(0,0),camera);
    ray.far=9;
    const sight=firstWorldImpact();
    if(sight<3.5)return true;
    ray.ray.at(sight,shotImpact);
    const muzzle=shotMuzzle();
    const direction=scratch.copy(shotImpact).sub(muzzle);
    ray.far=direction.length();
    ray.set(muzzle,direction.normalize());
    return firstWorldImpact()+.13<ray.far;
}

function fire() {
    if(weapon.capacity&&!ammo){burstRemaining=0;beginReload();return;}
    if(weapon.utility==='wall'&&!deployWall())return;
    if(weapon.capacity)ammo--;
    nextShot=elapsed+weapon.interval+(weapon.burst&&!burstRemaining?.22:0);
    player.shot=elapsed+.17;
    aimPunch=Math.min(.095,aimPunch+weapon.recoil);
    playWeaponAudio?.(weapon);
    if(weapon.utility==='wall')return;
    if(weapon.aid){player.hp=Math.min(100,player.hp+(weapon.capacity>20?6:12));return;}
    const moving=localDirection.lengthSq()>1;
    const spread=weapon.spread*(input.aim?.49:crouched?.76:1)*(moving?1.26:1)*(1+shotHeat*.46);
    if(weapon.automatic||weapon.burst)shotHeat=Math.min(1,shotHeat+.14);
    camera.updateMatrixWorld();
    const muzzle=shotMuzzle();
    let maxHit=0;
    for(let pellet=0;pellet<weapon.pellets;pellet++){
        ray.setFromCamera(shotOffset.set((Math.random()-.5)*spread,(Math.random()-.5)*spread),camera);
        ray.far=weapon.range+5;
        const impact=ray.ray.at(firstWorldImpact(),shotImpact);
        scratch.copy(impact).sub(muzzle);
        const distance=Math.min(weapon.range,scratch.length()+.04);
        ray.set(muzzle,scratch.normalize());
        ray.far=distance;
        let nearest=firstWorldImpact();
        let target=null,head=false;
        for(const bot of bots){
            if(bot.hp<=0)continue;
            shotSphere.center.copy(bot.root.position);
            shotSphere.center.y+=1.60;shotSphere.radius=.24;
            const upper=ray.ray.intersectSphere(shotSphere,shotHit)?.distanceTo(muzzle)??Infinity;
            shotSphere.center.y-=.66;shotSphere.radius=.53;
            const trunk=ray.ray.intersectSphere(shotSphere,shotHit)?.distanceTo(muzzle)??Infinity;
            const d=Math.min(upper,trunk);
            if(d<nearest){nearest=d;target=bot;head=upper<trunk;}
        }
        ray.ray.at(nearest,impact);
        if(weapon.utility&&weapon.utility!=='wall'){
            deployEffect(weapon.utility,impact,weapon.blast||6);
        }
        if(weapon.blast){
            const blast=impact.clone().addScaledVector(ray.ray.direction,-.12);
            if(blastDamage(blast,weapon.blast,weapon.damage,.68,false))maxHit=1;
        }else if(target){
            const falloff=Math.max(.42,1-Math.max(0,nearest/weapon.range-.48));
            hitBot(target,weapon.damage*falloff*(head?2.3:1),head);maxHit=1;
        }
        if(weapon.type!=='melee'&&pellet<3)tracer(muzzle,impact,weapon.blast?0xff8255:0xffdb82);
    }
    if(maxHit)hitUntil=elapsed+.16;
}

function blastDamage(point,range,damageAmount,falloff,includePlayer){
    let hit=false;
    for(const actor of includePlayer?[player,...bots]:bots){
        if(actor.hp<=0)continue;
        scratch.copy(actor.root.position).y+=.75;
        scratch.sub(point);
        const distance=scratch.length();
        if(distance>=range)continue;
        if(distance>.001){
            ray.set(point,scratch.multiplyScalar(1/distance));ray.far=distance;
            if(firstWorldImpact()<distance-.025)continue;
        }
        const points=damageAmount*(1-distance/range*falloff);
        if(actor===player)damage(actor,points);
        else{hitBot(actor,points,false);hit=true;}
    }
    return hit;
}

function deployWall() {
    const pos=player.root.position;
    const x=pos.x+Math.sin(input.yaw)*4.2,z=pos.z-Math.cos(input.yaw)*4.2;
    const horizontal=Math.abs(Math.cos(input.yaw))>Math.abs(Math.sin(input.yaw));
    const w=horizontal?4.1:.42,d=horizontal?.42:4.1;
    if(![-1,0,1].every(i=>canMove(x+(horizontal?i*1.7:0),z+(horizontal?0:i*1.7),.23,groundHeight(x+(horizontal?i*1.7:0),z+(horizontal?0:i*1.7)))))return false;
    const floor=groundHeight(x,z);
    const mesh=new T.Mesh(wallGeometry,utilityMaterials.wall);
    mesh.position.set(x,floor+1.12,z);mesh.scale.set(w,2.24,d);
    mesh.updateMatrixWorld(true);scene.add(mesh);
    const obstacle={x,z,w,d,bottom:floor,top:floor+2.24};
    arena.obstacles.push(obstacle);collision.add(obstacle);
    deployments.push({mesh,obstacle,type:'wall',until:elapsed+19});
    return true;
}
function deployEffect(type,impact,range) {
    if(type==='melter'){
        for(const entry of deployments.slice())if(entry.type==='wall'&&
            Math.hypot(entry.mesh.position.x-impact.x,entry.mesh.position.z-impact.z)<range)removeDeployment(entry);
        return;
    }
    const mesh=new T.Mesh(smokeGeometry,utilityMaterials[type]);
    const ground=groundHeight(impact.x,impact.z);
    const height=type==='smoke'?2.7:type==='flash'?range*.7:.29;
    mesh.scale.set(range,height,range);
    mesh.position.set(impact.x,type==='smoke'?Math.max(ground,impact.y)+1.35:type==='flash'?impact.y+.4:ground+.15,impact.z);
    scene.add(mesh);
    const duration=type==='smoke'?8:type==='flash'?.23:5;
    deployments.push({mesh,type,range,until:elapsed+duration});
}
function removeDeployment(entry) {
    scene.remove(entry.mesh);
    if(entry.obstacle){
        arena.obstacles.splice(arena.obstacles.indexOf(entry.obstacle),1);
        collision.remove(entry.obstacle);
    }
    deployments.splice(deployments.indexOf(entry),1);
}
function clearDeployments() {
    while(deployments.length)removeDeployment(deployments.at(-1));
}
function inEffect(type,position) {
    return deployments.some(entry=>entry.type===type&&
        Math.hypot(entry.mesh.position.x-position.x,entry.mesh.position.z-position.z)<entry.range);
}
function smokeObstructs(from,to) {
    const dx=to.x-from.x,dz=to.z-from.z;
    return deployments.some(entry=>{
        if(entry.type!=='smoke')return false;
        const p=entry.mesh.position;
        const t=clamp(((p.x-from.x)*dx+(p.z-from.z)*dz)/(dx*dx+dz*dz||1),0,1);
        const y=from.y+(to.y-from.y)*t;
        return Math.abs(y-p.y)<2.7&&Math.hypot(from.x+dx*t-p.x,from.z+dz*t-p.z)<entry.range;
    });
}

function hitBot(target,points,head) {
    if(head)headUntil=elapsed+.16;
    if(damage(target,points)){
        kills++;
        placeLoot(target.root.position.x,target.root.position.z,kills%3===0?'kit':'ammo',target.root.position.y);
    }
}

function switchWeapon(slot) {
    if(!inventory[slot])return;
    if(slot!==equipped&&inventory[equipped])Object.assign(inventory[equipped],{ammo,reserve});
    equipped=slot;
    ({profile:weapon,ammo,reserve}=inventory[slot]);
    reload=0;burstRemaining=0;queuedShot=input.fire;aimPunch=0;shotHeat=0;
    player.root.remove(player.gun);
    player.gun=weaponModel(weapon,rifle,collection?.skin());
    player.weapon=weapon;
    player.root.add(player.gun);
    nextShot=elapsed+.16;
    $('weapon-name').textContent=weapon.name;
    $('weapon-swap').textContent='Trocar · '+inventory[1-slot].profile.name;
}
function tracer(from,to,color) {
    const beam=beams.find(item=>!item.line.visible)||beams[0];
    const attr=beam.line.geometry.attributes.position;
    attr.setXYZ(0,from.x,from.y,from.z);attr.setXYZ(1,to.x,to.y,to.z);
    attr.needsUpdate=true;
    beam.line.material.color.setHex(color);
    beam.line.visible=true;beam.until=elapsed+.065;
}

function damage(actor,points,source='combat') {
    if(actor.hp<=0)return false;
    if(actor===player&&source==='combat'){
        if(weapon.shield&&input.aim)points*=.6;
        points=defendWithAbility(ability,elapsed,points);
    }
    if(points<=0)return false;
    actor.hp=Math.max(0,actor.hp-points);
    if(actor===player)damageFlash=.63;
    if(actor.hp>0)return false;
    pose(actor,'death');
    actor.gun.visible=false;
    return true;
}

function planBot(bot,center,position){
    bot.target=player;
    let best=position.distanceToSquared(center);
    for(const other of bots){
        if(other===bot||other.hp<=0)continue;
        const distance=position.distanceToSquared(other.root.position)*1.26;
        if(distance<best){bot.target=other;best=distance;}
    }
    const target=bot.target.root.position;
    const dx=target.x-position.x,dz=target.z-position.z;
    const length=Math.hypot(dx,dz)||.001;
    const fromCenter=Math.hypot(position.x,position.z);
    let gx=dx/length,gz=dz/length;
    if(fromCenter>=radius){
        gx=-position.x/Math.max(1,fromCenter);
        gz=-position.z/Math.max(1,fromCenter);
    }else if(length<17){
        gx=dz/length*bot.strafe;gz=-dx/length*bot.strafe;
    }
    const free=(x,z)=>canMove(x,z,.36,groundHeight(x,z));
    if(!free(position.x+gx*.52,position.z+gz*.52)) {
        const sidestep=gz*bot.strafe;
        gz=-gx*bot.strafe;gx=sidestep;
        if(!free(position.x+gx*.52,position.z+gz*.52)){gx=-gx;gz=-gz;}
    }
    bot.gx=gx;bot.gz=gz;
    bot.facing=Math.atan2(dx,dz);
    bot.nextPlan=elapsed+bot.planInterval;
}

function stepBots(dt) {
    const center = player.root.position;
    for(const bot of bots) {
        if(bot.hp<=0)continue;
        const p=bot.root.position;
        if(!bot.target||bot.target.hp<=0||elapsed>=bot.nextPlan)planBot(bot,center,p);
        const target=bot.target;
        const d=target.root.position;
        const speed=inEffect('freeze',p)?1.25:3.05;
        move(bot,bot.gx*speed,bot.gz*speed,dt);
        if(inEffect('fire',p)){hitBot(bot,8*dt,false);if(bot.hp<=0)continue;}
        const turn=bot.facing-bot.root.rotation.y;
        bot.root.rotation.y+=Math.atan2(Math.sin(turn),Math.cos(turn))*Math.min(1,dt*14);
        if(elapsed<bot.nextFire||elapsed<4||(d.x-p.x)**2+(d.z-p.z)**2>29**2)continue;
        bot.nextFire=elapsed+.9+Math.random()*1.0;
        const from=p.clone().add(new T.Vector3(0,1.32,0));
        const to=d.clone().add(new T.Vector3(0,target===player&&crouched?.85:1.1,0));
        ray.set(from,scratch.copy(to).sub(from).normalize());
        ray.far=from.distanceTo(to);
        if(firstWorldImpact()<ray.far||smokeObstructs(from,to))continue;
        bot.shot=elapsed+.18;
        tracer(from,to,0xff8d54);
        if(Math.random()<(target===player?.34:.54))damage(target,target===player?8:19);
    }
}

function collect() {
    if(state!=='playing'||zipRide||!nearby)return;
    const item=nearby;
    const p=player.root.position;
    if(Object.hasOwn(throwables,item.kind)){
        grenades[item.kind]=Math.min(3,grenades[item.kind]+1);
        if(!grenades[selectedThrow])selectedThrow=item.kind;
    }else if(item.kind==='kit')kits=Math.min(5,kits+1);
    else if(item.kind==='ammo')reserve=Math.min(weapon.capacity*6,reserve+Math.max(2,Math.round(weapon.capacity*.8)));
    else{
        const old={profile:weapon,ammo,reserve};
        if(old.profile===item.profile) {
            reserve=Math.min(weapon.capacity*6,reserve+(item.stored?.ammo??weapon.capacity)+(item.stored?.reserve??0));
        }else{
            inventory[equipped]=item.stored??{profile:item.profile,ammo:item.profile.capacity,reserve:item.profile.capacity};
            switchWeapon(equipped);
            const px=canMove(p.x+.36,p.z,.2,p.y,.6)?p.x+.36:p.x;
            const floor=floorAt(collision,px,p.z,p.y+.035,.2);
            placeLoot(px,p.z,'weapon',floor,old.profile,old);
        }
    }
    scene.remove(item.mesh);
    loot.splice(loot.indexOf(item),1);
    nearby=null;
    updateHud();
}
function heal(){if(state==='playing'&&!zipRide&&kits&&player.hp<100){kits--;player.hp=Math.min(100,player.hp+40);updateHud();}}
function beginReload(){if(state==='playing'&&!zipRide&&reserve&&ammo<weapon.capacity&&!reload)reload=elapsed+weapon.reload;}
function end(win) {
    state='result';document.body.dataset.state=state;input.fire=false;
    if(document.pointerLockElement)document.exitPointerLock();
    $('hud').hidden=true;$('result').hidden=false;
    resetTraversal();
    $('outcome').textContent=win?'Vitória':'Eliminado';
    const credits=collection?.award(win,kills,elapsed)??0;
    $('stats').textContent=`${kills} ${kills===1?'eliminação':'eliminações'} · ${Math.floor(elapsed)} segundos · +${credits} créditos`;
}
function tick(dt) {
    elapsed+=dt;
    const p=player.root.position;
    const recovery=healingWithAbility(ability,operator,elapsed,dt,Math.hypot(p.x,p.z)<=radius);
    if(recovery>0)player.hp=Math.min(100,player.hp+recovery);
    updateAbilityEffect();
    for(const entry of deployments.slice())if(elapsed>=entry.until)removeDeployment(entry);
    aimPunch*=Math.exp(-12*dt);damageFlash*=Math.exp(-7*dt);
    shotHeat=Math.max(0,shotHeat-.62*dt);
    radius=zoneAt(elapsed);
    updateZoneRing();
    if(zipRide)stepZipline(dt);
    else updatePlayer(dt);
    nearZip=!zipRide&&Math.abs(verticalSpeed)<.1&&!vault&&slideTime<=0?nearestZipline(ziplines,p):null;
    stepBots(dt);
    stepThrows(dt);
    for(const actor of [player,...bots]) {
        if(actor.hp>0&&Math.hypot(actor.root.position.x,actor.root.position.z)>radius+.25)damage(actor,dt*13,'zone');
    }
    for(const beam of beams)if(elapsed>=beam.until)beam.line.visible=false;
    if (elapsed>=nextHud) {updateHud();nextHud=elapsed+.08;}
    if(player.hp<=0||elapsed>=totalTime)end(false);
    else if(bots.every(bot=>bot.hp<=0))end(true);
}
function updateHud() {
    $('alive').textContent=`${1+bots.filter(b=>b.hp>0).length} vivos`;
    const remaining=Math.max(0,Math.ceil(totalTime-elapsed));
    $('clock').textContent=`${Math.floor(remaining/60)}:${String(remaining%60).padStart(2,'0')}`;
    $('kills').textContent=`${kills} ELIM`;
    $('health').textContent=`${Math.ceil(player.hp)} / 100`;$('hp').value=player.hp;
    $('kits').textContent=`Kits médicos · ${kits}`;
    $('mag').textContent=weapon.capacity?ammo:'—';$('reserve').textContent=weapon.capacity?`/ ${reserve}`:'';
    $('action').textContent=reload?'Recarregando…':'';
    const active=elapsed<ability.until;
    const cooling=Math.max(0,Math.ceil(ability.readyAt-elapsed));
    $('ability').disabled=cooling>0;
    $('throw').disabled=grenades[selectedThrow]<=0;
    $('throw-count').textContent=grenades[selectedThrow];
    $('throw-type').textContent=throwables[selectedThrow].label+' · '+grenades[selectedThrow];
    $('ability-timer').textContent=active?'Ativa':cooling?cooling+' s':'Pronta';
    $('skill-state').textContent=active
        ?operator.ability+(ability.shield>0?' · '+Math.ceil(ability.shield)+' escudo':'')
        :operator.ability+' · '+(cooling?cooling+' s':'Pronta');
    const crosshair=$('crosshair');
    crosshair.classList.toggle('hit',elapsed<hitUntil);
    crosshair.classList.toggle('head',elapsed<headUntil);
    const blocked=coverBlocksAim();
    crosshair.classList.toggle('blocked',blocked);
    $('cover-hint').hidden=!blocked;
    const crossSize=Math.round((input.aim?19:28)+shotHeat*11)+'px';
    crosshair.style.width=crosshair.style.height=crossSize;
    $('aim').setAttribute('aria-pressed',input.aim);
    $('crouch').setAttribute('aria-pressed',crouched);
    $('sprint').setAttribute('aria-pressed',running);
    $('loot').hidden=!nearby;
    $('zip-action').hidden=!zipRide&&!nearZip;
    if(zipRide||nearZip)$('zip-action').textContent=zipRide?'Soltar cabo':'Usar tirolesa';
    if(nearby)$('loot').textContent=Object.hasOwn(throwables,nearby.kind)?
        'Coletar '+throwables[nearby.kind].label+' · E':nearby.kind==='weapon'?
        (weapon===nearby.profile?'Recuperar munição':'Equipar '+nearby.profile.name)+' · E':
        nearby.kind==='ammo'?'Coletar munição · E':'Coletar kit · E';
    const p=player.root.position;
    $('zone-alert').textContent=Math.hypot(p.x,p.z)>radius?'Fora da zona segura':
        elapsed<zoneDelay?`Zona fecha em ${Math.ceil(zoneDelay-elapsed)} s`:'';
    $('flash').style.opacity=damageFlash;
    const district=districts.reduce((a,b)=>Math.hypot(p.x-a.x,p.z-a.z)<Math.hypot(p.x-b.x,p.z-b.z)?a:b);
    $('district').textContent=district.name;
    drawMap();
}
function updateZoneRing() {
    if(Math.abs(lastZoneDraw-radius)<.45)return;
    lastZoneDraw=radius;
    const attr=zoneRing.geometry.attributes.position;
    for(let i=0;i<attr.count;i++){
        const angle=i/attr.count*Math.PI*2;
        const x=Math.cos(angle)*radius,z=Math.sin(angle)*radius;
        attr.setXYZ(i,x,groundHeight(x,z)+.19,z);
    }
    attr.needsUpdate=true;
    zoneRing.geometry.computeBoundingSphere();
}
function firstWorldImpact() {
    const hit=collision.rayDistance(ray.ray.origin,ray.ray.direction,ray.far);
    const {origin:point,direction}=ray.ray;
    const yAt=t=>point.y+direction.y*t-groundHeight(point.x+direction.x*t,point.z+direction.z*t);
    let previous=0;
    const steps=Math.max(1,Math.ceil(hit/2.15));
    for(let i=1;i<=steps;i++){
        const t=i*hit/steps;
        if(yAt(t)<0){
            let lo=previous,hi=t;
            for(let j=0;j<8;j++){const mid=(lo+hi)*.5;if(yAt(mid)<0)hi=mid;else lo=mid;}
            return hi;
        }
        previous=t;
    }
    return hit;
}
function createTerrainMap() {
    const layer=document.createElement('canvas');
    layer.width=layer.height=144;
    const context=layer.getContext('2d');
    for(let z=0;z<144;z+=3)for(let x=0;x<144;x+=3){
        const y=groundHeight((x-72)*boundary/69,(z-72)*boundary/69);
        const light=Math.min(52,17+y*.76);
        context.fillStyle=`hsl(133 16% ${light}%)`;
        context.fillRect(x,z,3,3);
    }
    context.fillStyle='#68756b';
    context.fillRect(0,70,144,4);context.fillRect(72,0,4,144);
    context.fillStyle='#111c22';
    const scale=69/boundary;
    for(const w of arena.obstacles)if(w.w>4||w.d>4)
        context.fillRect(72+(w.x-w.w/2)*scale,72+(w.z-w.d/2)*scale,w.w*scale,w.d*scale);
    return layer;
}
function drawMap() {
    if(now<nextMapDraw)return;
    nextMapDraw=now+.115;
    const center=72,scale=69/boundary;
    map.clearRect(0,0,144,144);
    terrainMap??=createTerrainMap();
    map.drawImage(terrainMap,0,0);
    map.beginPath();map.arc(center,center,radius*scale,0,7);map.strokeStyle='#9deff2';map.lineWidth=1.2;map.stroke();
    map.strokeStyle='#e5ab59';map.fillStyle='#e5ab59';map.lineWidth=1.1;
    for(const {a,b} of ziplines){
        map.beginPath();map.moveTo(center+a.x*scale,center+a.z*scale);map.lineTo(center+b.x*scale,center+b.z*scale);map.stroke();
        for(const p of [a,b])map.fillRect(center+p.x*scale-1.3,center+p.z*scale-1.3,2.6,2.6);
    }
    if(state==='flight'||state==='descent'){
        map.save();map.setLineDash([3,4]);map.strokeStyle='#dae4de';
        map.beginPath();map.moveTo(center+flightFrom.x*scale,center+flightFrom.z*scale);
        map.lineTo(center+flightTo.x*scale,center+flightTo.z*scale);map.stroke();map.restore();
    }
    map.fillStyle='#ff8e78';
    const scan=operator.id==='vigia'&&elapsed<ability.until;
    if(scan){
        map.beginPath();
        map.arc(center+player.root.position.x*scale,center+player.root.position.z*scale,46*scale,0,7);
        map.strokeStyle='#61ecd6';map.lineWidth=.9;map.stroke();
    }
    for(const bot of bots)if(bot.hp>0&&(inEffect('sonar',bot.root.position)||
        (scan&&Math.hypot(bot.root.position.x-player.root.position.x,bot.root.position.z-player.root.position.z)<=46))){
        map.beginPath();
        map.arc(center+bot.root.position.x*scale,center+bot.root.position.z*scale,2.2,0,7);
        map.fill();
    }
    map.save();map.translate(center+player.root.position.x*scale,center+player.root.position.z*scale);
    map.rotate(state==='flight'?Math.atan2(airDirection.x,-airDirection.z):input.yaw);
    map.fillStyle='#e0ff79';map.beginPath();map.moveTo(0,-5);map.lineTo(4,4);map.lineTo(-4,4);map.fill();map.restore();
}
function resize() {
    const profile=activePreset??presets.medium;
    studioResolution=state==='lobby';
    const scale=studioResolution||!settings.adaptive?1:resolutionControl.scale;
    presentation.configure(innerWidth,innerHeight,devicePixelRatio||1,profile,studioResolution,scale);
    renderDirty=true;
    $('render-resolution').textContent='Cena 3D · '+presentation.width+' × '+presentation.height;
    camera.aspect=innerWidth/innerHeight;
    camera.updateProjectionMatrix();
}
function updateStudio(dt) {
    if(player.root.parent!==studio)studio.add(player.root);
    restoreGrip(player);
    pose(player,'idle');
    player.gun.visible=false;
    player.mix.update(dt);
    player.root.position.set(0,.103,0);
    player.root.rotation.y=.13+Math.sin(now*.49)*(previewOperator?.20:.16);
    updateAppearance(player);
    const portrait=camera.aspect<1;
    if(previewOperator&&inspecting){
        camera.position.set(.54,1.66,2.25);
        camera.lookAt(portrait?-.02:-.70,portrait?1.34:1.54,0);
        camera.fov=portrait?43:36;
    }else{
        const orbit=.18*Math.sin(now*.26);
        camera.position.set((portrait?.17:.55)+orbit,1.35,portrait?4.37:4.64);
        const focus=previewOperator?-1.12:0;
        camera.lookAt(portrait?0:focus,portrait?.56:1.07,0);
        camera.fov=portrait?45:34;
    }
    camera.updateProjectionMatrix();
}

function updateGroundShadow(){
    groundShadow.visible=false;
    if(activePreset.shadowSize||state==='flight'||state==='descent'||!player.root.visible)return;
    const {x,y,z}=player.root.position;
    const floor=floorAt(collision,x,z,y+.035);
    const gap=Math.max(0,y-floor);
    if(gap>=1.3)return;
    groundShadow.visible=true;
    groundShadow.position.set(x,floor+.028,z);
    contactNormal.set(0,.36,0);
    if(floor<=groundHeight(x,z)+.04){
        contactNormal.x=groundHeight(x-.18,z)-groundHeight(x+.18,z);
        contactNormal.z=groundHeight(x,z-.18)-groundHeight(x,z+.18);
    }
    groundShadow.quaternion.setFromUnitVectors(planeNormal,contactNormal.normalize());
    groundShadow.material.opacity=.76*(1-gap/1.3);
}

function frame(time) {
    const wallTime=Math.max(0,(time-last)/1000),dt=Math.min(.1,wallTime);
    last=time;now=time/1000;
    if((state==='lobby')!==studioResolution){resolutionControl.reset();resize();}
    if(state==='playing'){
        const budget=Math.min(.24,wallTime),steps=Math.ceil(budget/.08);
        for(let i=0;i<steps&&state==='playing';i++)tick(budget/steps);
    }
    else if(deploymentStages.has(state))stepDeployment(dt,Math.min(1,wallTime));
    const stance=vault?'vaulting':slideTime>0?'sliding':crouched?'crouched':Math.abs(verticalSpeed)>.04?'airborne':'standing';
    if(document.body.dataset.stance!==stance)document.body.dataset.stance=stance;
    if(state==='lobby')updateStudio(dt);
    if(state==='result'){player.mix.update(dt);updateAppearance(player);for(const bot of bots)if(bot.hp<=0&&bot.root.visible)bot.mix.update(dt);}
    if(renderer.shadowMap.enabled&&now>=nextShadow&&state!=='paused'&&state!=='lobby'&&state!=='entry'){
        lighting.focus(player.root.position);
        renderer.shadowMap.needsUpdate=true;
        const pressure=settings.adaptive&&resolutionControl.scale<.88?2:1;
        nextShadow=now+activePreset.shadowInterval*pressure;
    }
    const interval=1000/(settings?.fps||30);
    const adjust=settings.adaptive&&(state==='playing'||deploymentStages.has(state));
    if(!adjust)resolutionControl.idle();
    if(state!=='entry'&&(state!=='paused'||renderDirty)&&time-lastFrame>=interval-1){
        if(adjust&&resolutionControl.sample(time,interval,activePreset.minScale))resize();
        if(state==='lobby')presentation.render(studio,camera);
        else{
            if(state==='playing'||deploymentStages.has(state)){
                if(state!=='flight')updateBotView(state==='waiting');
                animateActors(Math.min(.24,(time-lastVisualTime)/1000));
            }
            arena.updateView(camera.position,player.root.position,now,activePreset);
            updateGroundShadow();
            presentation.render(scene,camera);
        }
        renderDirty=false;
        lastVisualTime=time;
        lastFrame=Math.max(lastFrame+interval,time-interval+1);
    }
    requestAnimationFrame(frame);
}

function initAudio() {
    if(audio){audio.resume().catch(()=>{});return;}
    try {
        audio=new AudioContext();
        playWeaponAudio=createWeaponAudio(audio,()=>pickup);
        fetch(hitSound).then(r=>r.arrayBuffer()).then(data=>audio.decodeAudioData(data))
            .then(buffer=>{pickup=buffer;}).catch(()=>{});
    } catch { /* The game remains playable with muted browser audio. */ }
}
$('deploy').addEventListener('click',start);
$('again').addEventListener('click',start);
$('reload').addEventListener('click',beginReload);
$('heal').addEventListener('click',heal);
$('loot').addEventListener('click',collect);
function clearInput() {
    keys.clear();stick.set(0,0);
    groundVelocity.set(0,0);localDirection.set(0,0);sprintBlend=0;
    input.fire=false;input.aim=false;queuedShot=false;burstRemaining=0;running=false;
    movePointer=null;stickBounds=null;lookPointer=null;firePointer=null;
    $('stick').firstElementChild.style.transform='';
}
function floorUnderPlayer() {
    const p=player.root.position;
    return floorAt(collision,p.x,p.z,p.y+.035);
}
function jump() {
    if(state==='flight'){leaveAircraft();return;}
    if(state==='descent'){openParachute();return;}
    if(zipRide&&state==='playing'){finishZipline();return;}
    if(!['waiting','playing'].includes(state)||vault||slideTime>0||player.root.position.y-floorUnderPlayer()>.035||verticalSpeed>0)return;
    const p=player.root.position;
    if(!canMove(p.x,p.z,.36,p.y+.04))return;
    const x=Number(keys.has('KeyD'))-Number(keys.has('KeyA'))+stick.x;
    const z=Number(keys.has('KeyS'))-Number(keys.has('KeyW'))+stick.y;
    const forwardMotion=Math.hypot(x,z)<.2;
    const dx=forwardMotion?Math.sin(input.yaw):x*Math.cos(input.yaw)-z*Math.sin(input.yaw);
    const dz=forwardMotion?-Math.cos(input.yaw):x*Math.sin(input.yaw)+z*Math.cos(input.yaw);
    const length=Math.hypot(dx,dz);
    vault=findVault(collision,boundary,p,dx/length,dz/length);
    crouched=false;
    if(vault){queuedShot=false;burstRemaining=0;}
    else verticalSpeed=5.7;
}
function toggleCrouch() {
    if(state!=='playing'||zipRide||vault||Math.abs(verticalSpeed)>.04||slideTime>0)return;
    const pressed=performance.now();
    if(pressed-lastCrouch<320){
        lastCrouch=-Infinity;
        slideTime=.56;
        const x=Number(keys.has('KeyD'))-Number(keys.has('KeyA'))+stick.x;
        const z=Number(keys.has('KeyS'))-Number(keys.has('KeyW'))+stick.y;
        if(Math.hypot(x,z)<.2)slideDirection.set(Math.sin(input.yaw),-Math.cos(input.yaw));
        else slideDirection.set(x*Math.cos(input.yaw)-z*Math.sin(input.yaw),
            x*Math.sin(input.yaw)+z*Math.cos(input.yaw)).normalize();
        crouched=false;
        return;
    }
    lastCrouch=pressed;
    if(!crouched||canMove(player.root.position.x,player.root.position.z,.36,player.root.position.y))crouched=!crouched;
}
function showLobby() {
    clearDeployments();
    clearThrows();
    resetTraversal();
    abilityRing.visible=false;
    clearInput();
    clearBots();
    for(const item of loot.splice(0))scene.remove(item.mesh);
    for(const beam of beams)beam.line.visible=false;
    zoneRing.visible=false;
    player.root.visible=true;
    slideTime=0;vault=null;crouched=false;verticalSpeed=0;player.gun.visible=true;pose(player,'ready_idle');
    $('flash').style.opacity=0;$('hud').hidden=true;$('lobby').hidden=false;
    state='lobby';document.body.dataset.state=state;
}
settings=setupSettings({
    change(value) {
        if(!presentation)return;
        const preference=[value.quality,value.fps,value.adaptive].join('|');
        if(renderPreference===preference)return;
        resolutionControl.reset();renderPreference=preference;
        activePreset=presets[value.quality];
        renderer.shadowMap.enabled=Boolean(activePreset.shadowSize);
        renderer.shadowMap.needsUpdate=true;
        nextShadow=0;
        if(arena)applyDetail();
        resize();
    },
    pause() {
        if(state!=='playing'&&!deploymentStages.has(state))return;
        pausedState=state;
        clearInput();state='paused';document.body.dataset.state=state;
        if(document.pointerLockElement)document.exitPointerLock();
    },
    resume() {if(state==='paused'){state=pausedState;document.body.dataset.state=state;}},
    lobby:showLobby,
});
$('jump').onclick=jump;
$('deployment-action').onclick=jump;
$('zip-action').onclick=useZipline;
$('crouch').onclick=toggleCrouch;
$('sprint').onclick=()=>{if(state==='playing')running=!running;};
$('aim').addEventListener('click',()=>{if(settings.aimMode==='toggle'&&state==='playing'&&!zipRide)input.aim=!input.aim;});
$('aim').addEventListener('pointerdown',e=>{
    if(settings.aimMode==='hold'&&state==='playing'&&!zipRide){
        input.aim=true;$('aim').setPointerCapture(e.pointerId);
    }
});
for(const type of ['pointerup','pointercancel','lostpointercapture'])
    $('aim').addEventListener(type,()=>{if(settings.aimMode==='hold')input.aim=false;});
addEventListener('keydown',e=>{
    if(e.code==='Escape'&&!e.repeat){
        if(state==='playing'||deploymentStages.has(state))$('pause').click();
        else if(state==='paused'&&!$('settings').hidden)$('settings-close').click();
        return;
    }
    if(state!=='playing'&&!deploymentStages.has(state))return;
    keys.add(e.code);
    if(e.code==='Space')e.preventDefault();
    if(e.code==='Space'&&!e.repeat){jump();return;}
    if(state!=='playing')return;
    if(!e.repeat) {
        if(e.code==='KeyR')beginReload();
        if(e.code==='KeyC')toggleCrouch();
        if(e.code==='KeyQ'&&!zipRide)switchWeapon(1-equipped);
        if(e.code==='KeyX')useZipline();
        if(e.code==='KeyE')collect();
        if(e.code==='KeyH')heal();
        if(e.code==='KeyF')useAbility();
        if(e.code==='KeyG')throwItem();
        if(e.code==='KeyT')cycleThrow();
    }
});
addEventListener('keyup',e=>keys.delete(e.code));
addEventListener('blur',clearInput);
function look(dx,dy) {
    const speed=input.aim?.00145*settings.adsSensitivity:.00265*settings.sensitivity;
    input.yaw+=dx*speed;
    input.pitch=clamp(input.pitch-dy*speed,-.62,.53);
}
canvas.addEventListener('contextmenu',e=>e.preventDefault());
canvas.addEventListener('pointerdown',e=>{
    if(!['waiting','descent','playing'].includes(state))return;
    const shooting=state==='playing'&&!zipRide;
    if(e.pointerType==='mouse'){
        if(e.button===0){
            if(shooting)requestShot();
            try{canvas.requestPointerLock?.()?.catch(()=>{});}catch{}
        }
        if(e.button===2&&shooting)input.aim=true;
    }else if(lookPointer===null){lookPointer=e.pointerId;lookX=e.clientX;lookY=e.clientY;canvas.setPointerCapture(e.pointerId);}
});
document.addEventListener('pointermove',e=>{
    if(!['waiting','descent','playing'].includes(state))return;
    if(document.pointerLockElement===canvas)look(e.movementX,e.movementY);
    else if(e.pointerId===lookPointer){
        look(e.clientX-lookX,e.clientY-lookY);lookX=e.clientX;lookY=e.clientY;
    }
});
function release(e) {
    if(e.pointerId===lookPointer)lookPointer=null;
    if(e.pointerType==='mouse'){
        if(e.button===0)input.fire=false;
        if(e.button===2)input.aim=false;
    }
}
document.addEventListener('pointerup',release);
document.addEventListener('mouseup',e=>{
    if(e.button===0)input.fire=false;
    if(e.button===2)input.aim=false;
});
document.addEventListener('pointerlockchange',()=>{
    if(!document.pointerLockElement){input.fire=false;input.aim=false;}
});
document.addEventListener('pointercancel',release);
const joystick=$('stick');
function moveStick(e) {
    const r=stickBounds;
    stick.set((e.clientX-r.left-r.width*.5)/(r.width*.33),(e.clientY-r.top-r.height*.5)/(r.height*.33));
    const length=stick.length();
    const strength=clamp((length-.12)/.88,0,1);
    stick.multiplyScalar(length?strength/length:0);
    joystick.firstElementChild.style.transform=`translate(${stick.x*r.width*.29}px,${stick.y*r.height*.29}px)`;
}
joystick.addEventListener('pointerdown',e=>{
    if(movePointer!==null)return;
    movePointer=e.pointerId;stickBounds=joystick.getBoundingClientRect();
    joystick.setPointerCapture(e.pointerId);moveStick(e);
});
joystick.addEventListener('pointermove',e=>{if(movePointer===e.pointerId)moveStick(e);});
for(const type of ['pointerup','pointercancel','lostpointercapture'])joystick.addEventListener(type,e=>{
    if(movePointer!==e.pointerId)return;
    movePointer=null;stickBounds=null;stick.set(0,0);joystick.firstElementChild.style.transform='';
});
const fireButton=$('fire');
fireButton.addEventListener('pointerdown',e=>{
    if(firePointer!==null||state!=='playing'||zipRide)return;
    firePointer=e.pointerId;fireX=e.clientX;fireY=e.clientY;
    fireButton.setPointerCapture(e.pointerId);requestShot();
});
fireButton.addEventListener('pointermove',e=>{
    if(firePointer!==e.pointerId||e.pointerType==='mouse')return;
    look(e.clientX-fireX,e.clientY-fireY);fireX=e.clientX;fireY=e.clientY;
});
for(const type of ['pointerup','pointercancel','lostpointercapture'])fireButton.addEventListener(type,e=>{
    if(firePointer===e.pointerId){firePointer=null;input.fire=false;}
});
function applyDetail() {
    const detail=activePreset.hd?hdSurfaces:null;
    const maxAnisotropy=renderer.capabilities.getMaxAnisotropy();
    arena.setDetail(activePreset,detail,maxAnisotropy);
    renderDirty=true;
    lighting.setDetail(activePreset);
    setFabricDepth(activePreset.hd?(activePreset===presets.ultra?.017:.011):0);
    const diffuse=detail?.stone??stoneColor;
    const normal=activePreset.normals?(detail?.stoneNormal??stoneDetail):null;
    if(rockSurface.map!==diffuse){rockSurface.map=diffuse;rockSurface.needsUpdate=true;}
    if(rockSurface.normalMap!==normal){rockSurface.normalMap=normal;rockSurface.needsUpdate=true;}
    if(!activePreset.hd&&hdSurfaces){
        for(const texture of Object.values(hdSurfaces))texture.dispose();
        hdSurfaces=null;
    }
    if(activePreset.hd&&!hdSurfaceRequest&&!hdSurfaces&&!hdSurfacesUnavailable){
        hdSurfaceRequest=loadHdSurfaces().then(textures=>{
            hdSurfaceRequest=null;
            if(activePreset.hd){hdSurfaces=textures;applyDetail();}
            else for(const texture of Object.values(textures))texture.dispose();
        }).catch(error=>{
            hdSurfaceRequest=null;hdSurfacesUnavailable=true;
            console.warn('Texturas de alta resolução indisponíveis.',error);
        });
    }
}
function useAbility() {
    if(state!=='playing'||zipRide||player.hp<=0||!activateAbility(ability,operator,elapsed))return;
    updateAbilityEffect();
    updateHud();
}
function updateAbilityEffect() {
    if(!abilityRing)return;
    const remaining=ability.until-elapsed;
    abilityRing.visible=remaining>0&&player.hp>0;
    if(!abilityRing.visible)return;
    const p=player.root.position;
    abilityRing.position.set(p.x,Math.max(p.y,groundHeight(p.x,p.z))+.16,p.z);
    const radius=operator.id==='vigia'?1.32+(1-Math.max(0,remaining)/operator.duration)*2.1:1.45;
    abilityRing.scale.set(radius,1,radius);
    abilityRing.material.color.setHex(operator.color);
    abilityRing.material.opacity=.32+.2*Math.sin(elapsed*7)**2;
}

function selectOperator(profile) {
    previewOperator=profile;
    const hue='#'+profile.color.toString(16).padStart(6,'0');
    $('operators').style.setProperty('--operator-color',hue);
    $('operator-role').textContent=profile.role;
    $('operator-ability').textContent=profile.ability;
    $('operator-description').textContent=profile.description;
    $('operator-cooldown').textContent='Recarga: '+profile.cooldown+' s';
    for(const button of $('operator-list').children)
        button.setAttribute('aria-pressed',button.dataset.operator===profile.id);
    if(player)equipAppearance(player,operatorLook(profile));
}
function closeOperators() {
    $('operators').hidden=true;
    $('lobby').hidden=false;
    if(previewOperator!==operator&&player)equipAppearance(player,operatorLook(operator));
    previewOperator=null;
    setInspect(false);
    updateOperatorLabel();
}
for(const profile of operators){
    const button=document.createElement('button');
    button.type='button';button.dataset.operator=profile.id;button.textContent=profile.name;
    button.addEventListener('click',()=>selectOperator(profile));
    $('operator-list').append(button);
}
$('operators-open').onclick=()=>{
    if(state!=='lobby')return;
    selectOperator(operator);
    $('lobby').hidden=true;
    $('operators').hidden=false;
};
$('operators-close').onclick=closeOperators;
function setInspect(enabled) {
    inspecting=enabled;
    $('operator-inspect').textContent=enabled?'Corpo inteiro':'Aproximar';
    $('operator-inspect').setAttribute('aria-pressed',enabled);
}
$('operator-inspect').onclick=()=>setInspect(!inspecting);
$('operator-confirm').onclick=()=>{
    if(!previewOperator)return;
    operator=previewOperator;
    try {localStorage.setItem('operation-zero-operator-v1',operator.id);} catch {}
    closeOperators();
};
$('ability').onclick=useAbility;

function showLoadoutWeapon() {
    player.root.remove(player.gun);
    player.gun=weaponModel(loadout[0],rifle,collection?.skin());
    player.weapon=loadout[0];player.root.add(player.gun);
}
function option(text,value){return new Option(text,value);}
for(const size of roomSizes)$('room-size').add(option(size+' combatentes',size));
$('room-size').value=String(roomSize);
$('room-size').onchange=e=>{roomSize=Number(e.target.value);};
for(const [key,value] of Object.entries(families))$('weapon-family').add(option(value.label,key));
for(const [i,item] of arsenal.entries())$('weapon-secondary').add(option(item.name,i));
function describeWeapon() {
    const item=arsenal[Number($('weapon-select').value)];
    $('weapon-description').textContent=item.capacity
        ?`${item.capacity} por carregador · ${item.range} m · recarga ${item.reload.toFixed(1)} s`
        :`Alcance de ${item.range.toFixed(1)} m · sem munição`;
}
function showFamily() {
    $('weapon-select').replaceChildren();
    for(const [i,item] of arsenal.entries())if(item.type===$('weapon-family').value)
        $('weapon-select').add(option(item.name,i));
    describeWeapon();
}
$('weapon-family').onchange=showFamily;
$('weapon-select').onchange=describeWeapon;
$('arsenal-open').onclick=()=>{
    $('weapon-family').value=loadout[0].type;
    showFamily();
    $('weapon-select').value=arsenal.indexOf(loadout[0]);
    $('weapon-secondary').value=arsenal.indexOf(loadout[1]);
    describeWeapon();
    $('arsenal').hidden=false;
};
$('arsenal-close').onclick=()=>{$('arsenal').hidden=true;};
$('weapon-confirm').onclick=()=>{
    loadout=[arsenal[Number($('weapon-select').value)],arsenal[Number($('weapon-secondary').value)]];
    try {localStorage.setItem('operation-zero-loadout-v1',JSON.stringify(loadout.map(item=>item.name)));} catch {}
    if(player)showLoadoutWeapon();
    $('arsenal').hidden=true;
};
$('weapon-swap').onclick=()=>{if(state==='playing'&&!zipRide)switchWeapon(1-equipped);};
function operatorLook(profile) {
    const outfit=collection?.outfit();
    if(!outfit)return profile;
    return {...profile,cloth:outfit.cloth,armor:outfit.armor,fabric:outfit.fabric,appearanceKey:profile.id+':'+outfit.id};
}
function showEntryOrLobby(){
    $('account').hidden=Boolean(accountProfile);
    $('lobby').hidden=!accountProfile;
    updateOperatorLabel();
    if(player){state=accountProfile?'lobby':'entry';document.body.dataset.state=state;}
}
function updateOperatorLabel(){
    $('equipped-operator').textContent=operator.name;
    $('equipped-role').textContent=operator.role;
}
function refreshCosmetics(){
    if(player&&state==='lobby'){
        equipAppearance(player,operatorLook(operator));
        showLoadoutWeapon();
    }
}
setupAccount({
    enter(profile){
        accountProfile=profile;
        $('profile-label').textContent=profile.label;
        collection=cosmetics(profile.key,refreshCosmetics);
        showEntryOrLobby();
        refreshCosmetics();
    },
    leave(){
        accountProfile=null;collection=null;
        showEntryOrLobby();
        if(player){equipAppearance(player,operator);showLoadoutWeapon();}
    },
});
for(const button of document.querySelectorAll('[data-collection]')){
    button.onclick=()=>{if(state==='lobby'&&collection)collection.open(button.dataset.collection);};
}
$('collection-close').onclick=()=>{$('collection').hidden=true;};
$('throw').onclick=throwItem;
$('throw-type').onclick=cycleThrow;

function cycleThrow() {
    if(state!=='playing'||zipRide)return;
    selectedThrow=selectedThrow==='frag'?'smoke':'frag';
    updateHud();
}
function throwItem() {
    if(state!=='playing'||zipRide||vault||grenades[selectedThrow]<=0)return;
    const kind=selectedThrow;
    grenades[kind]--;
    const direction=new T.Vector3(Math.sin(input.yaw),0,-Math.cos(input.yaw));
    const mesh=new T.Mesh(grenadeMesh,throwableMaterials[kind]);
    mesh.position.copy(player.root.position).addScaledVector(direction,.68);
    mesh.position.y+=crouched?1.0:1.4;
    const velocity=direction.multiplyScalar(11.5);
    velocity.y=5.2+Math.max(-.9,input.pitch*4.1);
    airborneItems.push({kind,mesh,velocity,until:elapsed+1.65});
    scene.add(mesh);
    playWeaponAudio?.(throwables[kind],.23);
    updateHud();
}
function clearThrows() {
    for(const item of airborneItems)scene.remove(item.mesh);
    airborneItems.length=0;
}
function stepThrows(dt) {
    for(let i=airborneItems.length-1;i>=0;i--){
        const item=airborneItems[i],p=item.mesh.position,v=item.velocity;
        const steps=Math.max(1,Math.ceil(dt/.024)),step=dt/steps;
        for(let k=0;k<steps;k++){
            const x=p.x+v.x*step,z=p.z+v.z*step;
            if(canMove(x,p.z,.075,p.y,.15))p.x=x;else v.x*=-.44;
            if(canMove(p.x,z,.075,p.y,.15))p.z=z;else v.z*=-.44;
            v.y-=15.5*step;
            const floor=floorAt(collision,p.x,p.z,p.y+.01,.075)+.078;
            const y=p.y+v.y*step;
            if(y<floor&&v.y<0){
                p.y=floor;v.y=Math.abs(v.y)<.72?0:-v.y*.35;
                v.x*=.72;v.z*=.72;
            }else if(canMove(p.x,p.z,.075,y,.15))p.y=y;else v.y*=-.28;
        }
        if(elapsed<item.until)continue;
        if(item.kind==='smoke')deployEffect('smoke',p,6);
        else{
            deployEffect('flash',p,2.5);
            playWeaponAudio?.(grenadeBlast,.45);
            blastDamage(p,6.4,78,.74,true);
        }
        scene.remove(item.mesh);airborneItems.splice(i,1);
    }
}

boot().catch(error=>{
    console.error(error);
    document.body.dataset.ready='failed';
    $('launch-status').textContent='Não foi possível iniciar WebGL 2. Abra em um navegador com aceleração gráfica.';
});
