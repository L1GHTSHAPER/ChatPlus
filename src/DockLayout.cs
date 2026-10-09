using System;
using System.Collections.Generic;

namespace ChatPlus
{
    // Unity-free layout, also used by the regression suite.
    internal static class DockLayout
    {
        internal readonly struct Box
        {
            internal readonly float X,Y,Width,Height;
            internal Box(float x,float y,float width,float height) { X=x;Y=y;Width=width;Height=height; }
            internal bool Overlaps(Box b) => X < b.X+b.Width && X+Width > b.X && Y < b.Y+b.Height && Y+Height > b.Y;
        }
        internal static bool Find(float width,float height,float scale,int count,IReadOnlyList<Box> obstacles,out Box result)
        {
            return Find(width,height,36*scale,36*scale,6*scale,12*scale,count,obstacles,out result);
        }
        // Preserve the native left edge and row spacing, including the part beyond the monitor.
        internal static bool FindRight(float width,float height,float x,float buttonWidth,float buttonHeight,float gap,
            float preferredTop,float margin,int count,IReadOnlyList<Box> obstacles,out Box result)
        {
            result=default;
            float pitch=buttonHeight+gap, total=count*pitch-gap;
            if(count<=0 || buttonWidth<=0 || buttonHeight<=0 || gap<0 || x<0 || x>=width ||
                x+buttonWidth<width || total+2*margin>height) return false;
            int first=(int)Math.Ceiling((margin-preferredTop)/pitch);
            int last=(int)Math.Floor((height-margin-total-preferredTop)/pitch);
            var rows=new List<int>();
            for(int row=Math.Max(0,first);row<=last;row++) rows.Add(row);
            for(int row=Math.Min(-1,last);row>=first;row--) rows.Add(row);
            foreach(int offset in rows)
            {
                // Start directly below the game buttons when possible; otherwise use a free earlier row.
                float y=preferredTop+offset*pitch;
                var visible=new Box(x,y,width-x,total);
                bool blocked=false;
                foreach(var obstacle in obstacles) if(obstacle.Overlaps(visible)) { blocked=true;break; }
                if(blocked) continue;
                result=new Box(x,y,buttonWidth,total);return true;
            }
            return false;
        }
        internal static bool Find(float width,float height,float buttonWidth,float buttonHeight,float gap,float margin,int count,IReadOnlyList<Box> obstacles,out Box result)
        {
            float total=count*(buttonHeight+gap)-gap;
            result=default;
            if(count<=0 || width<buttonWidth+2*margin || height<total+24 || buttonWidth<=0 || buttonHeight<=0) return false;
            foreach(bool right in new[]{true,false})
            {
                float x=right?width-margin-buttonWidth:margin;
                float preferred=Math.Max(12,Math.Min((height-total)*.5f,height-total-12));
                for(int step=0;step<height/(buttonHeight+gap)+2;step++)
                {
                    foreach(int direction in new[]{1,-1})
                    {
                        float y=preferred+step*(buttonHeight+gap)*direction;
                        if(y<12 || y+total>height-12) continue;
                        var candidate=new Box(x,y,buttonWidth,total);
                        bool blocked=false;
                        foreach(var obstacle in obstacles) if(obstacle.Overlaps(candidate)) { blocked=true;break; }
                        if(blocked) continue;
                        result=candidate;return true;
                    }
                }
            }
            return false;
        }
    }

    internal static class SidebarVisibility
    {
        internal static bool Show(bool desktop,bool nativeActive,bool canvasEnabled,float alpha) =>
            !desktop && nativeActive && canvasEnabled && alpha > .01f;
    }
}
