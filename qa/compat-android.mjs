import fs from 'node:fs/promises';
import path from 'node:path';
import {CDP,delay,until,leave,work} from './android-driver.mjs';
const folder=path.join(work,'compat-android');await fs.mkdir(folder,{recursive:true});
const checks=[];let c,main;
function check(name,pass,detail){checks.push({name,pass:!!pass,detail});if(!pass)throw Error(name)}
async function touch(selector){const p=await c.eval(`(()=>{const r=document.querySelector(${JSON.stringify(selector)}).getBoundingClientRect();return{x:r.x+r.width/2,y:r.y+r.height/2}})()`);await c.call('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[p]});await delay(100);await c.call('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await delay(150)}
async function count(){return c.eval("fetch('/stats'+location.search).then(r=>r.json()).then(s=>s.inputCount)")}
async function next(selector='#send'){const n=await count();await touch(selector);await until(async()=>await c.eval('ready&&!inFlight')&&(await count())>n);await delay(350);check('Input accepted without error',!await c.eval("fetch('/status'+location.search).then(r=>r.json()).then(s=>s.error)"))}
async function frame(name){const base64=await c.eval("fetch(screen.src).then(r=>r.blob()).then(b=>new Promise(resolve=>{const r=new FileReader();r.onload=()=>resolve(r.result.split(',')[1]);r.readAsDataURL(b)}))");await fs.writeFile(path.join(folder,name+'.png'),Buffer.from(base64,'base64'))}
try{
 main=await CDP.connect('main');await until(()=>main.eval('typeof games!=="undefined"'));
 const game=await main.eval('games.find(g=>g.name==="兼容性实验室")');check('Imported original fixture ZIP through Android picker',game);
 await main.eval(`native('play',${JSON.stringify(game.id)})`);main.close();c=await CDP.connect('game');await until(()=>c.eval('typeof ready!=="undefined"&&ready&&screen.naturalWidth>0'));
 check('Nested GB18030 script decoded',await c.eval("fetch('/diagnostics'+location.search).then(r=>r.json()).then(lines=>lines.some(l=>l.includes('编码校验通过：简体中文与日本語')))") );
 await delay(350);await frame('visible');
 const pixels=await c.eval('lineHeight*screen.getBoundingClientRect().width/engineW');
 await touch('#modeToggle');await c.eval("$('scale').value=150;$('scale').dispatchEvent(new Event('input'));$('scale').dispatchEvent(new Event('change'))");await delay(700);
 check('Text displayed at 150 percent line height',await c.eval('lineHeight*screen.getBoundingClientRect().width/engineW')>pixels*1.49);
 await touch('#reset');await touch('#images');await delay(500);await frame('images-off');await touch('#images');await delay(500);await frame('images-on');await touch('#modeToggle');
 await next();await frame('group-hidden');await next();await frame('group-restored');await next();await frame('screen-hidden');await next();await frame('cleared-all');
 check('ONEINPUT activates immediate number entry',await c.eval('singleInput&&state==="integer"'));
 await next('[data-key="7"]');check('Single digit reached game without confirm',await c.eval("fetch('/diagnostics'+location.search).then(r=>r.json()).then(lines=>lines.includes('SINGLE_RESULT=7'))"));
 check('Normal INPUT restores buffered entry',await c.eval('!singleInput&&state==="integer"'));
 const n=await count();await touch('[data-key="1"]');await touch('[data-key="1"]');check('11 stays buffered without changing sidebar',await c.eval('text.value==="11"&&!$("sidebar").classList.contains("settings")')&&(await count())===n);await next();
 check('Normal integer input preserved both digits',await c.eval("fetch('/diagnostics'+location.search).then(r=>r.json()).then(lines=>lines.includes('MULTI_RESULT=11'))"));
 await next();await frame('before-clearline');await next();await frame('after-clearline');
 check('CLEARLINE continued without error',await c.eval("fetch('/diagnostics'+location.search).then(r=>r.json()).then(lines=>lines.includes('CLEARED'))"));
 await leave(c);c=null;
}catch(e){checks.push({name:'Fixture run',pass:false,error:e.stack});if(c)try{await leave(c);c=null}catch{}process.exitCode=1}
main?.close();c?.close();await fs.writeFile(path.join(folder,'checks.json'),JSON.stringify(checks,null,2));console.log(JSON.stringify(checks));
