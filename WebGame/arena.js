import { placeScenery } from './scenery.js';
import { haze, makeSky, warehouseSigns, wearOnGeometry } from './arena-finish.js';
import { calmWaterSurface, cladding, shoreShelf, waterSurface } from './coast.js';
import * as T from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { boundary, createGround, groundHeight, terrainSize } from './terrain.js';

export { boundary };
const random = (() => {
    let seed = 92177;
    return () => ((seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0) / 4294967296);
})();

export function buildArena(scene, textures, rocks, tree, distantTree) {
    const obstacles = [];
    const lootSites = [], signPoints=[];
    const material = (color, map, metalness = 0, roughness = .88) =>
        new T.MeshStandardMaterial({ color, map:map??null, roughness, metalness });
    const tiles=[];
    function tiled(key, x, y) {
        const copy = textures[key].clone();
        tiles.push({key,copy});
        copy.repeat.set(x, y);
        copy.needsUpdate = true;
        return copy;
    }
    const soil = material(0xe0dfc9, tiled('grass', 93, 93));
    soil.vertexColors=true;
    const plaster = material(0xe5c8a5, tiled('concrete', 3, 2));
    const steel = material(0x4a6674, tiled('concrete', 3, 2), .31);
    const rust = material(0x9e5d49, tiled('concrete', 2, 1), .25);
    const siding=cladding();
    const harborPaint=material(0xa95839,siding,.31,.58);
    const harborBlue=material(0x477c86,siding,.37,.56);
    const harborGold=material(0xba914e,siding,.29,.57);
    const pier=material(0x8f8983,tiled('concrete',5,3),.04,.84);
    const signal=material(0xf3c274,null,.24,.53);
    const dark = material(0x253238, null, .5);
    const roof = material(0x485253, tiled('asphalt', 3, 2), .28);
    const road = material(0xc3bbb2, tiled('asphalt', 34, 2));
    const avenue=material(0xb9b6a8,tiled('asphalt',2,33));
    const surfaces=[
        [soil,'grassNormal',93,93,.62],
        [plaster,'concreteNormal',3,2,.93],
        [steel,'concreteNormal',3,2,.38],
        [rust,'concreteNormal',2,1,.52],
        [road,'asphaltNormal',34,2,.82],
        [avenue,'asphaltNormal',2,33,.82],
        [roof,'asphaltNormal',3,2,.44],
    ].filter(([,normal])=>textures[normal]).map(([surface,normal,x,y,strength])=>{
        surface.normalScale.setScalar(strength);
        return [surface,tiled(normal,x,y)];
    });
    const wood = material(0x685741);
    const glass = material(0x587a89, null, .33, .29);
    const sill = material(0x9ea7a5, null, .29, .76);
    const paint = material(0xd9aa62);
    const lanePaint=material(0xd4d5c5);
    const unit = new T.BoxGeometry(1,1,1);
    const boxes = [];
    function piece(geometry, mat, x, y, z) {
        const obj = new T.Mesh(geometry, mat);
        obj.position.set(x,y,z);
        obj.receiveShadow = true;
        scene.add(obj);
        return obj;
    }
    function box(x,y,z,w,h,d,mat,solid = false) {
        const mesh = piece(unit,mat,x,y,z);
        mesh.scale.set(w,h,d);
        mesh.castShadow = h > .2;
        boxes.push(mesh);
        if (solid) {
            obstacles.push({x,z,w,d,bottom:y-h/2,top:y+h/2});
        }
        return mesh;
    }
    function terrainCover(x,z,w,h,d) {
        const floor=groundHeight(x,z);
        obstacles.push({x,z,w,d,bottom:floor,top:floor+h});
    }
    function plane(x,z,w,d,mat,y = .012) {
        const obj = piece(new T.PlaneGeometry(w,d),mat,x,y,z);
        obj.rotation.x = -Math.PI/2;
        return obj;
    }
    const ground=createGround(soil);
    scene.add(ground);
    plane(0,0,terrainSize-4,11,road);
    plane(6,0,10,terrainSize-4,avenue,.018);
    for(let i=-187;i<188;i+=6) {
        if (Math.abs(i-6)>6) box(i,.03,0,2.4,.012,.15,paint);
        if (Math.abs(i)>6) box(6,.03,i,.15,.012,2.4,paint);
    }
    for(const z of [-5.7,5.7]) box(0,.10,z,380,.20,.45,plaster);
    for(const side of [-1,1]){
        box(0,.026,side*4.88,380,.012,.095,lanePaint);
        for(const z of [-22,21])for(let i=0;i<7;i++)
            box(2.3+i*1.23,.029,z,.67,.013,3.05,lanePaint);
    }
    const water=waterSurface();
    const sea=plane(0,0,1100,1100,water,-1.41);
    scene.add(shoreShelf());
    sea.receiveShadow=false;
    const calmWater=calmWaterSurface();
    const scenery=[];
    const trees=[],stones=[];

    function warehouse(x,z,w,d,mat) {
        const eave = 4.4;
        box(x,.12,z,w,.24,d,plaster,true);
        lootSites.push({kind:'interior',points:[
            {x:x-w*.26,y:.24,z:z-d*.23}, {x:x+w*.27,y:.24,z:z+d*.20},
        ]});
        box(x,2.2,z+d/2,w,eave,.32,mat,true);
        box(x-w/2,2.2,z,.32,eave,d,mat,true);
        box(x+w/2,2.2,z,.32,eave,d,mat,true);
        const section = (w-5)/2;
        for(const side of [-1,1]){
            box(x+side*(w+5)/4,2.2,z-d/2,section,eave,.32,mat,true);
            box(x+side*(w+5)/4,2.95,z-d/2-.19,section*.52,1.00,.10,glass);
            box(x+side*2.52,1.7,z-d/2-.19,.10,3.4,.14,dark);
            const pane=x+side*(w+5)/4;
            box(pane,2.42,z-d/2-.265,section*.57,.092,.19,sill);
            box(pane,2.96,z-d/2-.258,.072,1.01,.09,dark);
        }
        signPoints.push({x,z:z-d/2-.25});
        for(const side of [-1,1]){
            box(x+side*(w*.5-.32),.29,z-d/2-.18,.52,.58,.10,steel);
            box(x+side*2.69,2.57,z-d/2-.245,.15,.25,.14,signal);
        }
        box(x,3.91,z-d/2,5,1,.37,steel,true);
        const roofHeight=4.58;
        box(x,roofHeight-.09,z,w+.56,.18,d+.86,roof,true);
        for(const side of [-1,1]){
            box(x+side*(w/2+.16),roofHeight+.27,z,.18,.54,d+.82,steel,true);
            box(x+side*(w*.29),roofHeight+.27,z-d/2-.33,w*.36,.54,.18,steel,true);
        }
        const stairX=x+w/2-1.65;
        const steps=20, tread=.39;
        for(let i=0;i<steps;i++){
            const height=roofHeight*(i+1)/steps;
            const stairZ=z+d/2+.43+(steps-i-.5)*tread;
            box(stairX,height/2,stairZ,2.14,height,tread,steel,true);
            box(stairX-1.14,height+.45,stairZ,.12,.10,tread,paint);
            box(stairX+1.14,height+.45,stairZ,.12,.10,tread,paint);
            if(i%4===0)for(const side of [-1,1])
                box(stairX+side*1.14,height+.21,stairZ,.10,.49,.1,dark);
        }
        lootSites.push({kind:'roof',points:[
            {x:x-w*.22,y:roofHeight,z:z+d*.16}, {x:x+w*.21,y:roofHeight,z:z-d*.18},
        ]});
        box(x,.12,z-d/2-.74,5.2,.24,1.4,plaster,true);
        for(let i=2;i<d-1;i+=1.95) {
            box(x-w/2-.19,2.2,z-d/2+i,.10,4.25,.19,dark);
            box(x+w/2+.19,2.2,z-d/2+i,.10,4.25,.19,dark);
        }
        box(x,1.05,z+1.5,2.2,2.1,2.3,steel,true);
    }
    warehouse(-24,-20,19,16,plaster);
    warehouse(28,-21,18,15,steel);
    warehouse(-26,26,16,14,rust);
    warehouse(30,25,21,16,harborGold);
    warehouse(68,-38,18,15,rust);
    warehouse(-69,-41,21,14,steel);
    warehouse(-65,38,19,17,plaster);
    warehouse(67,48,20,17,steel);
    warehouse(-145,-20,20,15,steel);
    warehouse(-134,24,23,16,rust);
    warehouse(137,-20,23,16,harborBlue);
    warehouse(152,25,18,15,plaster);
    warehouse(29,-144,19,15,rust);
    warehouse(-32,-155,20,15,plaster);
    warehouse(38,139,21,18,harborPaint);
    warehouse(-27,146,18,15,rust);
    const identifiers=warehouseSigns(signPoints);
    if(identifiers)scene.add(identifiers);
    function office(x,z,wall) {
        box(x,2.8,z,12,5.6,9,wall,true);
        box(x,5.75,z,12.65,.3,9.6,roof);
        for(const height of [1.55,4.1])for(const dx of [-3.6,0,3.6]){
            box(x+dx,height,z-4.56,2.05,1.26,.16,dark);
            box(x+dx,height,z-4.67,1.82,.98,.10,glass);
            box(x+dx,height,z+4.61,1.82,.98,.10,glass);
            box(x+dx,height-.58,z-4.67,2.14,.094,.21,sill);
            box(x+dx,height,z+4.674,.075,1.0,.069,dark);
            box(x+dx,height,z-4.737,.075,1.0,.069,dark);
        }
        box(x,5.9,z+2.6,3.2,.5,2.8,steel);
    }
    office(-37,-67,plaster);
    office(-20,-70,rust);
    office(37,71,plaster);
    office(53,73,steel);
    office(-81,12,rust);
    office(83,12,plaster);
    office(-124,-12,plaster);
    office(-149,15,steel);
    office(125,10,plaster);
    office(142,36,steel);
    office(23,-122,steel);
    office(-16,-136,plaster);
    office(18,122,plaster);
    office(-16,130,steel);


    function container(x,z,mat) {
        box(x,1.22,z,7,2.44,2.72,mat,true);
        for(let i=-3.3;i<=3.3;i+=.41){
            box(x+i,1.21,z+1.39,.11,2.32,.11,mat);
            box(x+i,1.21,z-1.39,.11,2.32,.11,mat);
        }
        for(const side of [-1,1]) box(x+3.55,1.23,z+side*.43,.07,2.24,.07,dark);
    }
    for(const [x,z] of [[-14,-9],[-42,8],[18,10],[42,-11],[15,-36],[-12,42],[47,33],[-64,-67],[68,-65],[-82,52],[86,63],[84,-8],[-162,17],[-119,-25],[117,13],[159,-9],[-13,-149],[19,-157],[-16,153],[25,158]]) container(x,z, x>0 ? steel : rust);
    for(const [x,z] of [[-11,10],[-12,14],[17,-10],[23,13],[-31,-3],[14,33],[30,-8],[33,35],[-33,35]]){
        box(x,.55,z,1.35,1.1,1.28,wood,true);
        for(const off of [-.50,.50]) box(x+off,.55,z, .13,1.16,1.33,dark);
    }
    const coverLocations=[[-7,17,true],[11,22,false],[-27,6,true],[31,-2,true],
        [-53,19,false],[53,-16,false],[-70,-16,true],[71,11,true],[50,53,true],[-54,-58,true],[-167,-12,true],[-123,15,false],[-152,37,true],[117,-5,true],[155,4,true],[167,19,false],[24,-136,true],[-18,-153,false],[25,140,false],[-17,152,true]];
    for(const [x,z,horizontal] of coverLocations){
        box(x,.55,z,horizontal?4.2:.46,1.10,horizontal?.46:4.2,plaster,true);
        box(x,1.12,z,horizontal?4.32:.55,.11,horizontal?.55:4.32,steel,true);
        for(const side of [-1,1]){
            box(x+(horizontal?side*1.65:0),.43,z+(horizontal?0:side*1.65),
                .53,.86,.55,plaster,true);
        }
    }

    const tire=new T.CylinderGeometry(.35,.35,.24,14);
    const hub=new T.CylinderGeometry(.20,.20,.044,12);
    function car(x,z,horizontal,color) {
        const paintwork=material(color,null,.40,.43);
        const position=(across,along)=>horizontal?[x+along,z+across]:[x+across,z+along];
        const part=(across,y,along,w,h,d,mat,solid=false)=>{
            const [px,pz]=position(across,along);
            return box(px,y,pz,horizontal?d:w,h,horizontal?w:d,mat,solid);
        };
        part(0,.55,0,1.79,.62,4.22,paintwork,true);
        part(0,1.05,.37,1.52,.43,1.91,glass,true);
        part(0,1.31,.40,1.6,.13,1.96,paintwork,true);
        part(0,.39,-2.12,1.82,.16,.13,dark);
        part(0,.42,2.13,1.74,.16,.15,dark);
        for(const side of [-1,1]){
            part(side*.57,.68,-2.11,.34,.13,.07,paint);
            part(side*.63,.66,2.12,.28,.13,.07,rust);
            part(side*.77,1.03,.37,.075,.49,.10,paintwork);
            for(const along of [-1.28,1.29]){
                const [tx,tz]=position(side*.83,along);
                const [hx,hz]=position(side*.96,along);
                for(const wheel of [piece(tire,dark,tx,.36,tz),piece(hub,steel,hx,.36,hz)]){
                    if(horizontal)wheel.rotation.x=Math.PI/2;
                    else wheel.rotation.z=Math.PI/2;
                    wheel.castShadow=true;
                }
            }
        }
        const [lx,lz]=position(2.6,.7);
        lootSites.push({kind:'yard',points:[{x:lx,y:0,z:lz}]});
    }
    car(-4,27,false,0x627b70);
    car(23,-3,true,0x753f2b);
    car(-45,3,true,0x697e8b);
    car(56,17,false,0x97856b);
    car(-27,-49,true,0x74604f);
    car(62,-24,false,0x525e75);
    car(-73,54,true,0x72754f);
    car(65,68,true,0x865141);
    car(-153,7,false,0x697e8b);
    car(122,-9,true,0x627b70);
    car(44,-126,true,0x97856b);
    car(-33,131,true,0x753f2b);
    car(34,154,false,0x6e7779);
    car(167,8,false,0x74604f);
    for(const [x,z] of [[-3,14],[2,12],[-12,13],[16,-13],[38,9],[-42,9],
            [3,-31],[-34,59],[35,-60],[60,37],[-73,-26],[77,6],[-54,35],[28,42],[-146,11],[149,-17],[19,-144],[-20,146]]){
        lootSites.push({kind:'yard',points:[{x,y:0,z}]});
    }

    // Distinct harbor landmarks sit close to the existing south route and stay clear of rooftop stairs.
    for(const x of [27,51,75]) {
        box(x,-.53,182,14,1.06,17,pier);
        for(const side of [-1,1]){
            box(x+side*6.5,.32,183,.20,.65,15,pier);
            box(x+side*6.5,-1.4,190,.64,2.2,.64,dark);
        }
        box(x,.015,177,.18,.025,7,signal);
        box(x-4,-1.18,194,.75,2.25,.75,steel);
        box(x+4,-1.18,194,.75,2.25,.75,steel);
        box(x,-.51,196,10,1.06,13,pier);
    }
    function truss(ax,ay,az,bx,by,bz,thickness,mat) {
        const start=new T.Vector3(ax,ay,az),end=new T.Vector3(bx,by,bz);
        const center=start.clone().lerp(end,.5);
        const beam=box(center.x,center.y,center.z,thickness,start.distanceTo(end),thickness,mat);
        beam.quaternion.setFromUnitVectors(new T.Vector3(0,1,0),end.sub(start).normalize());
    }
    for(const x of [31,81]){
        const z=170;
        for(const side of [-1,1]){
            const px=x+side*2.5;
            box(px,.36,z,1.1,.72,1.4,pier,true);
            truss(px,.7,z,px,15,z,.29,harborGold);
            truss(px,1,z+3,px,12,z,.22,steel);
            truss(px,14,z-5,px,14,z+18,.28,harborGold);
            for(let j=-5;j<17;j+=4){
                truss(px,14,z+j,px,16,z+j+2,.16,steel);
                truss(px,16,z+j+2,px,14,z+j+4,.16,steel);
            }
        }
        for(const dz of [-3,5,12,17])box(x,14,z+dz,5.4,.23,.27,harborGold);
        box(x,11.9,z-1,2.2,1.55,2.1,glass);
        box(x,10.0,z+12,.16,7.4,.18,dark);
        box(x,6.22,z+12,1.65,.32,.34,signal);
    }
    for(const [x,z,style] of [[20,106,harborBlue],[33,110,harborGold],[49,117,harborPaint],
        [47,163,harborBlue],[70,165,harborPaint],[15,172,harborGold]]){
        container(x,z,style);
        lootSites.push({kind:'yard',points:[{x,y:0,z:z+3.4}]});
    }
    const hullMat=material(0x324751,null,.38,.63);
    const deckMat=material(0xa19472,null,.14,.68);
    for(const [x,z] of [[61,216],[-44,-223]]){
        box(x,-.85,z,11,1.9,32,hullMat);
        box(x,.27,z,10,.34,28,deckMat);
        box(x,1.31,z+7,7.2,2.0,9.5,harborBlue);
        box(x,2.41,z+7,7.7,.19,9.9,pier);
        box(x,1.48,z+2.21,5.4,.57,.08,glass);
        box(x-.9,3.63,z+10,.28,2.3,.28,dark);
        for(const offset of [-8,-5,-2])box(x,.78,z+offset,6.1,.57,2.0,rust);
    }

    const poleGeometry = new T.CylinderGeometry(.055,.13,7.8,12);
    for(const [x,z] of [[-34,-6],[-17,6],[16,-6],[34,6],[53,-6],[-53,6],[0,-36],[12,36]]){
        piece(poleGeometry,dark,x,3.9,z);
        box(x,7.8,z+.46,.13,.14,.93,dark);
        box(x,7.78,z+.89,.38,.10,.28,paint);
    }
    const towerX=-42,towerZ=-32;
    for(const dx of [-2,2])for(const dz of [-2,2])box(towerX+dx,7,towerZ+dz,.27,14,.27,steel);
    for(let y=3;y<15;y+=3)for(const off of [-2,2]){
        box(towerX+off,y,towerZ,.15,.16,4.15,steel);
        box(towerX,y,towerZ+off,4.15,.16,.15,steel);
    }
    piece(new T.CylinderGeometry(2.25,2.25,3.4,24),steel,towerX,14.3,towerZ).castShadow=true;
    piece(new T.ConeGeometry(2.35,.92,24),dark,towerX,16.4,towerZ);

    // Low-poly blades are instanced across the roadside and strip of coastal sand.
    const blade=new T.BufferGeometry();
    const blades=[];
    for(let i=0;i<7;i++){
        const angle=i*2.399;
        const x=Math.cos(angle)*.16,z=Math.sin(angle)*.16;
        const dx=Math.cos(angle+.9)*.034,dz=Math.sin(angle+.9)*.034;
        blades.push(x-dx,0,z-dz,x+dx,0,z+dz,x*2.8,.43+i*.045,z*2.8);
    }
    blade.setAttribute('position',new T.Float32BufferAttribute(blades,3));
    blade.computeVertexNormals();
    const grass = new T.InstancedMesh(blade,new T.MeshStandardMaterial({color:0x76834f,side:T.DoubleSide,roughness:1}),3800);
    const dummy = new T.Object3D();
    let grassCount = 0;
    for(let i=0;i<6300&&grassCount<3800;i++){
        const x=(random()-.5)*374,z=(random()-.5)*374;
        if(Math.abs(z)<6.2||Math.abs(x-6)<6||obstacles.some(o=>Math.abs(x-o.x)<o.w*.5&&Math.abs(z-o.z)<o.d*.5))continue;
        dummy.position.set(x,groundHeight(x,z),z);dummy.scale.setScalar(.47+random()*.56);dummy.rotation.y=random()*Math.PI*2;
        dummy.updateMatrix();grass.setMatrixAt(grassCount++,dummy.matrix);
        grass.setColorAt(grassCount-1,new T.Color().setHSL(.13+random()*.04,.22,.35+random()*.25));
    }
    grass.count=grassCount;grass.receiveShadow=true;scene.add(grass);
    if(tree){
        let treeIndex=0;
        tree.updateMatrixWorld(true);
        const bounds=new T.Box3().setFromObject(tree);
        const factor=1/Math.max(.1,bounds.max.y-bounds.min.y);
        const locations=[[-9,9,7],[-14,18,6.1],[-48,12,8.5],[-57,38,7.9],[15,21,5.8],
            [43,14,8.4],[50,-29,6.6],[-16,-39,7.2],[-53,-23,8.2],[4,-47,8],[17,-29,6.1],[22,43,8.5],[-83,-58,7],[-75,73,8],[77,-77,9],[88,53,8],[-26,82,7],[20,-83,8],
            ...Array.from({length:78},(_,i)=>{
                const corner=i%4;
                const sx=corner<2?-1:1, sz=corner%2?-1:1;
                return [sx*(76+random()*108),sz*(74+random()*108),4.5+random()*5.2];
            })];
        for(const [x,z,height] of locations){
            if(obstacles.some(o=>Math.abs(x-o.x)<o.w*.5+1.4&&Math.abs(z-o.z)<o.d*.5+1.4))continue;
            const scale=height*factor;
            trees.push({x,y:groundHeight(x,z)-bounds.min.y*scale,z,scale,yaw:random()*Math.PI*2});
            if(treeIndex++%3===0)terrainCover(x,z,Math.min(1.04,height*.12),1.95,Math.min(.94,height*.11));
        }
    }
    if(rocks)for(let i=0;i<25;i++){
        const angle=i*2*Math.PI/25;
        const radius=191+random()*12;
        const x=Math.cos(angle)*radius,z=Math.sin(angle)*radius;
        stones.push({x,y:groundHeight(x,z)-.28,z,yaw:random()*Math.PI,scale:.65+random()*1.4});
    }

    if(rocks)for(const [cx,cz] of [[-139,-137],[138,-131],[-145,137],[144,141]]){
        for(let i=0;i<7;i++){
            const x=cx+(random()-.5)*31,z=cz+(random()-.5)*30;
            stones.push({x,y:groundHeight(x,z)-.43,z,yaw:random()*Math.PI,scale:.84+random()*1.08});
            terrainCover(x,z,2.2,1.22,1.9);
        }
        lootSites.push({kind:'yard',points:[{x:cx,y:groundHeight(cx,cz),z:cz}]});
    }
    if(tree)scenery.push(placeScenery(scene,trees,tree,distantTree,true));
    if(rocks)scenery.push(placeScenery(scene,stones,rocks));
    const island = new T.IcosahedronGeometry(1,3);
    for(const [x,z,w,h,d] of [[-195,-257,49,35,38],[-257,-177,45,23,34],[45,-290,45,28,37],[217,-244,39,20,35],[269,22,53,29,40],[-234,210,40,21,25]]) {
        piece(island,material(0x4f6b65),x,-5,z).scale.set(w,h,d);
    }
    // Merge static boxes by material to keep the detailed arena within a mobile draw-call budget.
    const groups=new Map(), tinted=new Map();
    for(const box of boxes){
        box.updateMatrixWorld(true);
        const key=box.material.uuid+Number(box.castShadow);
        if(!groups.has(key))groups.set(key,{material:box.material,castShadow:box.castShadow,geometries:[]});
        const geometry=unit.clone();
        wearOnGeometry(geometry,box);
        groups.get(key).geometries.push(geometry.applyMatrix4(box.matrixWorld));
        scene.remove(box);
    }
    for(const {material,castShadow,geometries} of groups.values()){
        if(!tinted.has(material)){
            const surface=material.clone();surface.vertexColors=true;tinted.set(material,surface);
        }
        const mesh=piece(mergeGeometries(geometries),tinted.get(material),0,0,0);
        mesh.castShadow=castShadow;
        for(const geometry of geometries)geometry.dispose();
    }
    return { obstacles, lootSites, ground, updateView(camera,focus,time,preset) {
        if(sea.material===water)water.uniforms.time.value=time;
        for(const update of scenery)update(camera,focus,preset);
    }, setDetail(preset,detail,maxAnisotropy=preset.anisotropy) {
        const filtering=Math.max(1,Math.min(maxAnisotropy,preset.anisotropy));
        for(const {key,copy} of tiles){
            const source=(detail?.[key]??textures[key]).source;
            if(copy.source!==source||copy.anisotropy!==filtering){
                copy.dispose();
                copy.source=source;
                copy.anisotropy=filtering;
                copy.needsUpdate=true;
            }
        }
        for(const [surface,normal] of surfaces){
            const map=preset.normals?normal:null;
            if(surface.normalMap!==map){surface.normalMap=map;surface.needsUpdate=true;}
            const copy=tinted.get(surface);
            if(copy&&copy.normalMap!==map){copy.normalMap=map;copy.needsUpdate=true;}
        }
        grass.count=Math.floor(grassCount*preset.grass);
        sea.material=preset.waves?water:calmWater;
    } };
}

export function lightArena(scene) {
    scene.fog = new T.FogExp2(haze,.0037);
    scene.background=new T.Color(haze);
    const sunDirection = new T.Vector3().setFromSphericalCoords(1,1.05,5.63);
    const sky=makeSky();
    sky.material.toneMapped=true;
    scene.add(sky,new T.HemisphereLight(0xd6e5ee,0xc2b092,1.73));
    const sun = new T.DirectionalLight(0xffe2bd,3.05);
    sun.position.copy(sunDirection).multiplyScalar(170);
    sun.castShadow=true;
    sun.shadow.mapSize.set(2048,2048);
    Object.assign(sun.shadow.camera,{left:-61,right:61,top:61,bottom:-61,near:1,far:310});
    sun.shadow.camera.updateProjectionMatrix();
    sun.shadow.normalBias=.04;
    sun.shadow.bias=-.00025;
    scene.add(sun,sun.target);
    return {
        setDetail(preset){
            const size=preset.shadowSize;
            const range=preset.shadowRange;
            const camera=sun.shadow.camera;
            if(camera.right!==range){
                Object.assign(camera,{left:-range,right:range,top:range,bottom:-range});
                camera.updateProjectionMatrix();
            }
            if(!size||sun.shadow.mapSize.x!==size){
                sun.shadow.map?.dispose();
                sun.shadow.map=null;
                if(size)sun.shadow.mapSize.set(size,size);
            }
        },
        focus(position) {
            const x=Math.round(position.x/3)*3,z=Math.round(position.z/3)*3;
            sun.target.position.set(x,position.y,z);
            sun.position.copy(sun.target.position).addScaledVector(sunDirection,170);
            sun.target.updateMatrixWorld();

        }
    };
}
