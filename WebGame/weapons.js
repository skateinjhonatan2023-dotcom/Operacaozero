import * as T from 'three';

export const families = {
    rifle:{label:'Rifles',range:105,spread:.032,recoil:.012,stock:1,mobility:1},
    smg:{label:'Submetralhadoras',range:66,spread:.049,recoil:.009,stock:.85,mobility:1.08},
    shotgun:{label:'Escopetas',range:33,spread:.118,recoil:.034,stock:.92,mobility:1},
    marksman:{label:'Precisão semiautomática',range:139,spread:.022,recoil:.024,stock:1.07,mobility:.94},
    sniper:{label:'Snipers',range:178,spread:.015,recoil:.039,stock:1.12,mobility:.87},
    lmg:{label:'Metralhadoras',range:114,spread:.048,recoil:.016,stock:1.1,mobility:.85},
    pistol:{label:'Pistolas',range:53,spread:.044,recoil:.017,stock:.58,mobility:1.1},
    launcher:{label:'Lançadores',range:70,spread:.029,recoil:.031,stock:1,mobility:.91},
    bow:{label:'Arcos e bestas',range:89,spread:.015,recoil:.01,stock:1,mobility:.98},
    melee:{label:'Corpo a corpo',range:2.6,spread:0,recoil:0,stock:.65,mobility:1.15},
    grenade:{label:'Arremessáveis',range:26,spread:.035,recoil:0,stock:.62,mobility:1.03},
};

const rows = {
    rifle:[
        ['M4A1',30,32,.137,1.32],['AK',30,39,.158,1.63],['SCAR',30,31,.124,1.44],
        ['FAMAS',30,27,.076,1.56,0,3],['GROZA',30,37,.129,1.74],['AUG',35,32,.123,1.7],
        ['AN94',30,37,.157,1.68],['XM8',25,35,.145,1.46],['M14',15,51,.25,1.58],
        ['PARAFAL',30,46,.196,1.76],['AC80',10,54,.275,1.64],['M7',30,34,.132,1.58],
        ['G36',30,33,.13,1.6],['Kingfisher',22,30,.081,1.42,1,3],['Plasma',35,31,.101,2.3],['Shield Gun',20,25,.175,1.7],
    ],
    smg:[
        ['MP40',20,23,.071,1.21],['UMP',30,26,.094,1.34],['MP5',30,24,.083,1.28],
        ['VSS',15,31,.127,1.52],['P90',50,22,.083,1.82],['CG15',20,27,.095,1.43],
        ['Thompson',42,23,.086,1.7],['Vector',25,21,.064,1.24],['MAC10',30,22,.069,1.35],
        ['Bizon',36,25,.095,1.61],['Skorp',30,23,.071,1.45],
    ],
    shotgun:[
        ['M1014',6,13,.43,1.83,7],['M1887',2,17,.46,1.44,7],['SPAS12',5,16,.63,1.67,7],
        ['MAG-7',8,10,.31,1.88,8],['Charge Buster',3,14,.61,1.65,8],
        ['Trogon',9,8,.11,2.04,6,3],['M590',1,20,.74,1.34,8],
    ],
    marksman:[
        ['SKS',10,54,.29,1.66],['SVD',10,63,.36,1.87],['Woodpecker',12,56,.28,1.62],
        ['Winchester',12,59,.37,1.86],
    ],
    sniper:[
        ['AWM',5,100,1.04,2.3],['Kar98k',5,88,.93,2.16],['M82B',8,98,1.08,2.42],
        ['M24',5,91,.84,1.96],['Healing Sniper',5,22,.85,2.34],['VSK94',26,51,.35,1.86],['Hawk',2,76,.43,1.91],
    ],
    lmg:[['M249',100,26,.097,3.0],['M60',60,30,.116,2.74],['Kord',80,31,.119,3.1],['RPK',75,29,.108,2.8],['Gatling',120,22,.071,3.7]],
    pistol:[
        ['G18',15,23,.177,1.14],['USP',12,29,.205,1.16],['Desert Eagle',7,56,.33,1.48],
        ['M500',5,61,.39,1.69],['M1917',12,34,.25,1.55],['Mini Uzi',18,21,.093,1.22],
        ['Treatment Gun',12,21,.25,1.33],['Treatment Laser Gun',30,15,.137,1.53],
        ['USP-2',14,24,.133,1.31],['M1873',2,11,.40,1.39,6],['Lança-chamas',55,8,.041,2.3],
        ['Hand Cannon',1,58,.9,1.8,1,0,3.5],['FGL-24',2,61,.66,1.67,1,0,4.2],
    ],
    launcher:[['M79',1,88,1.04,2.0,1,0,5],['MGL140',5,64,.64,2.75,1,0,4.4],['RGS50',3,76,.85,2.33,1,0,5.3]],
    bow:[['Crossbow',1,75,.95,1.9],['Compound Bow',1,65,.85,1.5]],
    melee:[['Faca',0,42,.43,0],['Panela',0,57,.57,0],['Parang',0,52,.48,0],['Taco',0,49,.52,0],['Katana',0,62,.62,0],['Foice',0,66,.72,0]],
    grenade:[['Granada',1,82,.87,1.8,1,0,6.5],['Corrosiva',1,48,1,1.9,1,0,5.5],
        ['Barreira portátil',1,0,.87,1.7],['Granada incendiária',1,36,.90,1.8,1,0,4.5],
        ['Granada de gelo',1,15,.86,1.7,1,0,5.3],['Granada de fumaça',1,0,.85,1.6],
        ['Granada de sonar',1,0,.87,1.6]],
};

export const arsenal=Object.entries(rows).flatMap(([type,items])=>items.map(
    ([name,capacity,damage,interval,reload,pellets=1,burst=0,blast=0],i)=>({
        name,type,capacity,damage,interval,reload,pellets:pellets||1,burst,blast,variant:i,
        ...families[type],
        automatic:(!burst&&['rifle','smg','lmg'].includes(type)&&!['M14','AC80'].includes(name))||name==='Lança-chamas',
        aid:name.startsWith('Treatment')||name==='Healing Sniper',
        utility:{'Barreira portátil':'wall','Corrosiva':'melter','Granada incendiária':'fire',
            'Granada de gelo':'freeze','Granada de fumaça':'smoke','Granada de sonar':'sonar'}[name],
        ...(name==='Lança-chamas'?{range:14,spread:.088}:name==='M1873'?{range:24}:{}),
        shield:name==='Shield Gun',
    }),
));
export const standardWeapon=arsenal[0];

const finishes=new Map();
const colorBands=['shotgun','bow','rifle','marksman','grenade','launcher','pistol','smg','sniper','melee','lmg'];
export function weaponSeed(name) {
    let hash=2166136261;
    for(const char of name)hash=Math.imul(hash^char.codePointAt(0),16777619);
    return hash>>>0;
}
export function finishHue(profile) {
    const band=colorBands.indexOf(profile.type);
    const shade=(profile.variant+.5)/rows[profile.type].length;
    return (band+.09+.82*shade)/colorBands.length;
}
function finishMaterials(profile,skin) {
    const key=profile.name+'|'+skin;
    if(finishes.has(key))return finishes.get(key);
    const hue=skin==='lagoon'?.50:skin==='ember'?.045:skin==='aurora'?.76:finishHue(profile);
    const tone=(offset,sat,light)=>new T.Color().setHSL((hue+offset+1)%1,sat,light);
    const make=(color,metalness,roughness)=>new T.MeshStandardMaterial({color,metalness,roughness});
    const set={
        metal:make(tone(0,.36,.32),.55,.48),
        rubber:make(tone(.06,.24,.16),.04,.84),
        wood:make(tone(-.04,.48,.30),.09,.74),
        accent:make(tone(.04,.78,.51),.35,.49),
    };
    finishes.set(key,set);
    return set;
}
const rifleFinishes=new Map();
const cube=new T.BoxGeometry(1,1,1);
const tube=new T.CylinderGeometry(1,1,1,9);
const sphere=new T.IcosahedronGeometry(1,1);

// Geometry uses the right-hand grip as origin and places the barrel toward -Z.
export function weaponModel(profile, rifle, skin='') {
    const model=new T.Group();
    const {metal,rubber,wood,accent}=finishMaterials(profile,skin);
    if(profile===standardWeapon&&rifle) {
        const mesh=rifle.clone(true);
        mesh.traverse(part=>{
            if(!part.isMesh)return;
            const key=part.material.uuid+'|'+skin;
            if(!rifleFinishes.has(key)){
                const material=part.material.clone();
                material.color.copy(accent.color).lerp(new T.Color(0xffffff),skin?.42:.71);
                material.roughness=.55;
                rifleFinishes.set(key,material);
            }
            part.material=rifleFinishes.get(key);
        });
        mesh.scale.setScalar(.83);model.add(mesh);
        return model;
    }
    function part(geometry,mat,x,y,z,w,h,d,angle=0) {
        const mesh=new T.Mesh(geometry,mat);
        mesh.position.set(x,y,z);mesh.scale.set(w,h,d);mesh.rotation.x=angle;model.add(mesh);
        return mesh;
    }
    const box=(mat,y,z,w,h,d)=>part(cube,mat,0,y,z,w,h,d);
    const barrel=(mat,y,z,r,len)=>part(tube,mat,0,y,z,r,len,r,Math.PI/2);
    if(profile.type==='melee'){
        box(rubber,0,0,.046,.045,.20);
        if(profile.name==='Panela')barrel(metal,0,-.23,.16,.036).rotation.x=0;
        else if(profile.name==='Taco')barrel(wood,0,-.34,.037,.63);
        else box(metal,.015,-.31,.025,.072,profile.name==='Faca'?.24:.55);
        return model;
    }
    if(profile.type==='grenade') {
        part(sphere,accent,0,.042,-.048,.055,.072,.055);
        box(metal,.116,-.048,.033,.021,.038);
        return model;
    }
    const sidearm=profile.type==='pistol';
    const compact=sidearm||profile.type==='smg';
    const heavy=profile.type==='launcher'||profile.type==='lmg';
    const long=['sniper','marksman','shotgun'].includes(profile.type);
    const length=(sidearm?.24:compact?.47:long?.81:.65)+(profile.variant%5)*.022;
    box(rubber,-.035,.015,.057,.135,.075).rotation.x=.21;
    box(metal,.088,-length*.24,heavy?.12:.072,heavy?.134:.107,length*.67);
    box(accent,.103,-length*.26,heavy?.124:.077,.035,length*.57);
    for(const side of [-1,1])
        part(cube,accent,side*(heavy?.061:.037),.082,-length*.25,.009,.040,length*.45);
    barrel(metal,.108,-length*.72,profile.type==='launcher'?.067:heavy?.033:.018,length*.57);
    if(!sidearm){
        box(profile.variant%4===1?wood:rubber,.02,.16,.071,.099,.23);
        box(rubber,-.044,-length*.33,heavy?.127:.068,heavy?.19:.134,.09).rotation.x=-.16;
    }
    if(profile.type==='sniper'||profile.type==='marksman'){
        barrel(rubber,.194,-.085,.038,.23);
        barrel(metal,.194,-.222,.045,.06);
    }
    if(profile.type==='shotgun')barrel(metal,.068,-length*.58,.021,length*.48);
    if(profile.type==='bow')for(const side of [-1,1]){
        part(cube,wood,side*.15,.1,-.32,.29,.035,.06).rotation.y=side*.31;
        part(cube,metal,side*.20,.1,-.22,.40,.012,.012);
    }
    return model;
}
