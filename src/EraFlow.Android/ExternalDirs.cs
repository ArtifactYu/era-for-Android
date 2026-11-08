// EraFlow 外部目录直读扩展
//
// 原版会把游戏从 SAF 目录拷贝到 /data/data/<pkg>/files/library/ 下再运行。
// 本扩展允许直接以 /sdcard/ 等外部目录作为游戏根目录运行，不拷贝：
//   - 存档、配置文件就地读写（和 PC 版 Emuera 一致）
//   - 改 ERB 脚本后重新打开游戏即生效，无需重新导入
//
// 需要 MANAGE_EXTERNAL_STORAGE（Android 11+）或 READ/WRITE_EXTERNAL_STORAGE。

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Provider;

namespace EraFlow;

public static class ExternalDirs
{
    /// <summary>外部存储根（通常 /storage/emulated/0，也兼容多用户路径）。</summary>
    public static string? Root
    {
        get
        {
            try
            {
                var dir = Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath;
                if (dir != null && Directory.Exists(dir)) return dir;
            }
            catch { }
            // 兜底：直接探测常见路径
            foreach (var candidate in new[] { "/storage/emulated/0", "/sdcard", "/mnt/sdcard" })
                if (Directory.Exists(candidate)) return candidate;
            return null;
        }
    }

    /// <summary>应用私有库目录（原版行为，保留作为兜底）。</summary>
    public static string? PrivateLibrary(Context ctx)
        => ctx.FilesDir?.AbsolutePath is { } f ? Path.Combine(f, "library") : null;

    /// <summary>是否已经拿到「所有文件访问」权限。</summary>
    public static bool HasAllFilesAccess(Context ctx)
    {
        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                return Android.OS.Environment.IsExternalStorageManager == true;
            return true; // 10 及以下只要有 READ/WRITE 权限
        }
        catch { return false; }
    }

    /// <summary>请求「所有文件访问」权限（会跳到系统设置页）。</summary>
    public static bool RequestAllFilesAccess(Activity activity)
    {
        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                var intent = new Intent(Settings.ActionManageAllFilesAccessPermission);
                intent.SetData(Android.Net.Uri.Parse("package:" + activity.PackageName));
                activity.StartActivity(intent);
                return true;
            }
            activity.RequestPermissions(new[]
            {
                "android.permission.READ_EXTERNAL_STORAGE",
                "android.permission.WRITE_EXTERNAL_STORAGE"
            }, 2001);
            return true;
        }
        catch (Exception)
        {
            try { activity.StartActivity(new Intent(Settings.ActionManageAllFilesAccessPermission)); }
            catch { }
            return false;
        }
    }

    /// <summary>
    /// 在外部存储里扫描 era 游戏（同时含 erb/ 与 csv/ 的目录）。
    /// 只扫指定深度内的目录，跳过隐藏目录与常见无关目录。
    /// </summary>
    public static IEnumerable<string> DiscoverGames(string root, int maxDepth = 7)
    {
        // 只跳过系统目录；era 游戏完全可能放在 Download/Documents 里
        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Android", "DCIM", "Pictures", "Movies", "Music",
            "Alarms", "Notifications", "Podcasts", "Recordings",
            "Ringtones", ".nomedia"
        };

        var seen = new List<string>();
        Walk(root, 0);
        return seen;

        void Walk(string dir, int depth)
        {
            if (depth > maxDepth) return;
            List<string> children;
            try { children = Directory.EnumerateDirectories(dir).ToList(); }
            catch { return; }

            // 当前目录本身就是游戏？
            if (LooksLikeGame(dir)) { seen.Add(dir); return; }

            foreach (var child in children)
            {
                string name = Path.GetFileName(child);
                if (name.StartsWith('.')) continue;
                if (depth == 0 && skip.Contains(name)) continue;
                Walk(child, depth + 1);
            }
        }
    }

    /// <summary>目录是否同时包含 erb 与 csv（era 游戏的标志）。</summary>
    public static bool LooksLikeGame(string dir)
    {
        try
        {
            if (!Directory.Exists(dir)) return false;
            bool hasErb = Directory.EnumerateDirectories(dir)
                .Any(d => Path.GetFileName(d).Equals("erb", StringComparison.OrdinalIgnoreCase));
            bool hasCsv = Directory.EnumerateDirectories(dir)
                .Any(d => Path.GetFileName(d).Equals("csv", StringComparison.OrdinalIgnoreCase));
            return hasErb && hasCsv;
        }
        catch { return false; }
    }

    /// <summary>判断路径是否在允许的外部根下（供 GameActivity 白名单用）。</summary>
    public static bool IsAllowedGamePath(string path)
    {
        try
        {
            string full = Path.GetFullPath(path);
            if (!Directory.Exists(full)) return false;
            if (!LooksLikeGame(full)) return false;

            string? root = Root;
            if (root == null) return false;
            return full.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar,
                                   StringComparison.OrdinalIgnoreCase)
                || full.StartsWith(Path.GetFullPath("/storage/") , StringComparison.OrdinalIgnoreCase)
                || full.StartsWith(Path.GetFullPath("/sdcard/") , StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }
}
