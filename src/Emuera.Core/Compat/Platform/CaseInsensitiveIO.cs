// EraFlow modification: preserve Windows filename semantics on Android.
global using File = MinorShift.Emuera.Compat.CaseFile;
global using Directory = MinorShift.Emuera.Compat.CaseDirectory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using F = System.IO.File;
using D = System.IO.Directory;

namespace MinorShift.Emuera.Compat;

public static class CasePath
{
    static readonly ConcurrentDictionary<string,string> cache=new(StringComparer.OrdinalIgnoreCase);
    public static void Index(string root)
    {
        cache.Clear();if(Path.DirectorySeparatorChar!='/'||!D.Exists(root))return;
        cache[Path.GetFullPath(root)]=Path.GetFullPath(root);
        foreach(var path in D.EnumerateFileSystemEntries(root,"*",new EnumerationOptions{RecurseSubdirectories=true,IgnoreInaccessible=true}))cache[path]=path;
    }
    public static string Resolve(string path)
    {
        if(string.IsNullOrEmpty(path)||Path.DirectorySeparatorChar!='/')return path;
        path=Path.GetFullPath(path.Replace('\\','/'));
        if(F.Exists(path)||D.Exists(path))return path;
        if(cache.TryGetValue(path.TrimEnd('/'),out var found)&&(F.Exists(found)||D.Exists(found)))return found;
        var parent=Path.GetDirectoryName(path);if(string.IsNullOrEmpty(parent)||parent==path)return path;
        string resolved=Resolve(parent),name=Path.GetFileName(path);
        if(D.Exists(resolved))foreach(var entry in D.EnumerateFileSystemEntries(resolved))if(Path.GetFileName(entry).Equals(name,StringComparison.OrdinalIgnoreCase)){cache[path]=entry;return entry;}
        return Path.Combine(resolved,name);
    }
}
public static class CaseFile
{
    public static bool Exists(string path){try{return F.Exists(CasePath.Resolve(path));}catch{return false;}}
    public static FileStream Open(string p,FileMode mode)=>F.Open(CasePath.Resolve(p),mode);
    public static FileStream Open(string p,FileMode mode,FileAccess access)=>F.Open(CasePath.Resolve(p),mode,access);
    public static FileStream Open(string p,FileMode mode,FileAccess access,FileShare share)=>F.Open(CasePath.Resolve(p),mode,access,share);
    public static FileStream OpenRead(string p)=>F.OpenRead(CasePath.Resolve(p));
    public static FileStream Create(string p)=>F.Create(CasePath.Resolve(p));
    public static byte[] ReadAllBytes(string p)=>F.ReadAllBytes(CasePath.Resolve(p));
    public static string ReadAllText(string p)=>F.ReadAllText(CasePath.Resolve(p));
    public static string ReadAllText(string p,Encoding e)=>F.ReadAllText(CasePath.Resolve(p),e);
    public static string[] ReadAllLines(string p,Encoding e)=>F.ReadAllLines(CasePath.Resolve(p),e);
    public static string[] ReadAllLines(string p)=>F.ReadAllLines(CasePath.Resolve(p));
    public static IEnumerable<string> ReadLines(string p,Encoding e)=>F.ReadLines(CasePath.Resolve(p),e);
    public static void WriteAllBytes(string p,byte[] data)=>F.WriteAllBytes(CasePath.Resolve(p),data);
    public static void WriteAllText(string p,string data)=>F.WriteAllText(CasePath.Resolve(p),data);
    public static void WriteAllText(string p,string data,Encoding e)=>F.WriteAllText(CasePath.Resolve(p),data,e);
    public static void Delete(string p)=>F.Delete(CasePath.Resolve(p));
    public static void Move(string a,string b)=>F.Move(CasePath.Resolve(a),CasePath.Resolve(b));
    public static DateTime GetLastWriteTime(string p)=>F.GetLastWriteTime(CasePath.Resolve(p));
    public static FileAttributes GetAttributes(string p)=>F.GetAttributes(CasePath.Resolve(p));
}
public static class CaseDirectory
{
    static EnumerationOptions Options(SearchOption s)=>new(){RecurseSubdirectories=s==SearchOption.AllDirectories,MatchCasing=MatchCasing.CaseInsensitive,IgnoreInaccessible=false,AttributesToSkip=0};
    public static bool Exists(string p){try{return D.Exists(CasePath.Resolve(p));}catch{return false;}}
    public static DirectoryInfo CreateDirectory(string p)=>D.CreateDirectory(CasePath.Resolve(p));
    public static void Delete(string p)=>D.Delete(CasePath.Resolve(p));
    public static void Delete(string p,bool recursive)=>D.Delete(CasePath.Resolve(p),recursive);
    public static string GetCurrentDirectory()=>D.GetCurrentDirectory();
    public static string[] GetFiles(string p,string pattern="*",SearchOption s=SearchOption.TopDirectoryOnly)=>D.GetFiles(CasePath.Resolve(p),pattern,Options(s));
    public static string[] GetFiles(string p,string pattern,EnumerationOptions o)=>D.GetFiles(CasePath.Resolve(p),pattern,o);
    public static string[] GetDirectories(string p,string pattern="*",SearchOption s=SearchOption.TopDirectoryOnly)=>D.GetDirectories(CasePath.Resolve(p),pattern,Options(s));
    public static IEnumerable<string> EnumerateFiles(string p,string pattern="*",SearchOption s=SearchOption.TopDirectoryOnly)=>D.EnumerateFiles(CasePath.Resolve(p),pattern,Options(s));
    public static IEnumerable<string> EnumerateFiles(string p,string pattern,EnumerationOptions o)=>D.EnumerateFiles(CasePath.Resolve(p),pattern,o);
    public static IEnumerable<string> EnumerateDirectories(string p,string pattern="*",SearchOption s=SearchOption.TopDirectoryOnly)=>D.EnumerateDirectories(CasePath.Resolve(p),pattern,Options(s));
    public static IEnumerable<string> EnumerateDirectories(string p,string pattern,EnumerationOptions o)=>D.EnumerateDirectories(CasePath.Resolve(p),pattern,o);
}
