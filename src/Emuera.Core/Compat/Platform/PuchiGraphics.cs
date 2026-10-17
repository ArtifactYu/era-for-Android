using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using MinorShift.Emuera.Api;
using MinorShift.Emuera.Runtime.Utils;
using MinorShift.Emuera.UI.Game.Image;
using SkiaSharp;

namespace MinorShift.Emuera.GameView;

// PuchiEmuera's fixed resource dialect is separate from ordinary inline images.
// Coordinates remain in the game's configured canvas, including negative offsets.
internal sealed class PuchiGraphics
{
    internal sealed record Sprite(string Name,int X,int Y,float Alpha,bool Root,int Group,bool Reverse=false,int[] Back=null,long Line=-1);
    static readonly ConcurrentDictionary<string,Sprite> definitions=new(StringComparer.OrdinalIgnoreCase);
    public static bool FontEdge;
    public static void Reset(){ definitions.Clear(); FontEdge=false; }
    static float Number(string s)=>float.Parse(s,NumberStyles.Float,CultureInfo.InvariantCulture);
    public static void Register(string[] fields)
    {
        if(fields.Length<8||!fields[6].Trim().Equals("FIXED",StringComparison.OrdinalIgnoreCase))return;
        var values=fields[7].Split('|'); if(values.Length<3)throw new CodeEE("Invalid FIXED image definition: "+fields[0]);
        var name=fields[0].Trim(); definitions[name]=new(name,int.Parse(values[0]),int.Parse(values[1]),Math.Clamp(Number(values[2]),0,1),values.Length>3&&values[3].Trim().Equals("ROOT",StringComparison.OrdinalIgnoreCase),values.Length>4?int.Parse(values[4]):-1);
    }
    readonly List<Sprite> visible=new();
    readonly Dictionary<int,float> opacity=new(),shake=new();
    float screenAlpha=1;
    int fadeColor;
    static readonly Regex tags=new(@"<img\b[^>]*>",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    static readonly Regex attributes=new("([a-z]+)\\s*=\\s*(['\"])(.*?)\\2",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    public void ClearFromLine(long line) => visible.RemoveAll(s=>s.Line>line);
    public string ProcessHtml(string html,long line)
    {
        if(string.IsNullOrEmpty(html)||definitions.IsEmpty)return html;
        return tags.Replace(html,m=>
        {
            var a=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            foreach(Match item in attributes.Matches(m.Value))a[item.Groups[1].Value]=System.Net.WebUtility.HtmlDecode(item.Groups[3].Value);
            if(!a.TryGetValue("src",out var name)||!definitions.TryGetValue(name,out var sprite))return m.Value;
            int Int(string key,int fallback)=>a.TryGetValue(key,out var s)?int.Parse(s,CultureInfo.InvariantCulture):fallback;
            int group=Int("group",sprite.Group);
            if(name.Equals("CLEARALL",StringComparison.OrdinalIgnoreCase)){visible.RemoveAll(x=>group<0||x.Group==group);return "";}
            // Repeating an existing resource toggles that layer off in this dialect.
            if(visible.RemoveAll(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase))>0)return "";
            sprite=sprite with {Line=sprite.Root?line:-1,X=Int("fxpos",sprite.X),Y=Int("fypos",sprite.Y),Group=group,Alpha=a.TryGetValue("alpha",out var alpha)?Math.Clamp(Number(alpha),0,1):sprite.Alpha,Reverse=a.TryGetValue("reverse",out var rev)&&bool.Parse(rev)};
            if(a.TryGetValue("back",out var back)){
                var parts=back.Split(',');if(parts.Length<2)throw new CodeEE("Invalid fixed image back groups");
                sprite=sprite with {Alpha=Math.Clamp(Number(parts[0]),0,1),Back=parts.Skip(1).Select(int.Parse).ToArray()};
            }
            if(!sprite.Root&&group>=0){var parent=visible.LastOrDefault(x=>x.Root&&x.Group==group);if(parent==null)return "";sprite=sprite with {X=parent.X,Y=parent.Y,Line=parent.Line};}
            visible.Add(sprite);return "";
        });
    }
    public void Draw(Graphics graph)
    {
        if(!DisplayOptions.ShowImages||visible.Count==0)return;
        var canvas=graph.Canvas;
        var groups=visible.Where(s=>s.Root&&s.Group>=0).ToList();
        var backParent=groups.LastOrDefault(s=>s.Back!=null);
        void One(Sprite s){var image=AppContents.GetSprite(s.Name);if(image?.IsCreated==true)image.GraphicsDraw(graph,new Rectangle(s.X,s.Y,image.DestBaseSize.Width,image.DestBaseSize.Height));}
        void Group(Sprite root,bool includeBack){
            using var paint=new SKPaint{Color=SKColors.White.WithAlpha((byte)Math.Clamp((int)Math.Round(255*root.Alpha*opacity.GetValueOrDefault(root.Group,1)),0,255))};
            canvas.SaveLayer(paint);canvas.Translate(shake.GetValueOrDefault(root.Group),0);
            if(root.Reverse){canvas.Translate(graph.ClipBounds.Width,0);canvas.Scale(-1,1);}
            foreach(var s in visible.Where(s=>s.Group==root.Group))One(s);
            if(includeBack)foreach(var g in groups.Where(g=>g.Group!=root.Group&&root.Back.Contains(g.Group)))Group(g,false);
            canvas.Restore();
        }
        foreach(var s in visible.Where(s=>s.Group<0)){
            using var paint=new SKPaint{Color=SKColors.White.WithAlpha((byte)(255*s.Alpha))};canvas.SaveLayer(paint);One(s);canvas.Restore();
        }
        foreach(var g in groups){if(backParent?.Back.Contains(g.Group)==true&&g!=backParent)continue;Group(g,g==backParent);}
    }
    public void DrawOverlay(Graphics graph,Color background)
    {
        if(screenAlpha>=1)return;
        var color=fadeColor switch{1=>Color.Red,2=>Color.Black,3=>Color.White,_=>background};
        using var b=new SolidBrush(Color.FromArgb((int)Math.Round(255*(1-screenAlpha)),color));
        graph.FillRectangle(b,graph.ClipBounds);
    }
    static int[] Groups(string text)=>text.Split(',').Select(x=>int.Parse(x.Trim(),CultureInfo.InvariantCulture)).Distinct().ToArray();
    public void Fade(float alpha,string groups,long duration,long color,Action redraw)
    {
        alpha=Math.Clamp(alpha,0,1);fadeColor=(int)color;var ids=Groups(groups);
        var from=ids.ToDictionary(i=>i,i=>i==-1?screenAlpha:opacity.GetValueOrDefault(i,1));
        Animate(duration,t=>{foreach(var id in ids){float v=from[id]+(alpha-from[id])*t;if(id==-1)screenAlpha=v;else opacity[id]=v;}},redraw);
    }
    public void Shake(float coefficient,long amplitude,string groups,long duration,Action redraw)
    {
        var ids=Groups(groups);
        Animate(Math.Max(0,duration),t=>{int tick=(int)(Math.Max(0,duration)*t/33);float offset=t>=1?0:(float)(coefficient*amplitude*Math.Pow(-.8,tick));foreach(var id in ids)shake[id]=offset;},redraw);
    }
    static void Animate(long duration,Action<float> apply,Action redraw)
    {
        // The original instruction blocks script execution for the effect duration.
        if(duration>60000)throw new CodeEE("Visual effect duration exceeds 60000 ms");
        if(duration<=0){apply(1);redraw();return;}
        var timer=Stopwatch.StartNew();
        while(timer.ElapsedMilliseconds<duration){apply((float)timer.ElapsedMilliseconds/duration);redraw();Thread.Sleep((int)Math.Min(33,duration-timer.ElapsedMilliseconds>0?duration-timer.ElapsedMilliseconds:1));}
        apply(1);redraw();
    }
}
internal sealed partial class EmueraConsole
{
    readonly PuchiGraphics puchi=new();
    void PuchiRedraw(){RefreshStrings(true);window.Host.TryPushFrameNow();}
    public void PuchiFade(float alpha,string groups,long duration,long color)=>puchi.Fade(alpha,groups,duration,color,PuchiRedraw);
    public void PuchiShake(float coefficient,long amplitude,string groups,long duration)=>puchi.Shake(coefficient,amplitude,groups,duration,PuchiRedraw);
}
