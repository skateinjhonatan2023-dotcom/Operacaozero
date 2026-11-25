import os, asyncio
os.environ['OZ_DB']=':memory:';os.environ['OZ_COUNTDOWN']='0.3'
from aiohttp.test_utils import TestClient,TestServer
from server import app
async def main():
 async with TestClient(TestServer(app)) as c:
  async def token(n):
   r=await c.post('/api/auth',json={'name':n,'password':'secret-password','register':True});assert r.status==200;return (await r.json())['token']
  async def until(w,t):
   for _ in range(60):
    m=await w.receive_json(timeout=2)
    if m['type']==t:return m
   raise AssertionError(t)
  a,b=await token('ALPHA'),await token('BRAVO');w1=await c.ws_connect('/ws?mode=br&token='+a);x=await until(w1,'welcome');assert x['phase']=='waiting';w2=await c.ws_connect('/ws?mode=br&token='+b);y=await until(w2,'welcome');assert x['room']==y['room'];await until(w1,'begin');await until(w2,'begin');await w1.send_json({'type':'shot','weapon':2,'yaw':0,'pitch':0});await until(w1,'kill');await until(w2,'eliminated');await until(w1,'victory');r=await c.get('/api/ranking');j=await r.json();assert j[0]['name']=='ALPHA' and j[0]['wins']==1 and j[0]['kills']==1;await w1.close();await w2.close();print('PASS: BR 2 clientes, fila, contagem, eliminação sem respawn, vitória, ranking')
asyncio.run(main())
