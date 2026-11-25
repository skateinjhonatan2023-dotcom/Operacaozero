import * as T from 'three';

const tileSize=60;

function batchedModel(template,points,castShadow,nearMaterials) {
    template.updateMatrixWorld(true);
    const batch=new T.Group();
    const matrix=new T.Matrix4();
    const dummy=new T.Object3D();
    template.traverse(source=>{
        if(!source.isMesh)return;
        const material=nearMaterials?.get(source.material.name)??source.material;
        if(material.transparent&&material.map){
            material.transparent=false;material.depthWrite=true;material.alphaTest=.42;
            material.needsUpdate=true;
        }
        const mesh=new T.InstancedMesh(source.geometry,material,points.length);
        points.forEach(({x,y,z,scale,yaw},i)=>{
            dummy.position.set(x,y,z);
            dummy.rotation.y=yaw;dummy.scale.setScalar(scale);dummy.updateMatrix();
            mesh.setMatrixAt(i,matrix.multiplyMatrices(dummy.matrix,source.matrixWorld));
        });
        mesh.castShadow=castShadow;
        mesh.receiveShadow=true;
        mesh.computeBoundingSphere();
        batch.add(mesh);
    });
    return batch;
}

export function placeScenery(scene,points,primary,distant,casts=false) {
    const tiles=new Map();
    for(const point of points){
        const x=Math.floor(point.x/tileSize)*tileSize,z=Math.floor(point.z/tileSize)*tileSize;
        const key=x+':'+z;
        if(!tiles.has(key))tiles.set(key,{x,z,points:[]});
        tiles.get(key).points.push(point);
    }
    const materials=new Map();
    primary.traverse(node=>{if(node.isMesh)materials.set(node.material.name,node.material);});
    const batches=[];
    for(const {x,z,points:items} of tiles.values()){
        const near=distant?batchedModel(primary,items,casts):null;
        const far=batchedModel(distant??primary,items,false,distant?materials:null);
        scene.add(far);
        if(near){scene.add(near);near.visible=false;}
        batches.push({x,z,near,far});
    }
    return (camera,focus,preset)=>{
        for(const {x,z,near,far} of batches){
            const dx=Math.max(x-camera.x,0,camera.x-x-tileSize);
            const dz=Math.max(z-camera.z,0,camera.z-z-tileSize);
            const y=camera.y>46?camera.y-46:0;
            const distance=dx*dx+dz*dz+y*y;
            const visible=distance<preset.scenery*preset.scenery;
            const px=Math.max(x-focus.x,0,focus.x-x-tileSize);
            const pz=Math.max(z-focus.z,0,focus.z-z-tileSize);
            const detail=near&&visible&&preset.treeDetail>0&&focus.y<50&&px*px+pz*pz<preset.treeDetail*preset.treeDetail;
            far.visible=visible&&!detail;
            if(near)near.visible=Boolean(detail);
        }
    };
}
