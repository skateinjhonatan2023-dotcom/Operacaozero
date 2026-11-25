import * as T from 'three';

export const boundary=188;
export const terrainSize=384;
const divisions=128;
const grid=terrainSize/divisions;
const half=terrainSize/2;
const columns=divisions+1;
const peaks=[
    [-138,-138,56,48,37],[-160,-111,28,29,18],[-109,-155,30,26,18],
    [137,-132,49,58,32],[109,-156,37,29,16],
    [-143,139,53,46,27],[-114,152,32,36,18],
    [143,143,47,46,29],[168,108,23,36,15],
];
const heights=new Float32Array(columns*columns);

function heightAtVertex(x,z) {
    let result=0;
    for(const [cx,cz,rx,rz,h] of peaks){
        const distance=Math.hypot((x-cx)/rx,(z-cz)/rz);
        if(distance<1)result=Math.max(result,h*(.5+.5*Math.cos(Math.PI*distance))**1.18);
    }
    return result===0?0:Math.max(0,result+
        Math.min(1,result*.16)*(.52*Math.sin(x*.37)*Math.cos(z*.32)+.3*Math.sin((x+z)*.56)));
}
for(let j=0;j<columns;j++)for(let i=0;i<columns;i++)
    heights[j*columns+i]=heightAtVertex(-half+i*grid,-half+j*grid);

// Match the triangle diagonal used by PlaneGeometry so collisions follow the rendered surface.
export function groundHeight(x,z) {
    const gx=T.MathUtils.clamp((x+half)/grid,0,divisions-.0001);
    const gz=T.MathUtils.clamp((z+half)/grid,0,divisions-.0001);
    const i=Math.floor(gx),j=Math.floor(gz),u=gx-i,v=gz-j,base=j*columns+i;
    const nw=heights[base],ne=heights[base+1],sw=heights[base+columns],se=heights[base+columns+1];
    if(u+v<=1)return nw+(ne-nw)*u+(sw-nw)*v;
    return se+(sw-se)*(1-u)+(ne-se)*(1-v);
}

export function createGround(material) {
    const geometry=new T.PlaneGeometry(terrainSize,terrainSize,divisions,divisions);
    geometry.rotateX(-Math.PI/2);
    const attr=geometry.attributes.position;
    const colors=[];
    const valley=new T.Color(0xb0bc8c), meadow=new T.Color(0x7f9676), stone=new T.Color(0xb6ad96);
    const verge=new T.Color(0xb4a585);
    const color=new T.Color();
    for(let n=0;n<attr.count;n++){
        const x=attr.getX(n),z=attr.getZ(n),y=heights[n];
        attr.setY(n,y);
        const patch=.44+.25*Math.sin(x*.029+Math.cos(z*.059))*Math.cos(z*.044)+
            .12*Math.sin(x*.114+z*.131);
        color.copy(valley).lerp(meadow,patch);
        const slope=Math.max(Math.abs(groundHeight(x+grid,z)-y),Math.abs(groundHeight(x,z+grid)-y))/grid;
        color.lerp(stone,Math.max(T.MathUtils.smoothstep(y,13,35)*.69,T.MathUtils.smoothstep(slope,.24,1.05)*.78));
        const vergeDistance=Math.min(Math.abs(z)-5.5,Math.abs(x-6)-5);
        const nearbyRoad=1-T.MathUtils.smoothstep(vergeDistance,.4,8.7);
        color.lerp(verge,nearbyRoad*(.40+.13*Math.sin(x*.071-z*.067))*(1-T.MathUtils.smoothstep(y,1,7)));
        colors.push(color.r,color.g,color.b);
    }
    geometry.setAttribute('color',new T.Float32BufferAttribute(colors,3));
    geometry.computeVertexNormals();
    const ground=new T.Mesh(geometry,material);
    ground.receiveShadow=true;
    return ground;
}

export function terrainObstructs(from,to) {
    const horizontal=Math.hypot(to.x-from.x,to.z-from.z);
    const steps=Math.ceil(horizontal/2.8);
    for(let i=1;i<steps;i++){
        const t=i/steps;
        const y=from.y+(to.y-from.y)*t;
        if(groundHeight(from.x+(to.x-from.x)*t,from.z+(to.z-from.z)*t)>y+.10)return true;
    }
    return false;
}

export const districts=[
    {name:'Serra do Farol',x:-138,z:-138},
    {name:'Morro da Vigia',x:137,z:-132},
    {name:'Pátio Norte',x:36,z:-135},
    {name:'Estação Oeste',x:-135,z:20},
    {name:'Estaleiro Leste',x:133,z:13},
    {name:'Terminal Costa de Ferro',x:0,z:4},
    {name:'Vale da Oficina',x:-125,z:109},
    {name:'Morro do Aço',x:143,z:143},
    {name:'Doca Sul',x:30,z:139},
];
