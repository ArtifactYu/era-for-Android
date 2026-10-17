using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Webkit;
using MinorShift.Emuera.WebHost;
using System.Text.Json;

namespace EraFlow;

[Activity(Label="era for Android",Exported=false,Process=":game",Theme="@style/EraPlayTheme",ScreenOrientation=ScreenOrientation.SensorLandscape,WindowSoftInputMode=SoftInput.AdjustResize,ConfigurationChanges=ConfigChanges.ScreenSize|ConfigChanges.Orientation|ConfigChanges.KeyboardHidden)]
public sealed class GameActivity:Activity
{
 WebView web=null!;EmueraWebHost? host;bool exiting;string title="",gameId="";
 protected override async void OnCreate(Bundle? state)
 {
  base.OnCreate(state);Window!.AddFlags(WindowManagerFlags.KeepScreenOn);
  if(OperatingSystem.IsAndroidVersionAtLeast(28))WebView.SetDataDirectorySuffix("game");
  web=new WebView(this);Platform.Configure(this,web,true);Platform.SetContent(this,web);web.AddJavascriptInterface(new JsBridge(Receive),"Android");
  title=Intent?.GetStringExtra("title")??"Era";gameId=Intent?.GetStringExtra("id")??"";
  web.SetWebViewClient(new LocalClient(()=>{Platform.Send(this,web,"title",title);Platform.Send(this,web,"settings",LoadSettings());},u=>u=="about:blank"||u.StartsWith("file:///android_asset/")||(host!=null&&u.StartsWith(host.Url))));
  web.LoadUrl("file:///android_asset/loading.html");
  try
  {
   string path=Path.GetFullPath(Intent?.GetStringExtra("path")??"");string root=Path.Combine(FilesDir!.AbsolutePath,"library")+Path.DirectorySeparatorChar;
   if(!path.StartsWith(root,StringComparison.Ordinal))throw new IOException("游戏路径无效。");
   Platform.Fonts(this);MinorShift.Emuera.Runtime.Utils.PluginSystem.BuiltinPlugins.BrowserLauncher=_=>{};
   host=new EmueraWebHost(path,1000,420){Log=s=>Android.Util.Log.Info("EraFlowEngine",s)};await host.StartAsync();if(!exiting)web.LoadUrl(host.Url);
  }catch(Exception e){Android.Util.Log.Error("EraFlowEngine",e.ToString());Platform.Send(this,web,"error","游戏未能启动："+e.Message);}
 }
 object LoadSettings()
 {
  var p=GetSharedPreferences("player",Android.Content.FileCreationMode.Private)!;string json=p.GetString(gameId,"{\"scale\":1,\"images\":true,\"haptic\":true,\"left\":false,\"compact\":false}")!;try{return JsonSerializer.Deserialize<JsonElement>(json);}catch{return new{scale=1,images=true,haptic=true,left=false,compact=false};}
 }
 void Receive(string json)=>RunOnUiThread(()=>{
  try{using var doc=JsonDocument.Parse(json);var d=doc.RootElement;switch(d.GetProperty("action").GetString())
  {
   case "home":ConfirmExit();break;
   case "haptic":web.PerformHapticFeedback(FeedbackConstants.KeyboardTap);break;
   case "settings":GetSharedPreferences("player",Android.Content.FileCreationMode.Private)!.Edit()!.PutString(gameId,d.GetProperty("value").GetRawText())!.Apply();break;
  }}catch(Exception e){Android.Util.Log.Warn("EraFlow",e.Message);}
 });
 void ConfirmExit(){new AlertDialog.Builder(this).SetTitle("返回游戏库？").SetMessage("请先使用游戏内的存档功能保存进度。").SetNegativeButton("继续游玩",(_,_)=>{}).SetPositiveButton("返回游戏库",(_,_)=>{exiting=true;Finish();}).Show();}
 public override void OnBackPressed()=>ConfirmExit();
 protected override void OnPause(){base.OnPause();web?.OnPause();}
 protected override void OnResume(){base.OnResume();web?.OnResume();}
 protected override void OnDestroy(){exiting=true;web?.Destroy();base.OnDestroy();new Handler(Looper.MainLooper!).PostDelayed(()=>Android.OS.Process.KillProcess(Android.OS.Process.MyPid()),200);}
}
