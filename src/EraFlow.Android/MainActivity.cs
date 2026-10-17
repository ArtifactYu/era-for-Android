using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Webkit;
using EraFlow.Storage;
using System.Text.Json;
using Uri=Android.Net.Uri;

namespace EraFlow;

[Activity(Label="era for Android",MainLauncher=true,Exported=true,Theme="@style/EraTheme",ScreenOrientation=ScreenOrientation.SensorPortrait,ConfigurationChanges=ConfigChanges.ScreenSize|ConfigChanges.Orientation|ConfigChanges.KeyboardHidden)]
public sealed class MainActivity:Activity
{
 WebView web=null!; GameLibrary library=null!; bool busy,openingGame; string? pendingGame;
 Android.Content.ISharedPreferences Prefs=>GetSharedPreferences("eraflow",FileCreationMode.Private)!;
 protected override void OnCreate(Bundle? state)
 {
  base.OnCreate(state);library=new GameLibrary(Path.Combine(FilesDir!.AbsolutePath,"library"));library.Load();
  web=new WebView(this);Platform.Configure(this,web,false);web.AddJavascriptInterface(new JsBridge(Receive),"Android");
  web.SetWebViewClient(new LocalClient(()=>{Render();if(!scanned){scanned=true;if(Prefs.Contains("tree"))_ = ScanSaved();}},u=>u.StartsWith("file:///android_asset/")));
  Platform.SetContent(this,web);web.LoadUrl("file:///android_asset/library.html");
 }
 bool scanned;
 protected override void OnResume(){base.OnResume();openingGame=false;if(library!=null){library.Load();Render();}}
 void Render()=>Platform.Send(this,web,"library",new {games=library.Games.OrderByDescending(g=>g.LastPlayed).Select(g=>new {id=g.Id,name=g.Name,scripts=g.Scripts,images=g.Images,bytes=g.Bytes,lastPlayed=g.LastPlayed}),folder=Prefs.GetString("tree",null)!=null});
 void Message(string text)=>Platform.Send(this,web,"message",text);
 void Progress(string? text)=>Platform.Send(this,web,"progress",text);
 void Receive(string json)=>RunOnUiThread(()=>{
  try {using var doc=JsonDocument.Parse(json);var d=doc.RootElement;string action=d.GetProperty("action").GetString()??"";string id=d.TryGetProperty("id",out var i)?i.GetString()??"":"";
   if(busy && action!="ready")return;
   switch(action)
   {
    case "ready":Render();break;
    case "exit":Finish();break;
    case "folder":StartActivityForResult(new Intent(Intent.ActionOpenDocumentTree).AddFlags(ActivityFlags.GrantReadUriPermission|ActivityFlags.GrantPersistableUriPermission),1);break;
    case "zip":PickFile(2);break;
    case "scan":_ = ScanSaved();break;
    case "play":_ = OpenGame(id);break;
    case "export":pendingGame=id;StartActivityForResult(new Intent(Intent.ActionCreateDocument).AddCategory(Intent.CategoryOpenable).SetType("application/zip").PutExtra(Intent.ExtraTitle,"era-for-Android-存档-"+DateTime.Now.ToString("yyyyMMdd-HHmm")+".zip"),3);break;
    case "restore":pendingGame=id;new AlertDialog.Builder(this).SetTitle("恢复存档").SetMessage("将替换此游戏的同名存档。原存档会先在本机备份。").SetNegativeButton("取消",(_,_)=>{}).SetPositiveButton("选择备份",(_,_)=>PickFile(4)).Show();break;
    case "delete":ConfirmDelete(id);break;
    case "haptic":web.PerformHapticFeedback(Android.Views.FeedbackConstants.KeyboardTap);break;
    case "about":ShowAbout();break;
   }
  }catch(Exception e){Message(e.Message);}
 });
 async Task OpenGame(string id)
 {
  if(openingGame)return;var game=library.Games.Find(g=>g.Id==id);if(game==null)return;openingGame=true;
  try
  {
   var manager=(ActivityManager)GetSystemService(ActivityService)!;
   bool Running()=>manager.RunningAppProcesses?.Any(p=>p.ProcessName==PackageName+":game")==true;
   for(int n=0;n<50&&Running();n++)await Task.Delay(100);
   if(Running()){openingGame=false;Message("上一个游戏正在关闭，请稍候再试");return;}
   library.Played(id);StartActivity(new Intent(this,typeof(GameActivity)).PutExtra("path",game.Path).PutExtra("title",game.Name).PutExtra("id",game.Id));
  }
  catch(Exception e){openingGame=false;Message(e.Message);}
 }
 void ConfirmDelete(string id)
 {
  var game=library.Games.Find(g=>g.Id==id);if(game==null)return;
  var dialog=new AlertDialog.Builder(this).SetTitle("删除游戏？").SetMessage($"「{game.Name}」的游戏文件和存档将从本机删除，无法恢复。\n\n如需保留进度，请先导出存档。").SetNegativeButton("取消",(_,_)=>{}).SetPositiveButton("删除",(s,e)=>{_=DeleteGame(game);}).Create();
  dialog.Show();dialog.GetButton((int)DialogButtonType.Positive)?.SetTextColor(Android.Graphics.Color.ParseColor("#b3261e"));
 }
 async Task DeleteGame(GameEntry game)
 {
  if(openingGame){Message("正在打开游戏，请稍候再试");return;}
  var manager=(ActivityManager)GetSystemService(ActivityService)!;
  bool Running()=>manager.RunningAppProcesses?.Any(p=>p.ProcessName==PackageName+":game")==true;
  for(int n=0;n<50&&Running();n++)await Task.Delay(100);
  if(Running()){Message("游戏仍在运行，请先返回游戏库再试");return;}
  await Work(async()=>{await Task.Run(()=>library.Delete(game.Id));GetSharedPreferences("player",FileCreationMode.Private)!.Edit()!.Remove(game.Id)!.Apply();Message($"已删除「{game.Name}」");},"正在删除…");
 }
 void PickFile(int request)=>StartActivityForResult(new Intent(Intent.ActionOpenDocument).AddCategory(Intent.CategoryOpenable).SetType("*/*").PutExtra(Intent.ExtraAllowMultiple,request==2).AddFlags(ActivityFlags.GrantReadUriPermission),request);
 protected override async void OnActivityResult(int request,Result result,Intent? data)
 {
  base.OnActivityResult(request,result,data);if(result!=Result.Ok||data==null)return;
  var uris=new List<Uri>();if(data.ClipData is { } clips){for(int k=0;k<clips.ItemCount;k++)if(clips.GetItemAt(k)?.Uri is {} item)uris.Add(item);}else if(data.Data is {} item)uris.Add(item);
  if(uris.Count==0)return;var uri=uris[0];
  if(request==1){try{ContentResolver!.TakePersistableUriPermission(uri,ActivityFlags.GrantReadUriPermission);Prefs.Edit()!.PutString("tree",uri.ToString())!.Apply();await ScanSaved();}catch(Exception e){Message("目录授权失败："+e.Message);}return;}
  await Work(async()=>{
   if(request==2){int count=0;var failures=new List<string>();foreach(var selected in uris){string temp=Path.Combine(CacheDir!.AbsolutePath,"import-"+Guid.NewGuid()+".zip");string name=DisplayName(selected);try{Progress("正在读取 · "+name);using(var from=ContentResolver!.OpenInputStream(selected)!)using(var to=File.Create(temp)){await from.CopyToAsync(to);}var added=await Task.Run(()=>library.ImportZip(temp,name,Progress));count+=added.Count;}catch(Exception e){failures.Add(name+"："+e.Message);}finally{if(File.Exists(temp))File.Delete(temp);}}Message($"已读取 {count} 个游戏"+(failures.Count>0?$"，{failures.Count} 个文件未导入":""));if(failures.Count>0)new AlertDialog.Builder(this).SetTitle("部分文件未导入").SetMessage(string.Join("\n\n",failures)).SetPositiveButton("知道了",(_,_)=>{}).Show();}
   else {var game=library.Games.Find(g=>g.Id==pendingGame);if(game==null)return;if(request==3){using var output=ContentResolver!.OpenOutputStream(uri)!;await Task.Run(()=>library.ExportSaves(game,output));Message("存档已导出");}else if(request==4){using var source=ContentResolver!.OpenInputStream(uri)!;string temp=Path.Combine(CacheDir!.AbsolutePath,"restore.zip");try{using(var to=File.Create(temp))await source.CopyToAsync(to);using var input=File.OpenRead(temp);await Task.Run(()=>library.ImportSaves(game,input));Message("存档已恢复");}finally{if(File.Exists(temp))File.Delete(temp);}}}
  });
 }
 string DisplayName(Uri uri){using var c=ContentResolver!.Query(uri,new[]{OpenableColumns.DisplayName},null,null,null);return c?.MoveToFirst()==true?c.GetString(0)??"游戏":"游戏";}
 async Task Work(Func<Task> action,string progress="正在读取文件…")
 {
  if(busy)return;busy=true;Progress(progress);try{await action();library.Save();Render();}catch(Exception e){Android.Util.Log.Error("EraFlow",e.ToString());Message("未完成："+e.Message);}finally{busy=false;Progress(null);}
 }
 async Task ScanSaved()
 {
  string? saved=Prefs.GetString("tree",null);if(saved==null){Message("请先选择游戏目录");return;}
  await Work(async()=>{int before=library.Games.Count;await Task.Run(()=>ScanTree(Uri.Parse(saved)!,DocumentsContract.GetTreeDocumentId(Uri.Parse(saved))!,0));int n=library.Games.Count-before;Message(n>0?$"发现 {n} 个新游戏":"游戏库已是最新");});
 }
 record Document(string Id,string Name,bool Directory,long Size);
 List<Document> Children(Uri tree,string id)
 {
  var uri=DocumentsContract.BuildChildDocumentsUriUsingTree(tree,id);using var cursor=ContentResolver!.Query(uri,new[]{DocumentsContract.Document.ColumnDocumentId,DocumentsContract.Document.ColumnDisplayName,DocumentsContract.Document.ColumnMimeType,DocumentsContract.Document.ColumnSize},null,null,null);
  if(cursor==null)throw new IOException("无法读取目录，请重新选择。");var list=new List<Document>();while(cursor.MoveToNext())list.Add(new(cursor.GetString(0)!,cursor.GetString(1)??"未命名",cursor.GetString(2)==DocumentsContract.Document.MimeTypeDir,cursor.IsNull(3)?0:cursor.GetLong(3)));return list;
 }
 void ScanTree(Uri tree,string id,int depth)
 {
  if(depth>10)return;var children=Children(tree,id);
  if(children.Any(d=>d.Directory&&d.Name.Equals("erb",StringComparison.OrdinalIgnoreCase))&&children.Any(d=>d.Directory&&d.Name.Equals("csv",StringComparison.OrdinalIgnoreCase)))
  {
   string source=tree+"|"+id;if(library.Games.Any(g=>g.Source==source))return;
   string key=GameLibrary.Key(source),stage=Path.Combine(library.Root,".stage-"+key),final=Path.Combine(library.Root,"dir-"+key);library.DeleteInside(stage);Directory.CreateDirectory(stage);
   try{long bytes=0;int files=0;CopyTree(tree,id,stage,ref bytes,ref files,0);if(Directory.Exists(final))throw new IOException("游戏目录已存在。");Directory.Move(stage,final);library.Register(final,source,id.Split('/').Last());library.Save();}finally{library.DeleteInside(stage);}return;
  }
  foreach(var child in children.Where(c=>c.Directory&&!c.Name.StartsWith('.')))ScanTree(tree,child.Id,depth+1);
 }
 void CopyTree(Uri tree,string id,string dest,ref long bytes,ref int files,int depth)
 {
  if(depth>32)throw new IOException("目录层级过深。");
  foreach(var child in Children(tree,id))
  {
   if(child.Name.Contains('/')||child.Name.Contains('\\')||child.Name is "." or "..")throw new IOException("文件名无效。");string path=GameLibrary.SafePath(dest,child.Name);
   if(child.Directory){Directory.CreateDirectory(path);CopyTree(tree,child.Id,path,ref bytes,ref files,depth+1);}
   else{if(++files>200000)throw new IOException("文件数量过多。");using var input=ContentResolver!.OpenInputStream(DocumentsContract.BuildDocumentUriUsingTree(tree,child.Id)!)!;using var output=File.Create(path);byte[] buffer=new byte[65536];int read;while((read=input.Read(buffer))>0){bytes+=read;if(bytes>8L*1024*1024*1024)throw new IOException("游戏超过 8 GB。");output.Write(buffer,0,read);}if(files%100==0)Progress($"正在导入 · {files} 个文件");}
  }
 }
 void ShowAbout()=>new AlertDialog.Builder(this).SetTitle("era for Android 1.1").SetMessage("为文字世界留一块安静的屏幕。\n\n基于 Emuera.EM+EE 与 andEmuera 的修改版本。支持 ERB、ERH、CSV 和游戏图片资源。\n\n引擎原作者：MinorShift、妊）|дﾟ)の中の人、VVII、EvilMask、Enter。Android 兼容移植：BluishCat。\n\n游戏及存档保存在本机；卸载应用前请导出存档。游玩中双击侧栏顶部切换设置。\n\n本应用免费、源码公开，禁止商业倒卖；若你是付费获得，说明卖家在倒卖本应用。\n\n官方仓库：https://github.com/ArtifactYu/era-for-Android\n\n特别感谢：@RikaMundus（X）\n\n完整许可证随安装包和源码提供。").SetPositiveButton("知道了",(_,_)=>{}).Show();
 public override void OnBackPressed(){if(busy){Message("正在处理文件，请稍候");return;}web.EvaluateJavascript("window.Era?.back()",null);}
 protected override void OnDestroy(){web?.RemoveJavascriptInterface("Android");web?.Destroy();base.OnDestroy();}
}
