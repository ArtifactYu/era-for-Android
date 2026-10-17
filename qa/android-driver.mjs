import fs from 'node:fs/promises';
import path from 'node:path';
import {execFileSync} from 'node:child_process';
const work=path.resolve(process.env.ERAFLOW_QA_WORK||'work'), adbPath=process.env.ADB_PATH||path.join(work,'tooling/android/platform-tools/adb.exe');
const env={...process.env,ANDROID_USER_HOME:path.join(work,'tooling/android-user'),ANDROID_SDK_HOME:path.join(work,'tooling/android-user')};
const adb=(...args)=>execFileSync(adbPath,args,{env,encoding:'utf8',timeout:30000,windowsHide:true}).trim();
const delay=ms=>new Promise(r=>setTimeout(r,ms));
const json=async p=>JSON.parse((await fs.readFile(p,'utf8')).replace(/^\uFEFF/,''));
async function until(fn,ms=90000){let last;const start=Date.now();while(Date.now()-start<ms){try{const v=await fn();if(v)return v;}catch(e){last=e;}await delay(500);}throw Error('Timed out: '+(last?.message||'state not ready'));}
class CDP{
 static async connect(kind){const port=kind==='game'?9223:9222;const page=await until(async()=>{const pid=adb('shell','pidof','app.eraflow.android'+(kind==='game'?':game':''));if(!pid)return false;adb('forward','tcp:'+port,'localabstract:webview_devtools_remote_'+pid);const a=await(await fetch('http://127.0.0.1:'+port+'/json')).json();return a.find(p=>p.type==='page');});const c=new CDP;c.ws=new WebSocket(page.webSocketDebuggerUrl);c.seq=0;c.pending=new Map();await new Promise((r,j)=>{c.ws.onopen=r;c.ws.onerror=j});c.ws.onmessage=e=>{const m=JSON.parse(e.data);if(m.id){c.pending.get(m.id)?.(m);c.pending.delete(m.id);}};return c;}
 call(method,params={}){return new Promise((r,j)=>{const id=++this.seq;const t=setTimeout(()=>{this.pending.delete(id);j(Error('CDP timeout '+method));},30000);this.pending.set(id,m=>{clearTimeout(t);if(m.error)j(Error(method+': '+m.error.message));else r(m);});this.ws.send(JSON.stringify({id,method,params}));});}
 async eval(expression){const m=await this.call('Runtime.evaluate',{expression,returnByValue:true,awaitPromise:true,userGesture:true});if(m.result?.exceptionDetails)throw Error(m.result.exceptionDetails.text+' '+m.result.result?.description);return m.result?.result?.value;}
 async shot(p){const m=await this.call('Page.captureScreenshot',{format:'png'});await fs.writeFile(p,Buffer.from(m.result.data,'base64'));}
 close(){this.ws.close();}
}
async function leave(c){await c.eval('native("home")');await delay(400);adb('shell','uiautomator','dump','/sdcard/qa.xml');const xml=adb('shell','cat','/sdcard/qa.xml');const nodes=[...xml.matchAll(/<node\s[^>]+>/g)].map(m=>m[0]);const n=nodes.find(n=>n.includes('resource-id="android:id/button1"'));if(!n)throw Error('Exit confirmation missing');const b=n.match(/bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"/).slice(1).map(Number);adb('shell','input','tap',String(Math.round((b[0]+b[2])/2)),String(Math.round((b[1]+b[3])/2)));c.close();await until(()=>{try{return !adb('shell','pidof','app.eraflow.android:game');}catch{return true;}},15000);await delay(1200);}

export {CDP,adb,delay,until,leave,work,json};
