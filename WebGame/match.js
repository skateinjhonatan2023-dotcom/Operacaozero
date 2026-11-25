import { boundary, groundHeight } from './terrain.js';

export const roomSizes=[25,35,45];
export const initialRadius=214;
export const zoneDelay=55;
export const totalTime=400;
const stages=[[zoneDelay,initialRadius],[140,169],[165,169],[242,105],[263,105],[332,43],[349,43],[389,6]];

export function zoneAt(time) {
    for(let i=1;i<stages.length;i++){
        const [end,r]=stages[i], [start,previous]=stages[i-1];
        if(time<=end)return previous+(r-previous)*Math.max(0,(time-start)/(end-start));
    }
    return stages.at(-1)[1];
}

export function spawnPositions(count,isClear,random=Math.random) {
    const slots=[];
    for(let i=0;i<count;i++){
        const ring=i%4;
        const angle=i*2.39996323+(random()-.5)*.28;
        let chosen=null;
        for(let attempt=0;attempt<90;attempt++){
            const theta=angle+attempt*.32;
            const radius=42+ring*33+(random()-.5)*14+Math.floor(attempt/12)*2;
            const x=Math.cos(theta)*radius,z=Math.sin(theta)*radius,y=groundHeight(x,z);
            if(Math.abs(x)>=boundary-1||Math.abs(z)>=boundary-1||Math.hypot(x,z-19)<25)continue;
            if(slots.some(p=>Math.hypot(p.x-x,p.z-z)<7)||!isClear(x,y,z))continue;
            chosen={x,y,z};
            break;
        }
        if(!chosen)throw new Error('Não foi encontrado um ponto livre para o combatente.');
        slots.push(chosen);
    }
    return slots;
}
