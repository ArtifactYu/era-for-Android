using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EraFlow.Storage;

public sealed record GameEntry(string Id, string Name, string Path, int Scripts, int Images, long Bytes, string Source, long LastPlayed = 0);

public sealed class GameLibrary
{
    public string Root { get; }
    public GameLibrary(string root) { Root=System.IO.Path.GetFullPath(root); Directory.CreateDirectory(Root); Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); }
    public List<GameEntry> Games { get; private set; } = [];
    public void Load() { var path=System.IO.Path.Combine(Root,"library.json"); if(File.Exists(path)) Games=JsonSerializer.Deserialize<List<GameEntry>>(File.ReadAllText(path)) ?? []; Games.RemoveAll(g=>!Directory.Exists(g.Path)); }
    public void Save() { var path=System.IO.Path.Combine(Root,"library.json"); File.WriteAllText(path+".tmp",JsonSerializer.Serialize(Games)); File.Move(path+".tmp",path,true); }
    public void Played(string id) { Games=Games.Select(g=>g.Id==id?g with {LastPlayed=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}:g).ToList(); Save(); }
    public static string Key(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..20].ToLowerInvariant();
    public static string SafePath(string root,string relative)
    {
        relative=relative.Replace('\\','/');
        if(relative.Split('/').Any(p=>p==".." || p.Contains(':') || p.IndexOf('\0')>=0) || relative.StartsWith('/')) throw new InvalidDataException("压缩包包含不安全的路径。");
        string full=System.IO.Path.GetFullPath(System.IO.Path.Combine(root,relative));
        if(!full.StartsWith(System.IO.Path.GetFullPath(root)+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("文件路径超出了游戏目录。");
        return full;
    }
    public static bool IsSave(string path)=>path.Replace('\\','/').Split('/').Any(p=>p.Equals("sav",StringComparison.OrdinalIgnoreCase)||p.Equals("saves",StringComparison.OrdinalIgnoreCase)) || path.EndsWith(".sav",StringComparison.OrdinalIgnoreCase) || System.IO.Path.GetFileName(path).Equals("global.dat",StringComparison.OrdinalIgnoreCase);
    public static string? Subdir(string path,string name)=>Directory.EnumerateDirectories(path).FirstOrDefault(d=>System.IO.Path.GetFileName(d).Equals(name,StringComparison.OrdinalIgnoreCase));
    public static IEnumerable<string> Discover(string root,int depth=0)
    {
        if(depth>12) yield break;
        if(Subdir(root,"csv") is not null && Subdir(root,"erb") is not null) { yield return root; yield break; }
        foreach(var d in Directory.EnumerateDirectories(root))
        {
            if(System.IO.Path.GetFileName(d).StartsWith('.') || (File.GetAttributes(d)&FileAttributes.ReparsePoint)!=0) continue;
            foreach(var g in Discover(d,depth+1)) yield return g;
        }
    }
    public List<GameEntry> ImportZip(string file,string source,Action<string>? progress=null)
    {
        using var stream=File.OpenRead(file); string hash=Convert.ToHexString(SHA256.HashData(stream)); string tag="zip:"+hash;
        string final=System.IO.Path.Combine(Root,"zip-"+hash[..20].ToLowerInvariant());
        var existing=Games.Where(g=>g.Source==tag).ToList();
        if(existing.Count>0)
        {
            // Fast path: every game in the archive is already registered, so avoid unpacking it again.
            stream.Position=0; using var peek=OpenArchive(stream);
            var expected=ArchiveRoots(peek.Entries.Select(e=>e.FullName));
            if(expected.Count>0 && expected.All(rel=>existing.Any(g=>SamePath(g.Path,System.IO.Path.Combine(final,rel)) && Directory.Exists(g.Path)))) return existing;
        }
        string stage=System.IO.Path.Combine(Root,".stage-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(stage);
        try
        {
            stream.Position=0; using var zip=OpenArchive(stream);
            if(zip.Entries.Count>200000) throw new InvalidDataException("压缩包文件数量超过 200000。");
            long total=0; int n=0;
            foreach(var entry in zip.Entries)
            {
                string dest=SafePath(stage,entry.FullName);
                if(((entry.ExternalAttributes>>16)&0xF000)==0xA000) throw new InvalidDataException("不支持符号链接。");
                if(entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\')) { Directory.CreateDirectory(dest); continue; }
                total+=entry.Length; if(total>8L*1024*1024*1024 || entry.Length>1024L*1024*1024) throw new InvalidDataException("压缩包展开后过大。");
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest)!);
                using(var src=entry.Open()) using(var output=File.Create(dest)) { CopyBounded(src,output,entry.Length); }
                if(++n%100==0) progress?.Invoke($"正在导入 · {n} / {zip.Entries.Count}");
            }
            var roots=Discover(stage).ToList(); if(roots.Count==0) throw new InvalidDataException("未找到同时包含 ERB 和 CSV 的游戏目录。");
            if(!Directory.Exists(final)) Directory.Move(stage,final);
            else
            {
                // A repeated import keeps games that are still present (and their saves); only missing ones are added back.
                foreach(var game in roots)
                {
                    string target=System.IO.Path.Combine(final,System.IO.Path.GetRelativePath(stage,game));
                    if(Directory.Exists(target)) continue;
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);
                    Directory.Move(game,target);
                }
            }
            foreach(var game in roots) Register(System.IO.Path.Combine(final,System.IO.Path.GetRelativePath(stage,game)),tag,System.IO.Path.GetFileNameWithoutExtension(source));
            Save(); return Games.Where(g=>g.Source==tag).ToList();
        }
        finally { DeleteInside(stage); }
    }
    public GameEntry Register(string path,string source,string fallback)
    {
        string id=Key(path); var existing=Games.Find(g=>g.Id==id); if(existing!=null) return existing;
        var files=Directory.GetFiles(path,"*",SearchOption.AllDirectories); string title=fallback;
        string? gamebase=files.FirstOrDefault(f=>System.IO.Path.GetFileName(f).Equals("gamebase.csv",StringComparison.OrdinalIgnoreCase));
        if(gamebase!=null)
        {
            foreach(var line in File.ReadLines(gamebase,Encoding.UTF8))
            {
                int comma=line.IndexOf(','); if(comma<0) continue;
                if(line[..comma].Trim('\uFEFF',' ','\t') is "タイトル" or "TITLE" or "标题") { var value=line[(comma+1)..].Trim(); if(!string.IsNullOrWhiteSpace(value) && !value.Contains('\uFFFD')) title=value; }
            }
        }
        if(title.Length>100) title=title[..100];
        var game=new GameEntry(id,title,path,files.Count(f=>f.EndsWith(".erb",StringComparison.OrdinalIgnoreCase)),files.Count(f=>new[]{".png",".jpg",".jpeg",".webp",".gif",".bmp"}.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant())),files.Sum(f=>new FileInfo(f).Length),source);
        Games.Add(game); return game;
    }
    public void ExportSaves(GameEntry game,Stream output)
    {
        using var zip=new ZipArchive(output,ZipArchiveMode.Create,true);
        foreach(var file in Directory.EnumerateFiles(game.Path,"*",SearchOption.AllDirectories).Where(f=>IsSave(System.IO.Path.GetRelativePath(game.Path,f))))
        { var entry=zip.CreateEntry(System.IO.Path.GetRelativePath(game.Path,file).Replace('\\','/')); using var target=entry.Open(); using var source=File.OpenRead(file); source.CopyTo(target); }
    }
    public void ImportSaves(GameEntry game,Stream input)
    {
        using var zip=new ZipArchive(input,ZipArchiveMode.Read,true); long size=0;
        if(zip.Entries.Count>20000) throw new InvalidDataException("存档备份文件数量过多。");
        foreach(var e in zip.Entries) { SafePath(game.Path,e.FullName); if(e.FullName.EndsWith('/')) continue; if(!IsSave(e.FullName)) throw new InvalidDataException("文件不是存档备份。"); size+=e.Length; if(size>512L*1024*1024) throw new InvalidDataException("存档备份过大。"); }
        string stage=System.IO.Path.Combine(Root,".restore-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
        try
        {
            // Validate every compressed file before touching the existing saves.
            foreach(var e in zip.Entries) { if(e.FullName.EndsWith('/')) continue; string dest=SafePath(stage,e.FullName); Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest)!); using var from=e.Open(); using var to=File.Create(dest); CopyBounded(from,to,e.Length); }
            string backup=System.IO.Path.Combine(Root,"save-backups",game.Id+"-"+DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")+".zip"); Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup)!);
            using(var output=File.Create(backup)) ExportSaves(game,output);
            foreach(var file in Directory.EnumerateFiles(stage,"*",SearchOption.AllDirectories)) { string dest=SafePath(game.Path,System.IO.Path.GetRelativePath(stage,file)); Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dest)!); File.Move(file,dest,true); }
        }
        finally { DeleteInside(stage); }
    }
    public bool Delete(string id)
    {
        var game=Games.Find(g=>g.Id==id); if(game==null) return false;
        Games.RemoveAll(g=>g.Id==id);
        string path=System.IO.Path.GetFullPath(game.Path), prefix=Root+System.IO.Path.DirectorySeparatorChar;
        bool inside=path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase);
        if(inside && !Games.Any(g=>Below(g.Path,path))) DeleteInside(path);
        string backups=System.IO.Path.Combine(Root,"save-backups");
        if(Directory.Exists(backups)) foreach(var file in Directory.EnumerateFiles(backups,game.Id+"-*.zip")) File.Delete(file);
        if(inside)
        {
            string package=System.IO.Path.Combine(Root,System.IO.Path.GetRelativePath(Root,path).Split(System.IO.Path.DirectorySeparatorChar)[0]);
            if(!SamePath(package,Root) && Directory.Exists(package) && !Games.Any(g=>SamePath(g.Path,package)||Below(g.Path,package))) DeleteInside(package);
        }
        Save(); return true;
    }
    static bool Below(string candidate,string parent)
    {
        string c=System.IO.Path.GetFullPath(candidate), p=System.IO.Path.GetFullPath(parent);
        return c.StartsWith(p+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);
    }
    static bool SamePath(string a,string b)=>System.IO.Path.GetFullPath(a).Equals(System.IO.Path.GetFullPath(b),StringComparison.OrdinalIgnoreCase);
    static List<string> ArchiveRoots(IEnumerable<string> names)
    {
        // Mirrors Discover() over the archive's directory names, without unpacking it.
        var dirs=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var name in names)
        {
            string p=name.Replace('\\','/'); bool folder=p.EndsWith('/'); p=p.Trim('/');
            if(!folder) { int cut=p.LastIndexOf('/'); p=cut<0?"":p[..cut]; }
            while(p.Length>0) { dirs.Add(p); int cut=p.LastIndexOf('/'); if(cut<0) break; p=p[..cut]; }
        }
        var roots=new List<string>();
        Walk("",0);
        return roots;
        void Walk(string dir,int depth)
        {
            if(depth>12) return;
            string prefix=dir.Length==0?"":dir+"/";
            if(dirs.Contains(prefix+"csv") && dirs.Contains(prefix+"erb")) { roots.Add(dir); return; }
            foreach(var child in dirs.Where(d=>IsChild(d,prefix)).OrderBy(d=>d,StringComparer.Ordinal))
            {
                if(child[prefix.Length..].StartsWith('.')) continue;
                Walk(child,depth+1);
            }
        }
        static bool IsChild(string path,string prefix)=>prefix.Length==0?!path.Contains('/'):path.Length>prefix.Length&&path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)&&path.IndexOf('/',prefix.Length)<0;
    }
    static void CopyBounded(Stream source,Stream dest,long expected) { byte[] buffer=new byte[65536]; long n=0; int read; while((read=source.Read(buffer))>0) {n+=read;if(n>expected)throw new InvalidDataException("压缩包大小不匹配。");dest.Write(buffer,0,read);} if(n!=expected)throw new InvalidDataException("文件不完整。"); }
    static ZipArchive OpenArchive(Stream stream)
    {
        // Several real Era distributions store UTF-8 names without ZIP's UTF-8 flag.
        // Validate the names before choosing a legacy decoder, or paths get corrupted.
        ZipArchive? zip=null;
        try { zip=new ZipArchive(stream,ZipArchiveMode.Read,true,new UTF8Encoding(false,true)); foreach(var e in zip.Entries) _=e.FullName; return zip; }
        catch(DecoderFallbackException) { zip?.Dispose();stream.Position=0;return new ZipArchive(stream,ZipArchiveMode.Read,true,Encoding.GetEncoding(54936)); }
    }
    public void DeleteInside(string path) { string p=System.IO.Path.GetFullPath(path); if(!p.StartsWith(Root+System.IO.Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(); if(Directory.Exists(p)) Directory.Delete(p,true); }
}
