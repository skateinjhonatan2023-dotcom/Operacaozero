const el = id => document.getElementById(id);
const slot = 'operation-zero-costa-settings-v2';
const defaults = {
    stick:[13,71,116], fire:[90,66,76], aim:[82,48,51], jump:[79,68,55],
    crouch:[87,83,55], sprint:[24,57,48], reload:[73,84,51], heal:[67,67,49], ability:[59,81,57], throw:[70,48,50],
};

export const presets = {
    low:{name:'Baixa',scale:.74,minScale:.75,pixels:350000,outputScale:.93,outputPixels:510000,studioScale:.90,edgeStrength:.78,filter:false,
        shadowSize:0,shadowRange:40,shadowInterval:.45,grass:.28,normals:false,waves:false,hd:false,anisotropy:2,
        scenery:116,treeDetail:0,grassRange:27,bots:90,waiting:7},
    medium:{name:'Média',scale:.93,minScale:.75,pixels:710000,outputScale:1.13,outputPixels:910000,studioScale:1.12,edgeStrength:.78,filter:true,
        shadowSize:1024,shadowRange:43,shadowInterval:.35,grass:.52,normals:true,waves:false,hd:false,anisotropy:4,
        scenery:158,treeDetail:16,grassRange:40,bots:118,waiting:9},
    high:{name:'Alta',scale:1.13,minScale:.73,pixels:1250000,outputScale:1.29,outputPixels:1450000,studioScale:1.27,edgeStrength:.74,filter:true,
        shadowSize:1536,shadowRange:54,shadowInterval:.29,grass:.79,normals:true,waves:true,hd:true,anisotropy:8,
        scenery:214,treeDetail:35,grassRange:52,bots:150,waiting:12},
    ultra:{name:'Ultra',scale:1.42,minScale:.70,pixels:1900000,outputScale:1.55,outputPixels:2073600,studioScale:1.41,edgeStrength:.68,filter:true,
        shadowSize:2048,shadowRange:65,shadowInterval:.22,grass:1,normals:true,waves:true,hd:true,anisotropy:12,
        scenery:280,treeDetail:52,grassRange:65,bots:175,waiting:14},
};

function standardLayout() {
    return Object.fromEntries(Object.entries(defaults).map(([id,[x,y,size]])=>[id,{x,y,size,opacity:1}]));
}

function number(value, min, max, fallback) {
    return typeof value==='number'&&Number.isFinite(value) ? Math.max(min,Math.min(max,value)) : fallback;
}

export function setupSettings({change, pause, resume, lobby}) {
    const layout=standardLayout();
    let saved={};
    try {saved=JSON.parse(localStorage.getItem(slot))||{};} catch {}
    for(const [id,item] of Object.entries(layout)) {
        const value=saved.layout?.[id];
        if(!value)continue;
        item.x=number(value.x,2,98,item.x);item.y=number(value.y,2,98,item.y);
        item.size=number(value.size,defaults[id][2]*.7,defaults[id][2]*1.5,item.size);
        item.opacity=number(value.opacity,.35,1,1);
    }
    const sensitivity=number(saved.sensitivity,.45,2.25,1);
    const settings={
        quality:Object.hasOwn(presets,saved.quality)?saved.quality:matchMedia('(pointer: coarse)').matches?'low':'medium',
        fps:saved.fps===60?60:30,
        adaptive:saved.adaptive!==false,
        sensitivity,
        adsSensitivity:number(saved.adsSensitivity,.45,2.25,sensitivity),
        aimMode:saved.aimMode==='hold'?'hold':'toggle',
        layout,
    };
    let draft, selected='fire', pointer=null, fromMatch=false;
    const buttons=Object.keys(defaults).map(id=>[id,el(id)]);
    const preview=new Map();

    function draw(button, style) {
        const factor=Math.max(.61,Math.min(1,innerHeight/480,innerWidth/740));
        const size=Math.round(style.size*factor);
        const x=Math.max(size*.5+2,Math.min(innerWidth-size*.5-2,innerWidth*style.x*.01));
        const y=Math.max(size*.5+2,Math.min(innerHeight-size*.5-2,innerHeight*style.y*.01));
        for(const [name,value] of Object.entries({x:x+'px',y:y+'px',size:size+'px',opacity:style.opacity}))
            button.style.setProperty('--'+name,value);
    }
    function apply() {
        for(const [id,button] of buttons)draw(button,settings.layout[id]);
        el('lobby-config').textContent=presets[settings.quality].name+' · '+settings.fps+' FPS';
        change(settings);
    }
    function save() {
        try {localStorage.setItem(slot,JSON.stringify(settings));} catch {}
        apply();
    }
    function select(id) {
        selected=id;
        const item=draft[id];
        el('control-name').textContent=el(id).getAttribute('aria-label')+' · tamanho';
        el('control-size').value=Math.round(item.size/defaults[id][2]*100);
        el('control-opacity').value=Math.round(item.opacity*100);
        for(const [key,button] of preview)button.classList.toggle('selected',key===id);
    }
    function renderDraft(){for(const [id,button] of preview)draw(button,draft[id]);}
    for(const [id,button] of buttons) {
        const clone=button.cloneNode(true);
        clone.removeAttribute('id');
        for(const child of clone.querySelectorAll('[id]'))child.removeAttribute('id');
        clone.dataset.control=id;
        preview.set(id,clone);
        el('editor-controls').append(clone);
        clone.addEventListener('click',()=>select(id));
        clone.addEventListener('pointerdown',e=>{
            if(pointer)return;
            select(id);
            pointer={id:e.pointerId,x:e.clientX,y:e.clientY,left:draft[id].x,top:draft[id].y};
            clone.setPointerCapture(e.pointerId);
        });
        clone.addEventListener('pointermove',e=>{
            if(pointer?.id!==e.pointerId)return;
            draft[id].x=number(pointer.left+(e.clientX-pointer.x)/innerWidth*100,2,98,50);
            draft[id].y=number(pointer.top+(e.clientY-pointer.y)/innerHeight*100,2,98,50);
            draw(clone,draft[id]);
        });
        for(const type of ['pointerup','pointercancel','lostpointercapture'])
            clone.addEventListener(type,e=>{if(pointer?.id===e.pointerId)pointer=null;});
    }
    function open(match) {
        fromMatch=match;
        if(match)pause();
        el('settings').hidden=false;
        el('lobby-back').hidden=!match;
    }
    function close() {
        el('settings').hidden=true;
        if(fromMatch)resume();
    }
    function editor(open) {
        el('layout-editor').hidden=!open;
        el('settings').hidden=open;
        pointer=null;
        if(open){draft=structuredClone(settings.layout);renderDraft();select(selected);}
    }
    el('settings-open').onclick=()=>open(false);
    el('pause').onclick=()=>open(true);
    el('settings-close').onclick=close;
    el('lobby-back').onclick=()=>{el('settings').hidden=true;lobby();};
    el('layout-open').onclick=()=>editor(true);
    el('layout-cancel').onclick=()=>editor(false);
    el('layout-save').onclick=()=>{settings.layout=draft;save();editor(false);};
    el('layout-reset').onclick=()=>{draft=standardLayout();renderDraft();select(selected);};
    el('control-size').oninput=e=>{draft[selected].size=defaults[selected][2]*Number(e.target.value)/100;renderDraft();};
    el('control-opacity').oninput=e=>{draft[selected].opacity=Number(e.target.value)/100;renderDraft();};
    el('quality').value=settings.quality;
    el('quality').onchange=e=>{settings.quality=e.target.value;save();};
    el('fps').value=settings.fps;
    el('fps').onchange=e=>{settings.fps=Number(e.target.value);save();};
    el('adaptive-resolution').checked=settings.adaptive;
    el('adaptive-resolution').onchange=e=>{settings.adaptive=e.target.checked;save();};
    el('aim-mode').value=settings.aimMode;
    el('aim-mode').onchange=e=>{settings.aimMode=e.target.value;save();};
    for(const [id,key] of [['sensitivity','sensitivity'],['ads-sensitivity','adsSensitivity']]){
        const slider=el(id),value=el(id+'-value');
        slider.value=settings[key];
        value.textContent=settings[key].toFixed(2)+'×';
        slider.oninput=e=>{
            settings[key]=Number(e.target.value);
            value.textContent=settings[key].toFixed(2)+'×';
            save();
        };
    }
    addEventListener('resize',()=>{apply();if(!el('layout-editor').hidden)renderDraft();});
    apply();
    return settings;
}
