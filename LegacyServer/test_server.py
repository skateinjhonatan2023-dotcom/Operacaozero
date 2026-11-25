import os, tempfile, asyncio
os.environ['OZ_DB']=':memory:'
from aiohttp.test_utils import TestServer,TestClient
from server import app
async def main():
 async with TestClient(TestServer(app)) as c:
  async def register(n):
   r=await c.post('/api/auth',json={'name':n,'password':'test-password123','register':True});assert r.status==200;return (await r.json())['token']
  a,b=await register('ALPHA'),await register('BRAVO');r=await c.post('/api/auth',json={'name':'ALPHA','password':'wrongpassword'});assert r.status==401
  w1,w2=await c.ws_connect('/ws?token='+a),await c.ws_connect('/ws?token='+b)
  async def until(w,t):
   for _ in range(30):
    j=await w.receive_json(timeout=2)
    if j['type']==t:return j
   raise AssertionError(t)
  x=await until(w1,'welcome');y=await until(w2,'welcome');assert x['room']==y['room'];s=await until(w2,'snapshot');assert len(s['players'])==2
  await w1.send_json({'type':'shot','weapon':2,'yaw':0,'pitch':0});await until(w1,'hit');k=await until(w1,'kill');assert k['kills']==1;await until(w2,'respawn');r=await c.get('/api/ranking');assert (await r.json())[0]['kills']==1
  await w1.close();await w2.close();print('PASS: cadastro, login inválido, matchmaking, 2 clientes, disparo, eliminação, respawn, ranking')
asyncio.run(main())
