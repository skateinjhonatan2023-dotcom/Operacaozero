import * as T from 'three';

const waterVertex=`
varying vec3 seaPoint;
#include <fog_pars_vertex>
void main(){
    seaPoint=(modelMatrix*vec4(position,1.)).xyz;
    vec4 mvPosition=modelViewMatrix*vec4(position,1.);
    gl_Position=projectionMatrix*mvPosition;
    #include <fog_vertex>
}
`;
const waterFragment=`
uniform float time;
varying vec3 seaPoint;
#include <fog_pars_fragment>
void main(){
    vec2 p=seaPoint.xz;
    vec3 toCamera=normalize(cameraPosition-seaPoint);
    float w1=sin(p.x*.49+p.y*.25+time*1.75);
    float w2=sin(-p.x*.37+p.y*.73-time*1.86);
    float w3=sin(p.x*1.28+p.y*.94-time*2.4);
    vec3 n=normalize(vec3(.075*w1+.042*w3,1.,.085*w2-.041*w3));
    float fresnel=pow(1.-max(0.,dot(toCamera,n)),4.);
    float edge=max(abs(p.x),abs(p.y))-193.;
    float shelf=1.-smoothstep(2.,34.,edge);
    float foam=(1.-smoothstep(1.5,4.8,abs(edge-2.-w1*.65)))*(.45+max(0.,w2)*.40);
    vec3 deep=mix(vec3(.014,.104,.16),vec3(.028,.28,.27),shelf*.80);
    vec3 reflected=mix(deep,vec3(.28,.57,.68),.16+.60*fresnel);
    vec3 sunlight=normalize(vec3(-.41,.79,.38));
    float sparkle=pow(max(0.,dot(reflect(-sunlight,n),toCamera)),75.);
    gl_FragColor=vec4(reflected+foam*vec3(.10,.19,.15)+sparkle*vec3(.68,.54,.28),1.);
    #include <tonemapping_fragment>
    #include <colorspace_fragment>
    #include <fog_fragment>
}
`;

export function waterSurface() {
    const uniforms=T.UniformsUtils.merge([T.UniformsLib.fog,{time:{value:0}}]);
    return new T.ShaderMaterial({uniforms,vertexShader:waterVertex,fragmentShader:waterFragment,fog:true});
}

export function calmWaterSurface() {
    const fragmentShader=`
varying vec3 seaPoint;
#include <fog_pars_fragment>
void main(){
    float edge=max(abs(seaPoint.x),abs(seaPoint.z))-193.;
    float shallow=1.-smoothstep(3.,38.,edge);
    float washed=1.-smoothstep(1.,3.8,abs(edge-3.9));
    vec3 color=mix(vec3(.036,.110,.151),vec3(.082,.233,.243),shallow*.88);
    gl_FragColor=vec4(color + washed*vec3(.087,.13,.12),1.);
    #include <tonemapping_fragment>
    #include <colorspace_fragment>
    #include <fog_fragment>
}
`;
    return new T.ShaderMaterial({uniforms:T.UniformsUtils.clone(T.UniformsLib.fog),vertexShader:waterVertex,fragmentShader,fog:true});
}

export function shoreShelf() {
    const perimeter=180;
    const coordinates=[], colors=[], faces=[];
    const sand=new T.Color(0xae9870), wet=new T.Color(0x736c57), grass=new T.Color(0xa29366);
    for(let i=0;i<=perimeter;i++){
        const a=i/perimeter*Math.PI*2,c=Math.cos(a),s=Math.sin(a);
        const square=192/Math.max(Math.abs(c),Math.abs(s));
        const width=6+2.4*Math.sin(a*13)+1.8*Math.cos(a*23);
        for(const [r,y,color] of [[square-.018,-.04,grass],[square+width*.43,-.65,sand],[square+width,-1.83,wet]]){
            coordinates.push(c*r,y,s*r);
            colors.push(color.r,color.g,color.b);
        }
        if(i===perimeter)continue;
        const j=i*3;
        faces.push(j,j+3,j+1,j+1,j+3,j+4,j+1,j+4,j+2,j+2,j+4,j+5);
    }
    const mesh=new T.BufferGeometry();
    mesh.setAttribute('position',new T.Float32BufferAttribute(coordinates,3));
    mesh.setAttribute('color',new T.Float32BufferAttribute(colors,3));
    mesh.setIndex(faces);mesh.computeVertexNormals();
    return new T.Mesh(mesh,new T.MeshStandardMaterial({vertexColors:true,side:T.DoubleSide,roughness:.92}));
}

// World-space trim on bulkheads, using a tileable material so that longer façades do not stretch.
export function cladding() {
    const width=256,height=128;
    const data=new Uint8Array(width*height*4),normalData=new Uint8Array(data.length);
    let noise=1901;
    for(let y=0;y<height;y++)for(let x=0;x<width;x++){
        noise=(Math.imul(noise,1664525)+1013904223)>>>0;
        const flute=x%16;
        const groove=flute<2?-.23:flute===2?.09:flute>=14?-.14:0;
        const streak=.10*Math.sin(x*.72+Math.sin(y*.062))*(.5+.5*Math.sin(y*.046));
        const seam=y%64<3?-.16:0;
        const dust=.038*Math.sin(y*.28+x*.17)+(noise>>>28)*.004;
        const rivet=y%32>=4&&y%32<=6&&flute>=4&&flute<=6?.18:0;
        const value=Math.min(255,Math.round(226*(.86+groove+streak+seam+dust+rivet)));
        const i=(y*width+x)*4;
        data[i]=value;
        data[i+1]=value*(.99-.032*Math.max(0,streak));
        data[i+2]=value*(.97-.10*Math.max(0,streak));
        data[i+3]=255;
        const slope=flute>=1&&flute<=3?-.62:flute>=13?.62:0;
        normalData[i]=128+110*slope;
        normalData[i+1]=y%64===2?166:128;
        normalData[i+2]=128+125*Math.sqrt(1-slope*slope);
        normalData[i+3]=255;
    }
    function texture(bytes,colorSpace){
        const map=new T.DataTexture(bytes,width,height,T.RGBAFormat);
        map.colorSpace=colorSpace;
        map.wrapS=map.wrapT=T.RepeatWrapping;
        map.repeat.set(1,1);
        map.magFilter=T.LinearFilter;
        map.minFilter=T.LinearMipmapLinearFilter;
        map.generateMipmaps=true;map.needsUpdate=true;
        return map;
    }
    return {color:texture(data,T.SRGBColorSpace),normal:texture(normalData,T.NoColorSpace)};
}
