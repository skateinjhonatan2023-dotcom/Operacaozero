function shuffle(values, random) {
    const shuffled=values.slice();
    for(let i=shuffled.length-1;i>0;i--){
        const j=Math.floor(random()*(i+1));
        [shuffled[i],shuffled[j]]=[shuffled[j],shuffled[i]];
    }
    return shuffled;
}

export function distributeSupplies(sites, profiles, isClear, random=Math.random) {
    const result=[];
    const quotas={interior:14,roof:7,yard:15};
    const bags={
        interior:shuffle(['weapon','weapon','weapon','weapon','weapon','ammo','ammo','ammo','ammo','frag','smoke','kit','kit','kit'],random),
        roof:shuffle(['weapon','weapon','weapon','ammo','ammo','kit','kit'],random),
        yard:shuffle(['weapon','weapon','weapon','weapon','ammo','ammo','ammo','ammo','ammo','frag','smoke','kit','kit','kit','kit'],random),
    };
    const weapons=shuffle(profiles.filter(item=>item.capacity>0&&!item.aid&&!item.utility),random);
    let nextWeapon=0;
    for(const [kind,quota] of Object.entries(quotas)){
        let count=0;
        for(const site of shuffle(sites.filter(site=>site.kind===kind),random)){
            const point=shuffle(site.points,random).find(pos=>isClear(pos)&&
                result.every(item=>Math.hypot(item.x-pos.x,item.z-pos.z)>=3.2||Math.abs(item.y-pos.y)>=2));
            if(!point)continue;
            const item=bags[kind][count++];
            result.push({...point,kind:item,weapon:item==='weapon'?weapons[nextWeapon++]:null});
            if(count===quota)break;
        }
    }
    return result;
}
