"""Experimental same-origin multiplayer service. Run with Python and aiohttp."""
import asyncio, hashlib, hmac, math, os, secrets, sqlite3, time
from pathlib import Path
from aiohttp import web, WSMsgType
ROOT=Path(__file__).parent
DB=sqlite3.connect(os.environ.get('OZ_DB',str(ROOT/'players.sqlite3')))
DB.execute('CREATE TABLE IF NOT EXISTS users (name TEXT PRIMARY KEY, salt TEXT, password TEXT, kills INTEGER DEFAULT 0)'); DB.commit()
try: DB.execute('ALTER TABLE users ADD COLUMN wins INTEGER DEFAULT 0');DB.commit()
except sqlite3.OperationalError: pass
TOKENS={}; ROOMS={}; META={}; AUTH_ATTEMPTS={}; WEAPONS=[(.125,34,68),(.075,24,45),(.85,100,95),(.23,30,38)]
SOLIDS=[(0,-41,84,1,1.1),(0,41,84,1,1.1),(-41,0,1,82,1.1),(41,0,1,82,1.1),(-23,-22,14,12,9),(22,-21,13,14,11),(-22,22,13,13,8),(23,23,15,12,10),(-11,-13,5,9,3),(12,12,5,9,3),(-26,3,10,5,4),(26,-3,10,5,4),(8,-27,5,5,2.5),(-8,27,5,5,2.5)]+[(x,z,2.5,2.2,1.35) for x,z in [(-7,-7),(7,7),(-17,8),(17,-8),(-31,-12),(31,12),(-5,15),(5,-15)]]
def digest(p,s): return hashlib.pbkdf2_hmac('sha256',p.encode(),bytes.fromhex(s),200000).hex()
def ray(o,d,b,limit):
 lo,hi=0,limit
 for x,v,(a,c) in zip(o,d,b):
  if abs(v)<1e-8:
   if not a<=x<=c:return float('inf')
  else:
   t,u=sorted(((a-x)/v,(c-x)/v));lo=max(lo,t);hi=min(hi,u)
   if lo>hi:return float('inf')
 return lo
def block(x,z):return any(abs(x-a)<w/2+.30 and abs(z-b)<d/2+.30 for a,b,w,d,h in SOLIDS)
async def auth(req):
 now=time.monotonic(); ip=req.remote or 'unknown'
 recent=[t for t in AUTH_ATTEMPTS.get(ip,[]) if now-t<60]
 if len(recent)>=12:return web.json_response({'error':'Muitas tentativas. Tente em um minuto.'},status=429)
 AUTH_ATTEMPTS[ip]=recent+[now]
 for key,(_,expiry) in list(TOKENS.items()):
  if expiry<now:TOKENS.pop(key,None)
 try:
  j=await req.json(); name=j['name'].strip().upper(); pwd=j['password']; assert isinstance(pwd,str); assert 3<=len(name)<=22 and all(c.isalnum() or c in '_-' for c in name) and 8<=len(pwd)<=128
 except Exception:return web.json_response({'error':'Nome: 3–22 letras/números. Senha: 8–128 caracteres.'},status=400)
 row=DB.execute('SELECT salt,password FROM users WHERE name=?',(name,)).fetchone()
 if j.get('register'):
  if row:return web.json_response({'error':'Nome já utilizado.'},status=409)
  salt=secrets.token_hex(16); DB.execute('INSERT INTO users(name,salt,password) VALUES(?,?,?)',(name,salt,digest(pwd,salt)));DB.commit()
 elif not row or not hmac.compare_digest(row[1],digest(pwd,row[0])):return web.json_response({'error':'Credenciais inválidas.'},status=401)
 token=secrets.token_urlsafe(32);TOKENS[token]=(name,time.monotonic()+86400);return web.json_response({'token':token,'name':name})
async def ranking(req):return web.json_response([{'name':n,'kills':k,'wins':w} for n,k,w in DB.execute('SELECT name,kills,wins FROM users ORDER BY wins DESC,kills DESC,name LIMIT 50')])
def public(p):return {k:p[k] for k in ['id','name','x','z','y','yaw','hp','kills','walk','skin','alive']}
async def broadcast(room):
 if not room:return
 rid=next(iter(room.values()))['room'];m=META[rid];now=time.monotonic()
 msg={'type':'snapshot','players':[public(p) for p in room.values()],'mode':m['mode'],'phase':m['phase'],'zone':m['zone'],'countdown':max(0,math.ceil(m['start']-now)) if m['start'] else None,'alive':sum(p['alive'] for p in room.values())}
 for p in list(room.values()):
  try:await p['ws'].send_json(msg)
  except Exception:pass
async def match(req):
 token=req.query.get('token'); identity=TOKENS.get(token)
 if not identity or identity[1]<time.monotonic():return web.Response(status=401)
 if any(p['name']==identity[0] for room in ROOMS.values() for p in room.values()):return web.Response(status=409,text='Conta já em uma partida')
 ws=web.WebSocketResponse(heartbeat=20,max_msg_size=2048);await ws.prepare(req)
 kind='br' if req.query.get('mode')=='br' else 'dm'
 rid=next((i for i,r in ROOMS.items() if len(r)<12 and META[i]['mode']==kind and (kind=='dm' or META[i]['phase']=='waiting') and all(p['name']!=identity[0] for p in r.values())),None)
 if rid is None:
  rid=secrets.token_hex(4);ROOMS[rid]={};META[rid]={'mode':kind,'phase':'playing' if kind=='dm' else 'waiting','start':0,'zone':40,'began':0}
 room=ROOMS[rid];pid=secrets.token_hex(5);spawn=[(0,14),(0,-14),(-14,0),(14,0)][len(room)%4]
 p={'id':pid,'name':identity[0],'x':spawn[0],'z':spawn[1],'y':0,'yaw':0,'hp':100,'kills':0,'walk':0,'skin':0,'ws':ws,'last':time.monotonic(),'shot':0,'room':rid,'alive':True,'ammo':[30,24,5,12],'reserve':[120,144,30,72],'reload':[0.0]*4,'packets':0,'window':time.monotonic(),'move_credit':.8,'move_time':time.monotonic(),'next_shot':0};room[pid]=p
 await ws.send_json({'type':'welcome','id':pid,'room':rid,'x':p['x'],'z':p['z'],'mode':kind,'phase':META[rid]['phase']});await broadcast(room)
 try:
  async for m in ws:
   if m.type!=WSMsgType.TEXT:continue
   try:
    j=m.json();now=time.monotonic()
    if not isinstance(j,dict):continue
    if now-p['window']>=1:p['window']=now;p['packets']=0
    p['packets']+=1
    if p['packets']>60:continue
    if j['type']=='move' and p['alive'] and META[rid]['phase']=='playing':
     x,z,y,yaw,walk=[float(j[k]) for k in ['x','z','y','yaw','walk']]
     if not all(math.isfinite(v) for v in [x,z,y,yaw,walk]):continue
     dt=max(0,min(.35,now-p['move_time']));p['move_time']=now;p['move_credit']=min(4.5,p['move_credit']+12*dt)
     distance=math.hypot(x-p['x'],z-p['z']);steps=max(1,math.ceil(distance/.2)) if distance<6 else 1
     if abs(x)<40 and abs(z)<40 and distance<=p['move_credit'] and distance<6 and all(not block(p['x']+(x-p['x'])*i/steps,p['z']+(z-p['z'])*i/steps) for i in range(1,steps+1)):p.update(x=x,z=z,y=max(0,min(y,1.4)),yaw=yaw,walk=walk,skin=max(0,min(2,int(j.get('skin',0)))));p['move_credit']-=distance
     await ws.send_json({'type':'self','hp':p['hp'],'kills':p['kills'],'x':p['x'],'z':p['z']})
    elif j['type']=='reload' and p['alive'] and META[rid]['phase']=='playing':
     i=int(j['weapon']);assert 0<=i<4
     if not p['reload'][i] and p['ammo'][i]<[30,24,5,12][i] and p['reserve'][i]>0:p['reload'][i]=now+[1.5,1.2,2.1,1.0][i]
    elif j['type']=='shot' and p['alive'] and META[rid]['phase']=='playing':
     i=int(j['weapon']); assert 0<=i<4;rate,damage,limit=WEAPONS[i];yaw,pitch=float(j['yaw']),float(j['pitch']);assert math.isfinite(yaw) and math.isfinite(pitch)
     if p['reload'][i] and now>=p['reload'][i]:
      count=min([30,24,5,12][i]-p['ammo'][i],p['reserve'][i]);p['ammo'][i]+=count;p['reserve'][i]-=count;p['reload'][i]=0
     if now<p['next_shot'] or p['reload'][i] or p['ammo'][i]<=0:continue
     p['ammo'][i]-=1;p['shot']=now;p['next_shot']=now+rate*.92;d=[math.sin(yaw)*math.cos(pitch),math.sin(pitch),-math.cos(yaw)*math.cos(pitch)];o=[p['x'],p['y']+1.62,p['z']]
     t=min([limit]+[ray(o,d,[(x-w/2,x+w/2),(0,h),(z-depth/2,z+depth/2)],limit) for x,z,w,depth,h in SOLIDS]);target=None
     for q in room.values():
      if q is p or not q['alive']:continue
      k=ray(o,d,[(q['x']-.36,q['x']+.36),(q['y'],q['y']+1.85),(q['z']-.36,q['z']+.36)],t)
      if k<t:t=k;target=q
     if target:
      target['hp']-=damage;await ws.send_json({'type':'hit'})
      if target['hp']<=0:
       p['kills']+=1;DB.execute('UPDATE users SET kills=kills+1 WHERE name=?',(p['name'],));DB.commit();await ws.send_json({'type':'kill','kills':p['kills']})
       if kind=='br':
        target.update(hp=0,alive=False);await target['ws'].send_json({'type':'eliminated'});await check_winner(room)
       else:
        target.update(hp=100,x=0,z=-14 if target['z']>0 else 14,y=0,ammo=[30,24,5,12],reserve=[120,144,30,72],reload=[0.0]*4);await target['ws'].send_json({'type':'respawn','x':target['x'],'z':target['z']})
    elif j['type']=='leave':break
   except (ValueError,KeyError,TypeError,AssertionError,OverflowError):continue
 finally:
  room.pop(pid,None)
  if not room:ROOMS.pop(rid,None);META.pop(rid,None)
  else:
   await check_winner(room);await broadcast(room)
 return ws
async def check_winner(room):
 if not room:return
 m=META[next(iter(room.values()))['room']]
 if m['mode']!='br' or m['phase']!='playing':return
 alive=[p for p in room.values() if p['alive']]
 if len(alive)>1:return
 m['phase']='ended'
 if alive:
  winner=alive[0];DB.execute('UPDATE users SET wins=wins+1 WHERE name=?',(winner['name'],));DB.commit()
  try:await winner['ws'].send_json({'type':'victory'})
  except Exception:pass
async def ticker(app):
 while True:
  await asyncio.sleep(.12);now=time.monotonic()
  for rid,room in list(ROOMS.items()):
   if not room or rid not in META:continue
   m=META[rid]
   if m['mode']=='br':
    if m['phase']=='waiting':
     if len(room)<2:m['start']=0
     elif not m['start']:m['start']=now+float(os.environ.get('OZ_COUNTDOWN',10))
     elif now>=m['start']:
      m.update(phase='playing',began=now,zone=40)
      for p in list(room.values()):
       try:await p['ws'].send_json({'type':'begin'})
       except Exception:pass
    elif m['phase']=='playing':
     m['zone']=max(2,40-(now-m['began'])*.30)
     for p in list(room.values()):
      if p['alive'] and math.hypot(p['x'],p['z'])>m['zone']:
       p['hp']=max(0,p['hp']-1.8)
       if p['hp']==0:
        p['alive']=False
        try:await p['ws'].send_json({'type':'eliminated'})
        except Exception:pass
     await check_winner(room)
   await broadcast(room)
async def startup(app):app['ticker']=asyncio.create_task(ticker(app))
async def cleanup(app):app['ticker'].cancel()
async def index(req):return web.FileResponse(ROOT/'index.html')
async def asset(req):
 n=req.match_info['name']
 if n not in ['engine.js','visuals.js','game.js','lobby.js','online.js','lobby.css','graphics.js','manifest.webmanifest','sw.js','icon.svg']:raise web.HTTPNotFound()
 return web.FileResponse(ROOT/n)
app=web.Application(client_max_size=8192);app.router.add_get('/',index);app.router.add_post('/api/auth',auth);app.router.add_get('/api/ranking',ranking);app.router.add_get('/ws',match);app.router.add_static('/assets', ROOT/'assets');app.router.add_get('/{name}',asset);app.on_startup.append(startup);app.on_cleanup.append(cleanup)
if __name__=='__main__':web.run_app(app,host='0.0.0.0',port=int(os.environ.get('PORT',8080)))
