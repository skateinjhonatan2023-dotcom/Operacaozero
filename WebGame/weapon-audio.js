import { weaponSeed } from './weapons.js';

const familySounds={
    rifle:[136,.17,.71],smg:[227,.105,.60],shotgun:[74,.27,.86],marksman:[111,.22,.63],
    sniper:[65,.33,.73],lmg:[103,.20,.82],pistol:[188,.128,.62],launcher:[51,.37,.62],
    bow:[169,.14,.22],melee:[305,.11,.29],grenade:[54,.38,.76],throwable:[345,.095,.34],
};

export function shotParameters(profile) {
    const seed=weaponSeed(profile.name);
    const [frequency,length,noise]=familySounds[profile.type]??familySounds.rifle;
    return [frequency*(.82+(seed%61)*.006),length*(.84+((seed>>>9)%51)*.007),
        noise*(.88+((seed>>>18)%37)*.007),seed];
}
export function createWeaponAudio(context,original) {
    const presets=new Map();
    return function play(profile,gain=.37){
        if(!context||context.state!=='running')return;
        let buffer=presets.get(profile.name);
        if(profile.name==='M4A1'&&original())buffer=original();
        if(!buffer){
            const [frequency,length,noise,seed]=shotParameters(profile);
            const samples=Math.ceil(context.sampleRate*length);
            buffer=context.createBuffer(1,samples,context.sampleRate);
            const data=buffer.getChannelData(0);
            let state=seed,previous=0;
            for(let i=0;i<samples;i++){
                const t=i/context.sampleRate;
                state=(Math.imul(1664525,state)+1013904223)>>>0;
                const n=state/4294967296*2-1;
                const blast=(n-.44*previous)*noise*Math.exp(-t/(length*.17));
                const pulse=Math.sin(2*Math.PI*frequency*t*(1-t*.55))*.66*Math.exp(-t/(length*.23));
                const click=Math.sin(t*3920*2*Math.PI)*.18*Math.exp(-t/.006);
                data[i]=Math.max(-1,Math.min(1,(blast+pulse+click)*Math.min(1,i/9)));
                previous=n;
            }
            presets.set(profile.name,buffer);
        }
        const source=context.createBufferSource(),volume=context.createGain();
        source.buffer=buffer;
        source.playbackRate.value=.975+Math.random()*.05;
        volume.gain.value=gain;
        source.connect(volume).connect(context.destination);
        source.start();
    };
}
