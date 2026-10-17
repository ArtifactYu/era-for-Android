import fs from 'node:fs/promises';
import path from 'node:path';
import {CDP,delay,until,leave,work} from './android-driver.mjs';
const results=[];function check(name,value){results.push({name,pass:!!value});if(!value)throw Error(name);}
const output=path.resolve('outputs/screenshots');await fs.mkdir(output,{recursive:true});
const hash=`(async()=>Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',await(await fetch(screen.src)).arrayBuffer()))).map(n=>n.toString(16).padStart(2,'0')).join(''))()`;
const transcript=`fetch('/diagnostics'+location.search).then(r=>r.json())`;
let main=await CDP.connect('main');const game=await main.eval('games.find(g=>g.name==="星间驿站")');check('SAF parent directory discovered game',game&&game.images===1&&game.scripts===1);
check('Library returns to portrait',await main.eval('innerHeight>innerWidth'));await main.shot(path.join(output,'library.png'));main.close();
async function launch(){main=await CDP.connect('main');await main.eval(`native('play',${JSON.stringify(game.id)})`);main.close();const c=await CDP.connect('game');await until(()=>c.eval('typeof ready!=="undefined"&&ready&&screen.naturalWidth>0'),90000);await delay(600);return c;}
let c=await launch();
async function tap(x,y){await c.call('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x,y}]});await delay(100);await c.call('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});}
async function press(value){await until(()=>c.eval('ready&&!inFlight'));await c.eval(`text.value='${value}';document.getElementById('send').click()`);await until(()=>c.eval('ready&&!inFlight'));await delay(250);const d=await c.eval(`fetch('/status'+location.search).then(r=>r.json())`);check('Input '+value+' has no engine error',!d.error);}
try{
 check('Automatic landscape',await c.eval('innerWidth>innerHeight'));
 const before=await c.eval(hash);const head=await c.eval(`(()=>{const r=document.getElementById('sidehead').getBoundingClientRect();return {x:r.x+45,y:r.y+r.height/2}})()`);
 await tap(head.x,head.y);await delay(90);await tap(head.x,head.y);await delay(350);check('Real touch double tap opens settings',await c.eval(`document.getElementById('sidebar').classList.contains('settings')`));
 await c.eval(`document.getElementById('images').click()`);await until(async()=>await c.eval(hash)!==before);check('Image switch changes rendered pixels',true);
 await c.eval(`document.getElementById('images').click()`);await until(async()=>await c.eval(hash)===before);check('Image switch restores original pixels',true);
 await c.eval(`document.getElementById('scale').value=140;document.getElementById('scale').dispatchEvent(new Event('input'));document.getElementById('scale').dispatchEvent(new Event('change'))`);await delay(600);
 check('Font size expands game rendering',await c.eval('setting.scale===1.4&&screen.getBoundingClientRect().width>wrap.clientWidth*1.39'));
 await c.shot(path.join(output,'settings.png'));
 await c.eval(`document.getElementById('left').click()`);await delay(250);check('Left hand sidebar placement',await c.eval(`document.getElementById('sidebar').getBoundingClientRect().left<document.querySelector('.reader').getBoundingClientRect().left`));
 await c.eval(`document.getElementById('left').click();document.getElementById('modeToggle').click()`);
 const one=await c.eval(`(()=>{const r=document.querySelector('[data-key="1"]').getBoundingClientRect();return{x:r.x+r.width/2,y:r.y+r.height/2}})()`);
 await tap(one.x,one.y);await delay(90);await tap(one.x,one.y);await delay(350);check('Repeated number taps stay in game mode',await c.eval(`text.value==='11'&&!document.getElementById('sidebar').classList.contains('settings')`));
 await c.eval('text.value=""');await press('0');await press('1');await press('3');await press('2');
 const lines=await c.eval(transcript);check('Native engine save/load restores progress',lines.filter(l=>l.includes('航行记录：')).at(-1)?.includes('航行记录：1 天'));
 await c.shot(path.join(output,'player.png'));await leave(c);
 c=await launch();check('Display settings persist after restart',await c.eval('setting.scale===1.4&&setting.images===true&&setting.left===false'));
 await c.eval(`document.getElementById('modeToggle').click();document.getElementById('reset').click()`);await delay(500);await leave(c);
}catch(e){results.push({name:'UI run',pass:false,error:e.message});if(c)try{await c.shot(path.join(work,'ui-failure.png'));await leave(c);}catch{}process.exitCode=1;}
await fs.writeFile(path.join(work,'ui-results.json'),JSON.stringify(results,null,2));console.log(JSON.stringify(results));
