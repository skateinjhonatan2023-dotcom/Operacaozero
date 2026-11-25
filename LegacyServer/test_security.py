import os,asyncio
os.environ['OZ_DB']=':memory:'
from aiohttp.test_utils import TestClient,TestServer
from aiohttp import WSServerHandshakeError
from server import app,ROOMS
async def main():
 async with TestClient(TestServer(app)) as c:
  r=await c.post('/api/auth',json={'name':'ALPHA','password':'password-test','register':True});assert r.status==200
  t=(await r.json())['token'];w=await c.ws_connect('/ws?token='+t)
  try:
   await c.ws_connect('/ws?token='+t);raise AssertionError('Sessão duplicada')
  except WSServerHandshakeError as e:assert e.status==409
  p=next(iter(next(iter(ROOMS.values())).values()));p['ammo'][2]=0
  await w.send_json({'type':'shot','weapon':2,'yaw':0,'pitch':0});await asyncio.sleep(.15);assert p['shot']==0
  await w.send_json({'type':'reload','weapon':2});await asyncio.sleep(.15);assert p['reload'][2]>0
  await w.send_json({'type':'move','x':39,'z':39,'y':0,'yaw':0,'walk':0});await asyncio.sleep(.15);assert (p['x'],p['z'])==(0,14)
  await w.close();print('PASS: sessão duplicada rejeitada, tiro sem munição bloqueado, recarga, teleporte rejeitado')
asyncio.run(main())
