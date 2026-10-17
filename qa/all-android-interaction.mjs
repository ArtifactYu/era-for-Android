import fs from 'node:fs/promises';
import path from 'node:path';
import {CDP,adb,delay,until,leave,work,json} from './android-driver.mjs';
const snap=`(async()=>({state,ready,inFlight,width:innerWidth,height:innerHeight,engine:await(await fetch('/status'+location.search)).json(),stats:await(await fetch('/stats'+location.search)).json(),hash:Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',await(await fetch(screen.src)).arrayBuffer()))).map(n=>n.toString(16).padStart(2,'0')).join('')}))()`;
await fs.mkdir(path.join(work,'all-android-compat'),{recursive:true});
await fs.mkdir(path.join(work,'android-results'),{recursive:true});
const desktop=await json(path.join(work,'all-test-library/library.json'));
let main=await CDP.connect('main');await until(()=>main.eval('typeof games!=="undefined"&&games.length>0'));const games=(await main.eval('games')).filter(g=>desktop.some(d=>d.Name===g.name));
if(!process.argv[2]&&games.length!==desktop.length)throw Error(`Import all ${desktop.length} catalog games first; found ${games.length}.`);
if(process.argv[2]&&!games.some(g=>g.name===process.argv[2]))throw Error('Requested game is not imported.');
await fs.writeFile(path.join(work,'android-results/imported-games.json'),JSON.stringify(games,null,2));
main.close();
for(const game of games){
 if(await fs.stat(path.join(work,'pause-android-batch')).then(()=>true).catch(()=>false))break;
 if(process.argv[2]&&game.name!==process.argv[2])continue;
 const original=desktop.find(g=>g.Name===game.name);
 const probe=await until(async()=>{try{return await json(path.join(work,'all-desktop-interaction',original.Id+'.json'));}catch{return false;}},600000);
 if(!process.argv[2]){try{const done=await json(path.join(work,'all-android-compat',game.id+'.json'));if(done.ok&&(done.profile==='choices-v1'||JSON.stringify(done.steps.slice(1).map(x=>x.input))===JSON.stringify(probe.steps.slice(1).map(x=>x.input))))continue;}catch{}}
 const record={profile:'choices-v1',id:game.id,name:game.name,steps:[],ok:false};let c;
 try{
  main=await CDP.connect('main');await main.eval(`native('play',${JSON.stringify(game.id)})`);main.close();const begin=Date.now();c=await CDP.connect('game');
  await until(()=>c.eval('typeof state!=="undefined"&&screen.naturalWidth>0&&state!=="loading"'),240000);
  record.startMs=Date.now()-begin;await delay(300);let s=await c.eval(snap);record.steps.push({input:'<launch>',...s});if(s.engine.error)throw Error('Engine startup error');if(s.width<=s.height)throw Error('Landscape not applied');

  for(const step of probe.steps.slice(1)){
   await until(()=>c.eval('ready&&!inFlight'),60000);s=await c.eval(snap);let requested=step.input;
   if(process.argv.includes('--live-choices')&&s.state==='integer'){
    const choices=await c.eval("fetch('/choices'+location.search).then(r=>r.json())");
    requested=choices.find(b=>/^[-+]?\d+$/.test(b.value))?.value??requested;
    record.liveChoices=true;
   }
   let value=requested;
   if(s.state==='enter')value='<enter>';else if(value==='<enter>'&&s.state==='integer')value='0';
   const before=s.stats.inputCount;
   // Timed text can start another wait between the snapshot and this call.
   // Check readiness and click in one browser task, retrying only before dispatch.
   const expr=`(()=>{if(!ready||inFlight||document.getElementById('send').disabled)return false;text.value='';let v=${JSON.stringify(requested)};if(state==='enter')v='<enter>';else if(v==='<enter>'&&state==='integer')v='0';let auto=false;if(v!=='<enter>'){if(/^\\d+$/.test(v)){auto=singleInput;for(const d of v)document.querySelector('[data-key="'+d+'"]').click();}else{text.value=v;text.dispatchEvent(new Event('input'));}}if(!auto)document.getElementById('send').click();return {value:v};})()`;
   value=(await until(()=>c.eval(expr),90000)).value;await until(async()=>{const q=await c.eval(`(async()=>({ready:ready&&!inFlight,error:state==='error',stats:await(await fetch('/stats'+location.search)).json()}))()`);return q.error||(q.ready&&q.stats.inputCount>before);},90000);await delay(250);s=await c.eval(snap);record.steps.push({input:value,...s});if(s.engine.error||s.state==='error')throw Error('Engine failed after input '+value);
  }
  record.ok=record.steps.length>1;record.finalChoices=await c.eval("fetch('/choices'+location.search).then(r=>r.json())");record.finalDiagnostics=await c.eval("fetch('/diagnostics'+location.search).then(r=>r.json())");
 }catch(e){record.failure=e.message;if(c){try{record.failureSnapshot=await c.eval(snap);record.failureUi=await c.eval("({disabled:document.getElementById('send').disabled,value:text.value,singleInput,connection:ws?.readyState})");record.diagnostics=await c.eval("fetch('/diagnostics'+location.search).then(r=>r.json())");await c.shot(path.join(work,'all-android-compat',game.id+'-failure.png'));}catch{}}}
 finally{if(c){try{await leave(c);}catch(e){record.exitFailure=e.message;try{adb('shell','am','force-stop','app.eraflow.android');adb('shell','monkey','-p','app.eraflow.android','-c','android.intent.category.LAUNCHER','1');await delay(1200);}catch{}}}await fs.writeFile(path.join(work,'all-android-compat',game.id+'.json'),JSON.stringify(record,null,2));console.log(JSON.stringify({game:game.name,ok:record.ok,inputs:record.steps.length-1,startMs:record.startMs,failure:record.failure}));}
}
