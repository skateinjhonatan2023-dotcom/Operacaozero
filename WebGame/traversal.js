import { groundHeight } from './terrain.js';

const radius=.36;
const standingHeight=1.8;

function overlaps(o,x,z,r) {
    return Math.abs(x-o.x)<o.w*.5+r&&Math.abs(z-o.z)<o.d*.5+r;
}

export function freeAt(obstacles,boundary,x,z,y,height=standingHeight,r=radius) {
    return Math.abs(x)<boundary&&Math.abs(z)<boundary&&y>=groundHeight(x,z)-.015&&!(obstacles.near?.(x,z,r)??obstacles).some(o=>
        y<o.top-1e-6&&y+height>o.bottom+1e-6&&overlaps(o,x,z,r));
}

export function floorAt(obstacles,x,z,maxY,r=radius) {
    let floor=groundHeight(x,z);
    for(const o of obstacles.near?.(x,z,r)??obstacles)if(o.top<=maxY&&o.top>floor&&overlaps(o,x,z,r))floor=o.top;
    return floor;
}

export function stepTo(obstacles,boundary,p,x,z,height,stepHeight) {
    let y=p.y;
    if(stepHeight>0){
        const floor=floorAt(obstacles,x,z,p.y+stepHeight);
        if(floor>p.y+stepHeight+.00001)return false;
        if(floor>=p.y-stepHeight)y=floor;
    }
    if(!freeAt(obstacles,boundary,x,z,y,height))return false;
    p.x=x;p.y=y;p.z=z;
    return true;
}

export function vaultPoint(route,t,out) {
    const progress=Math.min(1,Math.max(0,(t-.23)/.56));
    out.x=route.x+(route.endX-route.x)*progress;
    out.z=route.z+(route.endZ-route.z)*progress;
    if(t<.23)out.y=route.y+(route.apex-route.y)*t/.23;
    else if(t<=.79)out.y=route.apex;
    else out.y=route.apex+(route.endY-route.apex)*Math.min(1,(t-.79)/.21);
    return out;
}

export function findVault(obstacles,boundary,p,dx,dz) {
    const sample={};
    for(let distance=.17;distance<=1.04;distance+=.17) {
        const x=p.x+dx*distance,z=p.z+dz*distance;
        if(freeAt(obstacles,boundary,x,z,p.y))continue;
        const top=floorAt(obstacles,x,z,p.y+1.46);
        if(top-p.y<.38||!freeAt(obstacles,boundary,x,z,top+.025))return null;
        for(let reach=2.45;reach>=distance;reach-=.19) {
            const endX=p.x+dx*reach,endZ=p.z+dz*reach;
            const endY=floorAt(obstacles,endX,endZ,top+.03);
            const route={x:p.x,y:p.y,z:p.z,endX,endY,endZ,apex:top+.19,elapsed:0};
            let valid=true;
            for(let i=1;i<=16;i++) {
                vaultPoint(route,i/16,sample);
                if(!freeAt(obstacles,boundary,sample.x,sample.z,sample.y)){valid=false;break;}
            }
            if(valid)return route;
        }
        return null;
    }
    return null;
}
