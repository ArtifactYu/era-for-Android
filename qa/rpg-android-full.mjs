import fs from 'node:fs/promises';
import path from 'node:path';
import {CDP,adb,delay,until,leave,work,json} from './android-driver.mjs';
const folder=path.join(work,'rpg-android');await fs.mkdir(folder,{recursive:true});
const record={game:'eraRPG',steps:[],checks:[],started:new Date().toISOString()};
const check=(name,pass,detail)=>{record.checks.push({name,pass:!!pass,detail});if(!pass)throw Error(name)};
const diag="fetch('/diagnostics'+location.search).then(r=>r.json())";
let c,main,game;
async function checkpoint(){await fs.writeFile(path.join(folder,'result.json'),JSON.stringify(record,null,2));}
async function launch(){main=await CDP.connect('main');await until(()=>main.eval('typeof games!=="undefined"&&games.length>0'));game??=await main.eval('games.find(g=>g.name==="eraRPG")');check('Game imported',game);await main.eval(`native('play',${JSON.stringify(game.id)})`);main.close();c=await CDP.connect('game');await until(()=>c.eval('typeof ready!=="undefined"&&ready&&screen.naturalWidth>0'),240000);await delay(500);check('Landscape',await c.eval('innerWidth>innerHeight'));}
async function touch(selector){const r=await c.eval(`(()=>{const r=document.querySelector(${JSON.stringify(selector)}).getBoundingClientRect();return{x:r.x+r.width/2,y:r.y+r.height/2}})()`);await c.call('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[r]});await delay(100);await c.call('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await delay(120);}
async function snapshot(input){const state=await c.eval(`(async()=>({state,ready,inFlight,lines:await(${diag}),choices:await(await fetch('/choices'+location.search)).json(),stats:await(await fetch('/stats'+location.search)).json(),engine:await(await fetch('/status'+location.search)).json(),hash:Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',await(await fetch(screen.src)).arrayBuffer()))).map(n=>n.toString(16).padStart(2,'0')).join('')}))()`);record.steps.push({input,...state});check('No engine error at step '+(record.steps.length-1),!state.engine.error);await checkpoint();return state;}
async function input(value){
 await until(()=>c.eval('ready&&!inFlight'),180000);
 const count=await c.eval("fetch('/stats'+location.search).then(r=>r.json()).then(s=>s.inputCount)");
 const immediate=await c.eval('singleInput')&&/^\d+$/.test(value);
 await c.eval("text.value=''");
 if(value!=='<enter>'){
  if(/^\d+$/.test(value)){for(const digit of value)await touch('[data-key="'+digit+'"]')}
  else{
   await touch('#keyboard');await delay(700);await c.call('Input.insertText',{text:value});
   check('Soft keyboard leaves confirmation visible',await c.eval("$('send').getBoundingClientRect().bottom<=innerHeight"));
  }
 }
 if(!immediate){
  if(value!=='<enter>')await until(()=>c.eval('text.value==='+JSON.stringify(value)),5000);
  await touch('#send');
 }
 await until(async()=>{const q=await c.eval("(async()=>({ready:ready&&!inFlight,error:state==='error',count:(await(await fetch('/stats'+location.search)).json()).inputCount}))()");return q.error||(q.ready&&q.count>count)},180000);
 await delay(150);return snapshot(value);
}
async function restUntilSaved(){
 for(let hour=0;hour<27;hour++){
  let s=await input('7001');let saved=s.lines.some(l=>l.includes('已自动保存'));
  for(let i=0;i<30&&s.state==='enter';i++){s=await input('<enter>');saved ||= s.lines.some(l=>l.includes('已自动保存'))}
  if(saved){check('Game confirmed its daily autosave',true);return s}
 }
 throw Error('No daily autosave message after resting');
}
try{
 await launch();await snapshot('<launch>');
 const transcript=(await fs.readFile(path.join(work,'text-playthrough-v3/transcript.jsonl'),'utf8')).trim().split('\n').map(JSON.parse);
 const setup=transcript.filter(x=>x.index>0&&x.index<=50).map(x=>x.input);
 // The route uses the game menus and a solution planned from its public script.
 // Never write game variables or save data directly.
 for(const v of setup)await input(v);
 const lab=await json(path.join(work,'rpg-lab-inputs.json'));for(const v of lab.slice(0,-1))await input(v);
 let s=await input('9999');check('World menu reached before graduation',s.lines.some(l=>l.includes('上学')));
 s=await restUntilSaved();check('Time advanced to saved day',s.lines.some(l=>l.includes('时，')));
 await c.shot(path.join(folder,'world.png'));
 // Exercise settings through touch, then preserve readable zoom across a restart.
 await touch('#modeToggle');await c.eval("$('scale').value=150;$('scale').dispatchEvent(new Event('input'));$('scale').dispatchEvent(new Event('change'))");await delay(800);check('Font scale applied',await c.eval('setting.scale===1.5&&screen.getBoundingClientRect().width>wrap.clientWidth*1.49'));await c.shot(path.join(folder,'settings.png'));await touch('#modeToggle');
 const before=s.lines.find(l=>/\d{4}年.*时，/.test(l)),equipment=s.lines.find(l=>l.startsWith('钱包：'));await leave(c);c=null;await launch();check('Font setting persisted',await c.eval('setting.scale===1.5'));await input('1');s=await snapshot('<reloaded>');check('Money and equipment restored',s.lines.includes(equipment),equipment);check('Saved time restored',s.lines.includes(before),{before,after:s.lines.find(l=>/\d{4}年.*时，/.test(l))});
 s=await input('7000');check('University menu restored',s.choices.some(x=>x.value==='4444'));
 s=await input('4444');check('Normal graduation ending reached',s.lines.some(l=>l.includes('恭喜')&&l.includes('成功取得学位')));await c.shot(path.join(folder,'graduation.png'));
 for(let i=0;i<5&&s.state==='enter';i++)s=await input('<enter>');
 check('Degree menu unlocked',s.choices.some(x=>x.value==='6666'));await input('9999');await restUntilSaved();await leave(c);c=null;await launch();await input('1');await input('7000');s=await snapshot('<degree-after-restart>');check('Graduation persists after saving and restart',s.choices.some(x=>x.value==='6666'));await leave(c);c=null;
 record.pass=true;
}catch(e){record.pass=false;record.failure=e.stack;if(c){try{record.last=await c.eval(diag);await c.shot(path.join(folder,'failure.png'));await leave(c);}catch{}}process.exitCode=1;}
main?.close();c?.close();record.finished=new Date().toISOString();await checkpoint();console.log(JSON.stringify({pass:record.pass,inputs:record.steps.length,checks:record.checks.length,failure:record.failure}));
