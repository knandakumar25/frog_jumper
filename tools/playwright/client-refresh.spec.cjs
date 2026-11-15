/** Run the actual website Axios client against an HTTP server that expires concurrent requests. */
const { test, expect } = require('@playwright/test');
const { readFileSync } = require('node:fs');
const { pathToFileURL } = require('node:url');
const http = require('node:http');
const root = process.env.CLIENT_SOURCE_DIR || '/website/src';

test('website client shares one refresh for concurrent expired requests', async () => {
  let initial = 0;
  let refreshes = 0;
  const waiting = [];
  const server = http.createServer((req, res) => {
    const answer = (status, body) => { res.writeHead(status, { 'Content-Type': 'application/json' }); res.end(JSON.stringify(body)); };
    if (req.url.endsWith('/token/refresh/')) {
      refreshes++;
      answer(200, { access: 'fresh-access', refresh: 'rotated-refresh' });
    } else if (req.headers.authorization === 'Bearer fresh-access') answer(200, { ok: true });
    else {
      initial++;
      waiting.push(() => answer(401, { detail: 'expired' }));
      // Release both expired responses together, ensuring both requests overlap.
      if (initial === 2) waiting.splice(0).forEach(send => send());
    }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  try {
    const endpoint = `http://127.0.0.1:${server.address().port}/api`;
    // Resolve imports and Vite's environment substitution only; client logic is unmodified.
    const tokenUrl = 'data:text/javascript;base64,' + Buffer.from(readFileSync(root + '/api/tokens.js', 'utf8')).toString('base64');
    const source = readFileSync(root + '/api/client.js', 'utf8')
      .replace("'axios'", JSON.stringify(pathToFileURL(require.resolve('axios')).href))
      .replace("'./tokens'", JSON.stringify(tokenUrl))
      .replace('import.meta.env.VITE_API_BASE_URL', JSON.stringify(endpoint));
    const { tokens } = await import(tokenUrl);
    const { default: client } = await import('data:text/javascript;base64,' + Buffer.from(source).toString('base64'));
    tokens.access = 'expired-access';
    tokens.refresh = 'original-refresh';
    const responses = await Promise.all([client.get('/auth/profile/'), client.get('/auth/countries/')]);
    expect(responses.every(response => response.data.ok)).toBe(true);
    expect(initial).toBe(2);
    expect(refreshes).toBe(1);
    expect(tokens.access).toBe('fresh-access');
    expect(tokens.refresh).toBe('rotated-refresh');
    tokens.clear();
  } finally {
    await new Promise(resolve => server.close(resolve));
  }
});

test('failed refresh preserves bare-route locale and game request; Clerk proof never uses legacy refresh', async () => {
  let refreshes=0;
  const server=http.createServer((req,res)=>{
    if(req.url.endsWith('/token/refresh/')) refreshes++;
    res.writeHead(401,{'Content-Type':'application/json'}); res.end('{"detail":"expired"}');
  });
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const previousWindow=global.window;
  try {
    const endpoint=`http://127.0.0.1:${server.address().port}/api`;
    const tokenUrl='data:text/javascript;base64,'+Buffer.from(readFileSync(root+'/api/tokens.js','utf8')).toString('base64');
    const source=readFileSync(root+'/api/client.js','utf8').replace("'axios'",JSON.stringify(pathToFileURL(require.resolve('axios')).href)).replace("'./tokens'",JSON.stringify(tokenUrl)).replace('import.meta.env.VITE_API_BASE_URL',JSON.stringify(endpoint));
    const {tokens}=await import(tokenUrl);
    const {default:client,setAccountProof}=await import('data:text/javascript;base64,'+Buffer.from(source).toString('base64'));
    const id='11111111-2222-4333-8444-555555555555';
    for(const path of [`/ar/account?session_id=${id}`,`/onboarding?locale=he&session_id=${id}`]) {
      global.window={location:{href:'http://localhost:5173'+path}};
      tokens.access='expired'; tokens.refresh='revoked';
      await expect(client.get('/auth/profile/')).rejects.toBeTruthy();
      expect(window.location.href).toBe(`/${path.includes('locale=he')?'he':'ar'}/login/game/${id}?expired=1`);
      expect(tokens.access).toBeNull();
    }
    const before=refreshes;
    let acquired=0;
    const clear=setAccountProof(async()=>`proof-${++acquired}`);
    global.window={location:{href:'http://localhost:5173/de/account'}};
    await expect(client.get('/auth/profile/')).rejects.toBeTruthy();
    expect(refreshes).toBe(before);
    expect(acquired).toBe(1);
    expect(window.location.href).toBe('http://localhost:5173/de/account');
    clear(); tokens.clear();
  } finally {
    global.window=previousWindow;
    await new Promise(resolve=>server.close(resolve));
  }
});

for(const refreshStatus of [200,401]) test(`late ${refreshStatus} refresh cannot restore or sign out a newer session`, async () => {
  let release;
  let reached;
  const refreshing=new Promise(resolve=>{reached=resolve;});
  const server=http.createServer((req,res)=>{
    const answer=(status,data)=>{res.writeHead(status,{'Content-Type':'application/json'});res.end(JSON.stringify(data));};
    if(req.url.endsWith('/token/refresh/')) { release=()=>answer(refreshStatus,{access:'old-restored',refresh:'old-rotated'}); reached(); }
    else answer(401,{detail:'expired'});
  });
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const previousWindow=global.window;
  try {
    const endpoint=`http://127.0.0.1:${server.address().port}/api`;
    const tokenUrl='data:text/javascript;base64,'+Buffer.from(readFileSync(root+'/api/tokens.js','utf8')).toString('base64');
    const source=readFileSync(root+'/api/client.js','utf8').replace("'axios'",JSON.stringify(pathToFileURL(require.resolve('axios')).href)).replace("'./tokens'",JSON.stringify(tokenUrl)).replace('import.meta.env.VITE_API_BASE_URL',JSON.stringify(endpoint));
    const {tokens}=await import(tokenUrl);
    const {default:client}=await import('data:text/javascript;base64,'+Buffer.from(source).toString('base64'));
    tokens.access='old-expired'; tokens.refresh='old-refresh';
    global.window={location:{href:'http://localhost:5173/de/account'}};
    const result=client.get('/auth/profile/').catch(error=>error);
    await refreshing;
    tokens.install('new-access','new-refresh');
    release();
    expect(await result).toBeInstanceOf(Error);
    expect(tokens.access).toBe('new-access'); expect(tokens.refresh).toBe('new-refresh');
    expect(window.location.href).toBe('http://localhost:5173/de/account');
    tokens.clear();
  } finally { global.window=previousWindow; await new Promise(resolve=>server.close(resolve)); }
});
