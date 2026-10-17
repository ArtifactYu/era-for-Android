using EraFlow.Storage;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
string root=Path.Combine(Path.GetTempPath(),"EraFlow-Tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
var results=new List<object>();
void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
void Reject(Action action){try{action();}catch(InvalidDataException){return;}throw new Exception("Invalid archive was accepted");}
string Zip(string name,Dictionary<string,string> entries,Encoding? encoding=null){string path=Path.Combine(root,name+".zip");using var z=new ZipArchive(File.Create(path),ZipArchiveMode.Create,false,encoding);foreach(var p in entries){using var w=new StreamWriter(z.CreateEntry(p.Key).Open(),Encoding.UTF8);w.Write(p.Value);}return path;}
void Test(string name,Action action){try{action();results.Add(new{name,pass=true});}catch(Exception e){results.Add(new{name,pass=false,error=e.Message});Environment.ExitCode=1;}}
try
{
 var lib=new GameLibrary(Path.Combine(root,"library"));
 string gameZip=Zip("game",new(){["游戏/CSV/GAMEBASE.csv"]="タイトル,测试世界",["游戏/ERB/开场.ERB"]="@TITLE\nPRINTL hello\nWAIT"});
 GameEntry? game=null;
 Test("UTF-8 nested game import",()=>{game=lib.ImportZip(gameZip,"game.zip").Single();Assert(game.Name=="测试世界"&&game.Scripts==1,"Game metadata mismatch");});
 Test("Legacy GB18030 filenames",()=>{string p=Zip("legacy",new(){["中文/CSV/GAMEBASE.csv"]="TITLE,legacy",["中文/ERB/开场.ERB"]="@TITLE"},Encoding.GetEncoding(54936));var g=lib.ImportZip(p,"legacy.zip").Single();Assert(File.Exists(Path.Combine(g.Path,"ERB","开场.ERB")),"Filename corrupted");});
 Test("ZIP traversal rejected and staging removed",()=>{var p=Zip("escape",new(){["../escaped.sav"]="bad"});Reject(()=>lib.ImportZip(p,"escape.zip"));Assert(!File.Exists(Path.Combine(root,"escaped.sav")),"Escaped root");Assert(!Directory.GetDirectories(lib.Root,".stage-*").Any(),"Staging leaked");});
 Test("Invalid game rejected",()=>{var p=Zip("invalid",new(){["README.txt"]="No ERB/CSV"});Reject(()=>lib.ImportZip(p,"invalid.zip"));});
 Test("Repeated import preserves saved progress",()=>{File.WriteAllText(Path.Combine(game!.Path,"save01.sav"),"progress-A");var again=lib.ImportZip(gameZip,"different-name.zip").Single();Assert(again.Id==game.Id&&File.ReadAllText(Path.Combine(game.Path,"save01.sav"))=="progress-A","Duplicate import changed progress");});
 Test("Save export/restore round trip with rollback backup",()=>{using var stream=new MemoryStream();lib.ExportSaves(game!,stream);File.WriteAllText(Path.Combine(game!.Path,"save01.sav"),"progress-B");stream.Position=0;lib.ImportSaves(game,stream);Assert(File.ReadAllText(Path.Combine(game.Path,"save01.sav"))=="progress-A","Restore mismatch");using var backup=ZipFile.OpenRead(Directory.GetFiles(Path.Combine(lib.Root,"save-backups")).Single());using var sr=new StreamReader(backup.GetEntry("save01.sav")!.Open());Assert(sr.ReadToEnd()=="progress-B","Original save not backed up");});
 Test("Invalid restore cannot replace scripts or saves",()=>{var p=Zip("badrestore",new(){["save01.sav"]="bad",["ERB/开场.ERB"]="bad"});using var stream=File.OpenRead(p);Reject(()=>lib.ImportSaves(game!,stream));Assert(File.ReadAllText(Path.Combine(game!.Path,"save01.sav"))=="progress-A","Validation changed existing save");});
 Test("Save traversal rejected",()=>{var p=Zip("badpath",new(){["../escape.sav"]="bad"});using var stream=File.OpenRead(p);Reject(()=>lib.ImportSaves(game!,stream));});
 Test("Library persists metadata and recent play",()=>{lib.Played(game!.Id);var fresh=new GameLibrary(lib.Root);fresh.Load();Assert(fresh.Games.Count==2&&fresh.Games.Single(g=>g.Id==game.Id).LastPlayed>0,"Library persistence failed");});
 Test("Safe path rejects rooted and drive paths",()=>{foreach(var p in new[]{"/outside.sav","C:/outside.sav","..\\outside.sav"})Reject(()=>GameLibrary.SafePath(lib.Root,p));});
 Test("Delete removes game, saves and backups",()=>{var p=Zip("solo",new(){["solo/CSV/GAMEBASE.csv"]="TITLE,独行",["solo/ERB/开场.ERB"]="@TITLE"});var g=lib.ImportZip(p,"solo.zip").Single();File.WriteAllText(Path.Combine(g.Path,"save01.sav"),"progress-C");using(var s=new MemoryStream()){lib.ExportSaves(g,s);s.Position=0;lib.ImportSaves(g,s);}Assert(Directory.GetFiles(Path.Combine(lib.Root,"save-backups"),g.Id+"-*.zip").Length>0,"Backup not created");string package=Directory.GetParent(g.Path)!.FullName;Assert(lib.Delete(g.Id)&&lib.Games.All(x=>x.Id!=g.Id),"Entry remains");Assert(!Directory.Exists(g.Path)&&!Directory.Exists(package),"Game files remain");Assert(Directory.GetFiles(Path.Combine(lib.Root,"save-backups"),g.Id+"-*.zip").Length==0,"Backups remain");Assert(lib.ImportZip(p,"solo.zip").Single().Id==g.Id,"Reimport failed after delete");});
 Test("Partial multi-game ZIP import restores only missing games",()=>{var p=Zip("pair",new(){["a/CSV/GAMEBASE.csv"]="TITLE,甲",["a/ERB/开场.ERB"]="@TITLE",["b/CSV/GAMEBASE.csv"]="TITLE,乙",["b/ERB/开场.ERB"]="@TITLE"});var both=lib.ImportZip(p,"pair.zip");Assert(both.Count==2,"Expected two games");var a=both.First(g=>g.Name=="甲");var b=both.First(g=>g.Name=="乙");File.WriteAllText(Path.Combine(b.Path,"save01.sav"),"b-progress");Assert(lib.Delete(a.Id)&&!Directory.Exists(a.Path)&&Directory.Exists(b.Path),"Delete affected sibling");var back=lib.ImportZip(p,"pair.zip");Assert(back.Count==2&&back.Any(g=>g.Id==a.Id),"Backfill missing");Assert(File.ReadAllText(Path.Combine(b.Path,"save01.sav"))=="b-progress","Sibling save changed");});
}
finally{Directory.Delete(root,true);}
Console.WriteLine(JsonSerializer.Serialize(new{tests=results,passed=Environment.ExitCode==0},new JsonSerializerOptions{WriteIndented=true}));
