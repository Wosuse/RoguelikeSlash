Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
public static class ForwardPack{
public static void Export(string src,string dst){
using(var sheet=new Bitmap(src)){
var sprites=new Bitmap[5];var boxes=new Rectangle[5];int mh=1,mw=1;
for(int i=0;i<5;i++){
int left=i*sheet.Width/5,right=(i+1)*sheet.Width/5;
var b=sheet.Clone(new Rectangle(left,0,right-left,sheet.Height),PixelFormat.Format32bppArgb);
int l=b.Width,t=b.Height,r=-1,bt=-1;
for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){
var p=b.GetPixel(x,y);int a=Math.Max(0,Math.Min(255,(p.A-180)*255/65));
b.SetPixel(x,y,a==0?Color.Transparent:Color.FromArgb(a,p.R,p.G,p.B));
if(a>32){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);bt=Math.Max(bt,y);}
}
boxes[i]=Rectangle.FromLTRB(l,t,r+1,bt+1);sprites[i]=b;mh=Math.Max(mh,bt-t+1);mw=Math.Max(mw,r-l+1);
}
double scale=Math.Min(365.0/mh,448.0/mw);
for(int i=0;i<5;i++){
var box=boxes[i];
using(var f=new Bitmap(512,512,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(f)){
g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
int w=(int)Math.Round(box.Width*scale),h=(int)Math.Round(box.Height*scale);
g.DrawImage(sprites[i],new Rectangle((512-w)/2,472-h,w,h),box,GraphicsUnit.Pixel);
f.Save(Path.Combine(dst,"forward-dodge-"+(i+1).ToString("00")+".png"),ImageFormat.Png);
}sprites[i].Dispose();
}
}
}
}
"@ -ReferencedAssemblies System.Drawing
[ForwardPack]::Export('C:\Users\82347\.codex\generated_images\01a1da2d-cd91-75f1-b56d-cf69b56febaf\exec-ef370b4d-e29e-44df-9fde-2eb2c799fdcb.png',"$PSScriptRoot\frames")
