const $=id=>document.getElementById(id);
export const catalog=[
    {id:'ember',type:'skin',name:'Ferrugem Viva',color:'#e77b4e',price:125},
    {id:'lagoon',type:'skin',name:'Lagoa Elétrica',color:'#47d2c7',price:150},
    {id:'aurora',type:'skin',name:'Aurora Noturna',color:'#a177ef',price:180},
    {id:'expedition',type:'outfit',name:'Expedição',color:'#c5a86d',price:120,cloth:0x62513d,armor:0xa29164,fabric:0x746953},
    {id:'naval',type:'outfit',name:'Patrulha Naval',color:'#63a6dc',price:150,cloth:0x304664,armor:0x53799b,fabric:0x4b5f79},
    {id:'rescue',type:'outfit',name:'Busca e Resgate',color:'#e0e1c9',price:180,cloth:0x667179,armor:0xb9b9a0,fabric:0x778481},
];
const known=new Map(catalog.map(item=>[item.id,item]));
const fresh=()=>({credits:180,owned:[],skin:'',outfit:''});
export function cosmetics(profileKey,onEquip) {
    const key='costa-cosmetics-v1:'+profileKey;
    let state=fresh(),mode='shop';
    try{
        const parsed=JSON.parse(localStorage.getItem(key));
        if(parsed&&Array.isArray(parsed.owned)){
            const owned=[...new Set(parsed.owned.filter(id=>known.has(id)))];
            state={
                credits:Number.isSafeInteger(parsed.credits)&&parsed.credits>=0?parsed.credits:180,
                owned,
                skin:owned.includes(parsed.skin)&&known.get(parsed.skin).type==='skin'?parsed.skin:'',
                outfit:owned.includes(parsed.outfit)&&known.get(parsed.outfit).type==='outfit'?parsed.outfit:'',
            };
        }
    }catch{}
    function sync(){
        $('credit-count').textContent=state.credits+' créditos';
        $('collection-balance').textContent=state.credits+' créditos';
    }
    function save(){
        try{localStorage.setItem(key,JSON.stringify(state));}catch{}
        sync();
    }
    function itemButton(label,action){
        const button=document.createElement('button');button.type='button';button.textContent=label;
        button.onclick=action;
        return button;
    }
    function message(text){$('collection-message').textContent=text;}
    function buy(item){
        if(!state.owned.includes(item.id)){
            if(state.credits<item.price){message('Créditos insuficientes. Ganhe mais jogando.');return;}
            state.credits-=item.price;state.owned.push(item.id);
            save();render();message(item.name+' entrou no inventário.');
        }
    }
    function equip(item){
        if(!state.owned.includes(item.id))return;
        state[item.type]=state[item.type]===item.id?'':item.id;save();render();onEquip();
    }
    function draw(cost,wheel,button) {
        const pool=catalog.filter(item=>!state.owned.includes(item.id));
        if(!pool.length){message('Inventário completo.');return;}
        if(state.credits<cost){message('Créditos insuficientes. Ganhe mais jogando.');return;}
        const index=Math.floor(Math.random()*pool.length),item=pool[index];
        state.credits-=cost;state.owned.push(item.id);save();
        const reveal=()=>{render();message('Recebeu '+item.name+'. Equipe no inventário.');};
        if(!wheel||matchMedia('(prefers-reduced-motion: reduce)').matches){reveal();return;}
        button.disabled=true;
        const rotation=1440-(index+.5)*360/pool.length;
        const spin=wheel.animate([{transform:'rotate(0deg)'},{transform:'rotate('+rotation+'deg)'}],
            {duration:1150,easing:'cubic-bezier(.12,.7,.2,1)',fill:'forwards'});
        spin.finished.then(()=>{if(wheel.isConnected&&!$('collection').hidden)reveal();}).catch(()=>{});
    }
    function render() {
        const list=$('collection-list');
        list.replaceChildren();
        $('collection-title').textContent={shop:'Loja',inventory:'Inventário',crate:'Caixas',wheel:'Roleta'}[mode];
        if(mode==='crate'||mode==='wheel'){
            const price=mode==='crate'?100:85;
            const pool=catalog.filter(item=>!state.owned.includes(item.id));
            let wheel=null;
            if(mode==='wheel'&&pool.length){
                const frame=document.createElement('div');frame.className='wheel-frame';frame.setAttribute('aria-hidden','true');
                wheel=document.createElement('div');wheel.className='wheel-face';
                const sectors=pool.map((item,i)=>item.color+' '+i*100/pool.length+'% '+(i+1)*100/pool.length+'%');
                wheel.style.background='conic-gradient('+sectors.join(',')+')';
                frame.append(wheel);list.append(frame);
            }
            const button=itemButton((mode==='crate'?'Abrir caixa':'Girar roleta')+' · '+price+' créditos',()=>draw(price,wheel,button));
            list.append(button);
            const notice=document.createElement('p');
            notice.textContent='Uma recompensa entre os itens que faltam, com chances iguais.';
            list.append(notice);
            const prizes=document.createElement('small');
            prizes.textContent=pool.map(item=>item.name).join(' · ');
            list.append(prizes);
            return;
        }
        const items=mode==='inventory'?catalog.filter(item=>state.owned.includes(item.id)):catalog;
        if(!items.length){list.textContent='Nenhum item equipado. Visite a loja ou uma caixa.';return;}
        for(const item of items){
            const row=document.createElement('div');row.className='collection-row';row.style.setProperty('--item',item.color);
            const name=document.createElement('strong');name.textContent=item.name;
            const type=document.createElement('small');type.textContent=item.type==='skin'?'Pintura de arma':'Traje';
            const meta=document.createElement('span');meta.append(name,type);
            const owned=state.owned.includes(item.id);
            const button=owned?itemButton(state[item.type]===item.id?'Remover':'Equipar',()=>equip(item)):
                itemButton('Comprar · '+item.price,()=>buy(item));
            row.append(meta,button);list.append(row);
        }
    }
    sync();
    return {
        open(value){mode=value;message('');render();$('collection').hidden=false;},
        award(won,kills,seconds){
            const amount=(won?70:0)+kills*14+(seconds>=30?25:0);
            state.credits+=amount;
            save();
            return amount;
        },
        skin:()=>state.skin,
        outfit:()=>known.get(state.outfit),
    };
}
