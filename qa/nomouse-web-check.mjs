import fs from 'node:fs/promises';
const log=await fs.readFile(process.argv[2],'utf8');
const url=log.match(/http:\/\/127\.0\.0\.1:\d+\S*/)?.[0];
if(!url)throw Error('Server URL not found');
const u=new URL(url);u.protocol='ws:';u.pathname='/ws';
const socket=new WebSocket(u);const states=[];let current;
socket.onmessage=e=>{if(typeof e.data!=='string')return;const m=JSON.parse(e.data);if(m.t==='state'){current=m.v;states.push(m.v)}};
await new Promise((r,j)=>{socket.onopen=r;socket.onerror=j});socket.send(JSON.stringify({t:'hello'}));
const wait=async wanted=>{const begin=Date.now();while(Date.now()-begin<5000){if(current===wanted)return;await new Promise(r=>setTimeout(r,50))}throw Error(`Expected ${wanted}; got ${current}`)};
try{
 if(process.argv[3]==='before'){await new Promise(r=>setTimeout(r,500));if(current!=='nomouse')throw Error('Expected original regression');console.log(JSON.stringify({regressionReproduced:true,current}));}
 else{await wait('enter');socket.send(JSON.stringify({t:'enter'}));await wait('string');socket.send(JSON.stringify({t:'submit',v:'hello'}));await wait('integer');socket.send(JSON.stringify({t:'submit',v:'42'}));await wait('enter');console.log(JSON.stringify({pass:true,states}));}
}finally{socket.close()}
