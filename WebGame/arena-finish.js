import * as T from 'three';
import { mergeGeometries } from 'three/addons/utils/BufferGeometryUtils.js';

export const haze = 0x9eacb4;

export function makeSky(panorama) {
    const geometry=new T.SphereGeometry(540,32,18);
    if(panorama){
        panorama.wrapT=T.ClampToEdgeWrapping;
        const sky=new T.Mesh(geometry,new T.MeshBasicMaterial({
            map:panorama,side:T.BackSide,depthWrite:false,fog:false,
        }));
        sky.rotation.y=Math.PI;
        sky.renderOrder=-99;
        return sky;
    }
    const points=geometry.attributes.position,colors=new Float32Array(points.count*3);
    const horizon=new T.Color(haze),zenith=new T.Color(0x4c7795),warm=new T.Color(0xf4d2a6),color=new T.Color();
    const sunward=new T.Vector3(-.51,.60,.60).normalize(),direction=new T.Vector3();
    for(let i=0;i<points.count;i++){
        direction.fromBufferAttribute(points,i).normalize();
        const up=Math.max(0,direction.y);
        color.copy(horizon).lerp(zenith,Math.pow(up,.72));
        const glimmer=Math.pow(Math.max(0,direction.dot(sunward)),13)*.62+
            .13*Math.exp(-14*up)*Math.max(0,direction.dot(sunward));
        color.lerp(warm,glimmer).toArray(colors,i*3);
    }
    geometry.setAttribute('color',new T.BufferAttribute(colors,3));
    const sky=new T.Mesh(geometry,new T.MeshBasicMaterial({
        toneMapped:false,vertexColors:true,side:T.BackSide,depthWrite:false,fog:false,
    }));
    sky.renderOrder=-99;
    return sky;
}

export function wearOnGeometry(geometry,object) {
    const p=geometry.attributes.position,colors=new Float32Array(p.count*3);
    const contrast=Math.min(.24,.078*object.scale.y);
    const patina=.94+.035*Math.cos(object.position.x*.119+object.position.z*.163);
    for(let i=0;i<p.count;i++){
        const floor=1-contrast*(.5-p.getY(i));
        const shade=patina*floor;
        colors[i*3]=shade;
        colors[i*3+1]=shade;
        colors[i*3+2]=shade*(.975+.025*(p.getY(i)+.5));
    }
    geometry.setAttribute('color',new T.BufferAttribute(colors,3));
}

export function windowReflections() {
    const width=128,height=64,data=new Uint8Array(width*height*4);
    for(let y=0;y<height;y++)for(let x=0;x<width;x++){
        const elevation=y/(height-1);
        const glare=Math.exp(-Math.pow((x/width+elevation*.7-.78)/.17,2));
        const horizon=Math.exp(-Math.pow((elevation-.46)/.09,2));
        const border=x<3||x>=width-3||y<2||y>=height-2?.58:1;
        const light=(57+54*elevation+31*glare+10*horizon)*border;
        const i=(y*width+x)*4;
        data[i]=light*.68;data[i+1]=light*.88;data[i+2]=light;data[i+3]=255;
    }
    const texture=new T.DataTexture(data,width,height,T.RGBAFormat);
    texture.colorSpace=T.SRGBColorSpace;
    texture.magFilter=T.LinearFilter;
    texture.minFilter=T.LinearMipmapLinearFilter;
    texture.generateMipmaps=true;texture.needsUpdate=true;
    return texture;
}

export function warehouseSigns(points) {
    if(typeof document==='undefined')return null;
    const atlas=document.createElement('canvas');atlas.width=512;atlas.height=256;
    const ctx=atlas.getContext('2d'),pieces=[];
    for(const [i,{x,z,designation}] of points.entries()){
        const column=i%4,row=Math.floor(i/4),left=column*128,top=row*64;
        ctx.fillStyle='#1e292e';ctx.fillRect(left,top,128,64);
        ctx.fillStyle='#d7aa67';ctx.fillRect(left+5,top+5,118,3);ctx.fillRect(left+5,top+56,118,3);
        ctx.fillStyle='#e6d9bf';ctx.font='bold 30px sans-serif';ctx.textAlign='center';ctx.textBaseline='middle';
        ctx.fillText('A'+String(i+1).padStart(2,'0'),left+64,top+(designation?26:34));
        if(designation){
            ctx.font='bold 13px sans-serif';
            ctx.fillText(designation,left+64,top+46,112);
        }
        const tile=new T.PlaneGeometry(1.98,.99);
        const uv=tile.attributes.uv;
        for(let k=0;k<uv.count;k++)uv.setXY(k,(column+.015+uv.getX(k)*.97)*.25,
            (3-row+.02+uv.getY(k)*.96)*.25);
        tile.rotateY(Math.PI);tile.translate(x,3.89,z);
        pieces.push(tile);
    }
    const texture=new T.CanvasTexture(atlas);
    texture.colorSpace=T.SRGBColorSpace;texture.anisotropy=2;
    const signs=new T.Mesh(mergeGeometries(pieces),new T.MeshStandardMaterial({map:texture,roughness:.83}));
    signs.receiveShadow=true;
    for(const piece of pieces)piece.dispose();
    return signs;
}
