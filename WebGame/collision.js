const cellSize=12;
const noBoxes=[];
const axes=['x','y','z'];
const cell=value=>Math.floor(value/cellSize);
const key=(x,z)=>x+z*4096;

function boxesBetween(box,visit){
    const x0=cell(box.x-box.w*.5),x1=cell(box.x+box.w*.5);
    const z0=cell(box.z-box.d*.5),z1=cell(box.z+box.d*.5);
    for(let z=z0;z<=z1;z++)for(let x=x0;x<=x1;x++)visit(key(x,z));
}

function boxHit(box,o,d,limit) {
    let lo=0,hi=limit;
    for(const axis of axes){
        const base=axis==='y'?box.bottom:box[axis]-(axis==='x'?box.w:box.d)*.5;
        const top=axis==='y'?box.top:box[axis]+(axis==='x'?box.w:box.d)*.5;
        if(Math.abs(d[axis])<1e-9){
            if(o[axis]<base||o[axis]>top)return limit;
            continue;
        }
        const a=(base-o[axis])/d[axis],b=(top-o[axis])/d[axis];
        lo=Math.max(lo,Math.min(a,b));hi=Math.min(hi,Math.max(a,b));
        if(lo>hi)return limit;
    }
    return lo;
}

export function collisionIndex(obstacles) {
    const cells=new Map();
    function add(box){
        boxesBetween(box,id=>{
            if(!cells.has(id))cells.set(id,[]);
            cells.get(id).push(box);
        });
    }
    function remove(box){
        boxesBetween(box,id=>{
            const items=cells.get(id);
            const offset=items?.indexOf(box)??-1;
            if(offset<0)return;
            items.splice(offset,1);
            if(!items.length)cells.delete(id);
        });
    }
    for(const box of obstacles)add(box);
    return {
        add,remove,
        near(x,z,r){
            const left=cell(x-r),right=cell(x+r),back=cell(z-r),front=cell(z+r);
            if(left===right&&back===front)return cells.get(key(left,back))??noBoxes;
            const result=[];
            for(let j=back;j<=front;j++)for(let i=left;i<=right;i++){
                const items=cells.get(key(i,j));
                if(items)result.push(...items);
            }
            return result;
        },
        rayDistance(o,d,limit){
            let x=cell(o.x),z=cell(o.z),best=limit;
            const dx=Math.abs(d.x)<1e-9?0:Math.sign(d.x);
            const dz=Math.abs(d.z)<1e-9?0:Math.sign(d.z);
            let nextX=dx?((x+(dx>0?1:0))*cellSize-o.x)/d.x:Infinity;
            let nextZ=dz?((z+(dz>0?1:0))*cellSize-o.z)/d.z:Infinity;
            const stepX=dx?cellSize/Math.abs(d.x):Infinity;
            const stepZ=dz?cellSize/Math.abs(d.z):Infinity;
            for(;;){
                for(const box of cells.get(key(x,z))??noBoxes)best=boxHit(box,o,d,best);
                if(Math.min(nextX,nextZ)>=best)return best;
                if(nextX<nextZ){x+=dx;nextX+=stepX;}
                else{z+=dz;nextZ+=stepZ;}
            }
        },
    };
}
