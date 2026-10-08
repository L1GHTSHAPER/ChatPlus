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
