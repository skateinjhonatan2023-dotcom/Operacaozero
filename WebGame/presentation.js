import * as T from 'three';

const vertexShader=`
varying vec2 imageUV;
void main(){
    imageUV=uv;
    gl_Position=vec4(position.xy,0.,1.);
}
`;

const fragmentShader=`
uniform sampler2D frameColor;
uniform vec2 texel;
uniform float edgeStrength;
varying vec2 imageUV;

float lightness(vec3 color){return dot(color,vec3(.2126,.7152,.0722));}

void main(){
    vec3 center=texture2D(frameColor,imageUV).rgb;
    vec2 halfPixel=texel*.5;
    vec3 cornerNW=texture2D(frameColor,imageUV-halfPixel).rgb;
    vec3 cornerNE=texture2D(frameColor,imageUV+vec2(halfPixel.x,-halfPixel.y)).rgb;
    vec3 cornerSW=texture2D(frameColor,imageUV+vec2(-halfPixel.x,halfPixel.y)).rgb;
    vec3 cornerSE=texture2D(frameColor,imageUV+halfPixel).rgb;
    float nw=lightness(cornerNW),ne=lightness(cornerNE);
    float sw=lightness(cornerSW),se=lightness(cornerSE);
    float mid=lightness(center);
    float minimum=min(mid,min(min(nw,ne),min(sw,se)));
    float maximum=max(mid,max(max(nw,ne),max(sw,se)));
    vec3 color=center;
    if(maximum-minimum>max(.025,maximum*.10)){
        vec2 tangent=vec2(sw+se-nw-ne,nw+sw-ne-se);
        float damp=max((nw+ne+sw+se)*.045,.009);
        tangent=clamp(tangent/(min(abs(tangent.x),abs(tangent.y))+damp),-2.2,2.2)*texel;
        vec3 first=texture2D(frameColor,imageUV+tangent/3.).rgb;
        vec3 second=texture2D(frameColor,imageUV-tangent/3.).rgb;
        color=mix(center,(first+second)*.5,edgeStrength);
    }else color=max(vec3(0.),center+.24*(center-(cornerNW+cornerNE+cornerSW+cornerSE)*.25));
    gl_FragColor=vec4(color,1.);
    #include <tonemapping_fragment>
    #include <colorspace_fragment>
}
`;

export function adaptiveResolution(){
    let scale=1,last=null,windowTime=0,frames=0,stableTime=0;
    function resetSamples(){last=null;windowTime=0;frames=0;stableTime=0;}
    return {
        get scale(){return scale;},
        reset(){scale=1;resetSamples();},
        idle:resetSamples,
        sample(now,interval,minScale){
            const duration=last===null?0:now-last;
            last=now;
            if(duration<=0)return false;
            if(duration>2000){resetSamples();return false;}
            windowTime+=duration;frames++;
            if(windowTime<970||frames<3)return false;
            const average=windowTime/frames;
            let next=scale;
            if(average>interval*1.18){
                next=Math.max(minScale,scale-.085);
                stableTime=0;
            }else if(average<interval*1.045){
                stableTime+=windowTime;
                if(stableTime>=2600){next=Math.min(1,scale+.045);stableTime=0;}
            }else stableTime=0;
            windowTime=0;frames=0;
            if(next===scale)return false;
            scale=next;
            return true;
        },
    };
}

export function createPresentation(renderer){
    const supported=renderer.extensions.has('EXT_color_buffer_float')||
        renderer.extensions.has('EXT_color_buffer_half_float');
    const target=supported?new T.WebGLRenderTarget(1,1,{
        type:T.HalfFloatType,minFilter:T.LinearFilter,magFilter:T.LinearFilter,stencilBuffer:false,
    }):null;
    const screen=new T.Scene(),view=new T.Camera();
    let filter=null;
    if(target){
        filter=new T.ShaderMaterial({
            uniforms:{frameColor:{value:target.texture},texel:{value:new T.Vector2(1,1)},edgeStrength:{value:.7}},
            vertexShader,fragmentShader,depthTest:false,depthWrite:false,blending:T.NoBlending,
        });
        // One triangle covers the viewport, with no seam between triangles.
        const triangle=new T.BufferGeometry();
        triangle.setAttribute('position',new T.Float32BufferAttribute([-1,-1,0,3,-1,0,-1,3,0],3));
        triangle.setAttribute('uv',new T.Float32BufferAttribute([0,0,2,0,0,2],2));
        const image=new T.Mesh(triangle,filter);image.frustumCulled=false;screen.add(image);
    }
    const resolution=new T.Vector2();
    let screenWidth=0,screenHeight=0,screenScale=0,filtered=false;
    let width=1,height=1;
    renderer.info.autoReset=false;
    return {
        get width(){return width;},
        get height(){return height;},
        get filtered(){return filtered;},
        configure(cssWidth,cssHeight,density,profile,studio,adaptive){
            const area=Math.max(1,cssWidth*cssHeight);
            const dimensionRatio=renderer.capabilities.maxTextureSize/Math.max(cssWidth,cssHeight,1);
            const ceiling=studio?910000:profile.pixels;
            const desired=studio?profile.studioScale:profile.scale;
            const ratio=Math.min(desired,Math.sqrt(ceiling/area),dimensionRatio)*adaptive;
            const useFilter=Boolean(target&&(studio||profile.filter&&adaptive>=.89));
            if(filtered&&!useFilter)target.dispose();
            filtered=useFilter;
            const output=filtered?Math.min(density,profile.outputScale,Math.sqrt(profile.outputPixels/area),dimensionRatio)*Math.sqrt(adaptive):ratio;
            if(screenWidth!==cssWidth||screenHeight!==cssHeight||screenScale!==output){
                // Both size setters resize the canvas. Change the pixel ratio once, then use a physical size.
                renderer.setDrawingBufferSize(cssWidth,cssHeight,output);
                screenWidth=cssWidth;screenHeight=cssHeight;screenScale=output;
            }
            if(!filtered){
                renderer.getDrawingBufferSize(resolution);
                width=resolution.x;height=resolution.y;
                return;
            }
            width=Math.max(1,Math.round(cssWidth*ratio));
            height=Math.max(1,Math.round(cssHeight*ratio));
            if(width!==target.width||height!==target.height)target.setSize(width,height);
            filter.uniforms.texel.value.set(1/width,1/height);
            filter.uniforms.edgeStrength.value=profile.edgeStrength;
        },
        render(scene,camera){
            renderer.info.reset();
            if(filtered){
                renderer.setRenderTarget(target);
                renderer.render(scene,camera);
                renderer.setRenderTarget(null);
                renderer.render(screen,view);
            }else renderer.render(scene,camera);
        },
    };
}
