using EraFlow.Storage;
using MinorShift.Emuera.Api;
using MinorShift.Emuera.Forms;
using MinorShift.Emuera.WebHost;
using System.Text.Json;
using System.Diagnostics;
using System.Security.Cryptography;
Console.OutputEncoding=System.Text.Encoding.UTF8;
Console.InputEncoding=System.Text.Encoding.UTF8;
if(args[0]=="import"){
 var library=new GameLibrary(args[1]);library.Load();foreach(var file in Directory.GetFiles(args[2],"*.zip")){try{var timer=Stopwatch.StartNew();var games=library.ImportZip(file,Path.GetFileName(file));Console.WriteLine(JsonSerializer.Serialize(new{zip=Path.GetFileName(file),ok=true,ms=timer.ElapsedMilliseconds,games}));}catch(Exception e){Console.WriteLine(JsonSerializer.Serialize(new{zip=Path.GetFileName(file),ok=false,error=e.ToString()}));}}return;
}
if(args[0]=="serve"){
 var web=new EmueraWebHost(args[1],1000,420,8321);await web.StartAsync();Console.WriteLine(web.Url);await Task.Delay(Timeout.Infinite);return;
}
if(args[0]=="session"){
 var host=new ProbeHost();var engine=await EmueraEngine.StartAsync(args[1],host,1000,420,true);
 void Snapshot(){lock(host.Gate){engine.EnsureRendered();Console.WriteLine(JsonSerializer.Serialize(new{error=engine.IsError,mode=engine.InputMode.ToString(),single=engine.IsSingleInput,choices=engine.GetSelectableChoices(),lines=engine.GetDisplayLines().TakeLast(50).ToArray()}));}}
 Snapshot();string? line;
 while((line=Console.ReadLine())!=null){try{lock(host.Gate){if(line=="<enter>")engine.PressEnter();else if(line=="<snapshot>"){}else engine.SubmitInput(line);}Snapshot();}catch(Exception e){Console.WriteLine(JsonSerializer.Serialize(new{failure=e.ToString()}));}}
 engine.Dispose();return;
}
if(args[0] is "probe" or "auto"){
 var host=new ProbeHost();var timer=Stopwatch.StartNew();var engine=await EmueraEngine.StartAsync(args[1],host,1000,420,true);var start=timer.ElapsedMilliseconds;var steps=new List<object>();
 void WaitForPrompt(){
  if(args[0]!="auto")return;
  var wait=Stopwatch.StartNew();
  while(true){
   lock(host.Gate){if(engine.IsError||engine.InputMode is not (EmueraInputMode.Void or EmueraInputMode.None))return;}
   if(wait.ElapsedMilliseconds>90000)throw new TimeoutException("Game did not reach an input prompt");
   Thread.Sleep(25);
  }
 }
 WaitForPrompt();
 engine.EnsureRendered();steps.Add(new{input="<launch>",error=engine.IsError,mode=engine.InputMode.ToString(),hash=Convert.ToHexString(SHA256.HashData(engine.RenderPng())),lines=engine.GetDisplayLines().TakeLast(30).ToArray()});
 IEnumerable<string> Inputs(){
  if(args[0]=="probe"){foreach(var s in args.Skip(2))yield return s;yield break;}
  var visits=new Dictionary<string,int>();
  for(int index=0;index<24&&!engine.IsError;index++){
   if(engine.InputMode==EmueraInputMode.EnterKey){yield return "<enter>";continue;}
   var buttonJson=JsonSerializer.SerializeToElement(engine.GetSelectableChoices());
   if(engine.InputMode==EmueraInputMode.String){yield return buttonJson.GetArrayLength()>0?buttonJson[0].GetProperty("value").GetString()!:"Traveler";continue;}
   var lines=engine.GetDisplayLines().TakeLast(24).ToArray();string signature=string.Join('\n',lines);int seen=visits.GetValueOrDefault(signature);visits[signature]=seen+1;if(seen>3)yield break;
   var choices=buttonJson.EnumerateArray().Select(b=>(Value:b.GetProperty("value").GetString()!,Line:b.GetProperty("text").GetString()!)).Where(b=>!string.IsNullOrEmpty(b.Value)&&!b.Value.StartsWith("__")&&(engine.InputMode!=EmueraInputMode.Integer||long.TryParse(b.Value,out _))).DistinctBy(b=>b.Value).ToList();foreach(var line in choices.Count>0?Array.Empty<string>():lines){foreach(System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(line,@"\[\s*(-?\d+)\s*\]"))choices.Add((m.Groups[1].Value,line));}
   var confirm=choices.LastOrDefault(c=>System.Text.RegularExpressions.Regex.IsMatch(c.Line,"ＯＫ|OK|就这样|完成|決定|これで|确认|確定|作成終了|終了する"));
   var startChoice=choices.FirstOrDefault(c=>System.Text.RegularExpressions.Regex.IsMatch(c.Line,"开始游戏|開始遊戲|最初から|初めから|はじめから|NEW GAME|New Game|新規|冒险启程|新しく|从头开始"));
   string value=confirm.Value??startChoice.Value??choices.FirstOrDefault(c=>c.Value=="0").Value??choices.FirstOrDefault(c=>!c.Line.Contains("返回")&&!c.Line.Contains("戻る")&&!c.Line.Contains("取消")).Value??"0";
   if(seen>0&&choices.Count>1)value=choices[(seen)%choices.Count].Value;
   yield return value;
  }
 }
 foreach(var input in Inputs()) {if(input.StartsWith("--shot=")){engine.SaveScreenshot(input[7..]);continue;}if(engine.IsError)break;timer.Restart();object result;lock(host.Gate){result=input=="<enter>"?engine.PressEnter():engine.SubmitInput(input);}WaitForPrompt();lock(host.Gate){engine.EnsureRendered();steps.Add(new{input,error=engine.IsError,mode=engine.InputMode.ToString(),result=result.ToString(),ms=timer.ElapsedMilliseconds,hash=Convert.ToHexString(SHA256.HashData(engine.RenderPng())),lines=engine.GetDisplayLines().TakeLast(30).ToArray()});}}
 Console.WriteLine(JsonSerializer.Serialize(new{path=args[1],title=host.Title,startMs=start,error=engine.IsError,steps}));engine.Dispose();return;
}
sealed class ProbeHost:IWindowHost{
 public readonly object Gate=new();public bool TryRunExclusive(Action action){if(!Monitor.TryEnter(Gate))return false;try{action();return true;}finally{Monitor.Exit(Gate);}}
 public string Title="";public void RequestRedraw(){}public void SetTitle(string title)=>Title=title;public void SetInputText(string text){}public void SetInputPosition(int xOffset,int yOffset,int width){}public void ResetInputPosition(){}public void ShowToolTip(string text,int x,int y){}public void RequestClose(){}public void RequestReboot(){}
}
