import fs from 'node:fs/promises';
import path from 'node:path';
import {CDP,delay,until,leave,work} from './android-driver.mjs';
let main,c;const checks=[];
function check(name,pass){checks.push({name,pass:!!pass});if(!pass)throw Error(name)}
async function touch(selector){const p=await c.eval(`(()=>{const r=document.querySelector(${JSON.stringify(selector)}).getBoundingClientRect();return{x:r.x+r.width/2,y:r.y+r.height/2}})()`);await c.call('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[p]});await delay(100);await c.call('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await delay(150)}
try{
 main=await CDP.connect('main');await until(()=>main.eval('typeof games!=="undefined"'));
 const game=await main.eval('games.find(g=>g.name==="Keyboard Only")');check('Keyboard-only fixture imported',game);
 await main.eval(`native('play',${JSON.stringify(game.id)})`);main.close();c=await CDP.connect('game');await until(()=>c.eval('typeof ready!=="undefined"&&ready&&screen.naturalWidth>0'));
 check('WAIT shows Continue with mouse disabled',await c.eval('state==="enter"&&text.value===""'));
 await touch('#send');await until(()=>c.eval('ready&&state==="string"'));check('INPUTS switches to text input',true);
 await touch('#text');await delay(700);await c.call('Input.insertText',{text:'hello'});check('Typed word remains intact',await c.eval('text.value==="hello"'));
 await touch('#send');await until(()=>c.eval('ready&&state==="integer"'));await delay(700);check('INPUT switches to numeric input',true);
 await touch('[data-key="4"]');await touch('[data-key="2"]');check('Two-digit input remains buffered',await c.eval('text.value==="42"'));
 await touch('#send');await until(()=>c.eval('ready&&state==="enter"'));check('Sequence reaches final WAIT',true);
 check('No engine error',!await c.eval("fetch('/status'+location.search).then(r=>r.json()).then(s=>s.error)"));
 await c.shot(path.join(work,'keyboard-only-release.png'));await leave(c);c=null;
}catch(e){checks.push({name:e.message,pass:false});process.exitCode=1;}
finally{if(c)try{await leave(c)}catch{}main?.close();await fs.writeFile(path.join(work,'keyboard-only-android.json'),JSON.stringify({pass:checks.every(c=>c.pass),checks},null,2));console.log(JSON.stringify(checks));}
