import * as T from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';
import { groundHeight, boundary } from './terrain.js';
import { floorAt, freeAt } from './traversal.js';

export const musterSeconds=8;
export const flightSeconds=15;
export const firstJump=1.5;
export const automaticOpening=24;
export const flightFrom=new T.Vector3(-176,96,-137);
export const flightTo=new T.Vector3(171,96,128);

export function flightPosition(time,out) {
    return out.lerpVectors(flightFrom,flightTo,T.MathUtils.clamp(time/flightSeconds,0,1));
}

export function landingFloor(obstacles,x,z) {
    return floorAt(obstacles,x,z,Infinity,.36);
}

export function descentStep(position,velocity,desired,dt,open,obstacles) {
    velocity.lerp(desired,1-Math.exp(-dt*(open?3.6:2.8)));
    const x=T.MathUtils.clamp(position.x+velocity.x*dt,-boundary+.55,boundary-.55);
    const z=T.MathUtils.clamp(position.z+velocity.z*dt,-boundary+.55,boundary-.55);
    if(landingFloor(obstacles,x,position.z)<=position.y)position.x=x;
    if(landingFloor(obstacles,position.x,z)<=position.y)position.z=z;
    const floor=landingFloor(obstacles,position.x,position.z);
    position.y=Math.max(floor,position.y-(open?6.3:22)*dt);
    return position.y<=floor+.001;
}

const routePoints=[
    {name:'Serra do Farol',a:[-132,-135],b:[-80,-86]},
    {name:'Morro da Vigia',a:[134,-126],b:[79,-80]},
    {name:'Morro do Aço',a:[137,143],b:[85,91]},
    {name:'Vale da Oficina',a:[-140,133],b:[-85,87]},
];

const cableHeight=3.5;
export function ziplinePoint(route,t,out) {
    out.lerpVectors(route.a,route.b,t);
    out.y+=cableHeight-.52*Math.sin(t*Math.PI);
    return out;
}

export function locateZiplines(obstacles) {
    const sample=new T.Vector3();
    return routePoints.flatMap(({name,a,b})=>{
        const anchor=([x,z])=>new T.Vector3(x,groundHeight(x,z),z);
        const route={name,a:anchor(a),b:anchor(b)};
        if(![route.a,route.b].every(p=>freeAt(obstacles,boundary,p.x,p.z,p.y,2,.75)))return [];
        const steps=Math.ceil(route.a.distanceTo(route.b)*2);
        for(let i=0;i<=steps;i++){
            ziplinePoint(route,i/steps,sample);
            if(!freeAt(obstacles,boundary,sample.x,sample.z,sample.y-1.92,2,.44))return [];
        }
        route.seconds=route.a.distanceTo(route.b)/17;
        return [route];
    });
}

export function nearestZipline(routes,p) {
    let nearest=null,limit=3.2**2;
    for(const route of routes)for(const reverse of [false,true]){
        const end=reverse?route.b:route.a;
        const distance=end.distanceToSquared(p);
        if(distance<limit){nearest={route,reverse};limit=distance;}
    }
    return nearest;
}

function material(color,roughness=.61,metalness=.2) {
    return new T.MeshStandardMaterial({color,roughness,metalness});
}
const unit=new T.BoxGeometry(1,1,1);
function box(group,mat,x,y,z,w,h,d) {
    const mesh=new T.Mesh(unit,mat);
    mesh.position.set(x,y,z);mesh.scale.set(w,h,d);
    group.add(mesh);
    return mesh;
}
function tube(group,mat,from,to,radius) {
    const delta=to.clone().sub(from);
    const mesh=new T.Mesh(new T.CylinderGeometry(radius,radius,delta.length(),8),mat);
    mesh.position.copy(from).lerp(to,.5);
    mesh.quaternion.setFromUnitVectors(new T.Vector3(0,1,0),delta.normalize());
    group.add(mesh);
}
function fin(group,points,height,y,mat) {
    const outline=new T.Shape();
    points.forEach(([x,z],i)=>i?outline.lineTo(x,-z):outline.moveTo(x,-z));
    const mesh=new T.Mesh(new T.ExtrudeGeometry(outline,{depth:height,bevelEnabled:false}),mat);
    mesh.rotation.x=-Math.PI/2;mesh.position.y=y;group.add(mesh);
    return mesh;
}

export function makeAircraft() {
    const plane=new T.Group();
    const skin=material(0x9ba9a5,.55,.30),dark=material(0x283c40,.48,.36);
    const trim=material(0xcd9339),glass=material(0x263b49,.24,.35);
    const shape=[[.40,-10.4],[.90,-9],[1.62,-6],[1.95,-2],[1.95,4.6],[1.8,6.6],[1.37,8.25],[.55,9.3],[.02,9.55]];
    const hull=new T.Mesh(new T.LatheGeometry(shape.map(([radius,z])=>new T.Vector2(radius,z)),24),skin);
    hull.rotation.x=Math.PI/2;hull.scale.z=.87;plane.add(hull);
    const wing=[[0,3.4],[4.4,2.75],[14.8,.2],[15,-.85],[13.2,-1.8],[5,-2.0],[0,-3.1]];
    const propellers=[];
    for(const side of [-1,1]){
        fin(plane,wing.map(([x,z])=>[side*x,z]),.28,.32,skin);
        fin(plane,[[0,-6.8],[5.0,-8.5],[4.8,-9.7],[0,-10.2]].map(([x,z])=>[side*x,z]),.15,.78,skin);
        fin(plane,[[13.4,.45],[14.83,.18],[15,-.85],[13.4,-1.65]].map(([x,z])=>[side*x,z]),.045,.61,trim);
        const cabin=box(plane,glass,side*.73,.94,7.32,1.18,.57,1.62);
        cabin.rotation.x=.26;cabin.rotation.y=side*.22;
        for(let z=-4;z<=5;z+=1.12)box(plane,dark,side*1.87,.52,z,.09,.30,.54);
        box(plane,trim,side*1.84,-.32,1,.08,.15,11);
        const motor=new T.Mesh(new T.CapsuleGeometry(.73,1.58,5,12),dark);
        motor.rotation.x=Math.PI/2;motor.position.set(side*5.55,.35,2.33);plane.add(motor);
        const prop=new T.Group();prop.position.set(side*5.55,.35,3.8);plane.add(prop);propellers.push(prop);
        for(let i=0;i<3;i++){
            const blade=new T.Group();blade.rotation.z=i*Math.PI*2/3;prop.add(blade);
            const bladeMesh=box(blade,dark,0,.95,0,.19,1.75,.085);
            bladeMesh.rotation.z=-.16;
            box(blade,trim,.10,1.72,0,.19,.20,.09);
        }
    }
    const rudder=fin(plane,[[0,-6.1],[3.42,-8.0],[3.30,-9.5],[0,-10.25]],.18,0,skin);
    const upright=new T.Group();
    plane.remove(rudder);upright.add(rudder);
    upright.rotation.z=Math.PI/2;upright.position.y=.72;plane.add(upright);
    box(plane,dark,0,-.28,-9.15,1.58,.43,.72);
    return {plane,propellers};
}

export function makeCanopy() {
    const group=new T.Group(),positions=[],colors=[],indices=[];
    const panels=12,depthRows=5;
    const ivory=new T.Color(0xefdfb3),orange=new T.Color(0xd27d34),blue=new T.Color(0x317c89);
    for(let row=0;row<=depthRows;row++)for(let i=0;i<=panels;i++){
        const u=2*i/panels-1,v=2*row/depthRows-1;
        const rib=.028*Math.cos(i*Math.PI);
        positions.push(3.15*u,3.3+1.08*(1-u*u)+.41*(1-v*v)+rib,1.18*v-.48*u*u);
        const color=(i===0||i===panels?blue:i%5<2?orange:ivory);
        colors.push(color.r,color.g,color.b);
        if(i<panels&&row<depthRows){
            const a=row*(panels+1)+i,b=a+panels+1;
            indices.push(a,b,a+1,a+1,b,b+1);
        }
    }
    const geometry=new T.BufferGeometry();
    geometry.setAttribute('position',new T.Float32BufferAttribute(positions,3));
    geometry.setAttribute('color',new T.Float32BufferAttribute(colors,3));
    geometry.setIndex(indices);geometry.computeVertexNormals();
    group.add(new T.Mesh(geometry,new T.MeshStandardMaterial({vertexColors:true,side:T.DoubleSide,roughness:.82})));
    const cords=[];
    for(const side of [-1,1])for(let i=1;i<=4;i++){
        const offset=side*i*2;
        const col=(panels+offset)/2;
        if(col<0||col>panels)continue;
        for(const row of [0,depthRows]){
            const vertex=(row*(panels+1)+col)*3;
            cords.push(side*.28,1.22,0,...positions.slice(vertex,vertex+3));
        }
    }
    const lines=new T.BufferGeometry();
    lines.setAttribute('position',new T.Float32BufferAttribute(cords,3));
    group.add(new T.LineSegments(lines,new T.LineBasicMaterial({color:0xcbd5ce})));
    return group;
}

export function makeMusterPad(x,z) {
    const group=new T.Group(),y=groundHeight(x,z);
    group.position.set(x,y,z);
    const plate=material(0x495758,.83),signal=material(0xd99e49,.72);
    const ring=new T.Mesh(new T.RingGeometry(6.6,6.78,48),signal);
    ring.rotation.x=-Math.PI/2;ring.position.y=.035;group.add(ring);
    for(const side of [-1,1]){
        box(group,signal,side*.9,.025,0,.26,.05,3.15);
        for(const end of [-1,1]){
            box(group,plate,side*4.9,.19,end*4.9,.72,.38,.72);
            box(group,signal,side*4.9,.41,end*4.9,.24,.26,.24);
        }
    }
    box(group,signal,0,.025,0,1.8,.05,.24);
    return group;
}

export function makeZiplineScenery(routes) {
    const group=new T.Group(),frame=material(0x344b4b),rail=material(0xd5a857);
    const cable=material(0xadbcae,.45,.62);
    for(const route of routes){
        for(const anchor of [route.a,route.b]){
            for(const side of [-1,1]){
                box(group,frame,anchor.x+side*.81,anchor.y+1.67,anchor.z,.19,3.34,.24);
                box(group,rail,anchor.x+side*.81,anchor.y+2.91,anchor.z,.22,.42,.25);
            }
            box(group,rail,anchor.x,anchor.y+3.39,anchor.z,1.94,.20,.26);
            box(group,frame,anchor.x,anchor.y+.035,anchor.z,1.96,.07,1.30);
        }
        let previous=null;
        for(let i=0;i<=12;i++){
            const current=ziplinePoint(route,i/12,new T.Vector3());
            if(previous)tube(group,cable,previous,current,.048);
            previous=current;
        }
    }
    const scenery=new T.Group();
    for(const mat of [frame,rail,cable]){
        const pieces=group.children.filter(mesh=>mesh.material===mat);
        if(!pieces.length)continue;
        const geometries=pieces.map(mesh=>{
            mesh.updateMatrix();
            const geometry=mesh.geometry.clone().applyMatrix4(mesh.matrix);
            if(mesh.geometry!==unit)mesh.geometry.dispose();
            return geometry;
        });
        scenery.add(new T.Mesh(mergeGeometries(geometries),mat));
        for(const geometry of geometries)geometry.dispose();
    }
    return scenery;
}
