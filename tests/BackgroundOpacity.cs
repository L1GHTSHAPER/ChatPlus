using System;
using UnityEngine;
using UnityEngine.UI;

namespace ChatPlus.Tests
{
    internal static class BackgroundOpacityTests
    {
        static VertexHelper Mesh(byte alpha)
        {
            var mesh = new VertexHelper();
            mesh.Vertices.Add(new UIVertex { color = new Color32(30, 70, 120, alpha) });
            return mesh;
        }

        internal static void Run(Action<bool, string> check)
        {
            var obj = new GameObject();
            var image = obj.AddComponent<Image>();
            BackgroundOpacity.Attach(image);
            var effect = obj.GetComponent<BackgroundOpacity>();
            check(effect != null && image.DirtyCalls == 1, "attaching opacity dirties native image vertices without requiring a shader property");
            BackgroundOpacity.Attach(image);
            check(effect == obj.GetComponent<BackgroundOpacity>() && image.DirtyCalls == 2, "repeated opacity attachment reuses the same effect");

            Plugin.Instance.BackgroundOpacity.Value = 0;
            var mesh = Mesh(255);
            effect.ModifyMesh(mesh);
            check(mesh.Vertices[0].color.a == 0, "zero opacity fully clears decorative image alpha");
            check(mesh.Vertices[0].color.r == 30 && mesh.Vertices[0].color.g == 70 && mesh.Vertices[0].color.b == 120, "opacity preserves the native image RGB tint");
            Plugin.Instance.BackgroundOpacity.Value = 50;
            mesh = Mesh(128);
            effect.ModifyMesh(mesh);
            check(mesh.Vertices[0].color.a == 64, "opacity multiplies the game's current fade instead of overwriting it");
            mesh = Mesh(0);
            effect.ModifyMesh(mesh);
            check(mesh.Vertices[0].color.a == 0, "a natively hidden image stays hidden at partial opacity");
            Plugin.Instance.BackgroundOpacity.Value = 100;
            mesh = Mesh(128);
            effect.ModifyMesh(mesh);
            check(mesh.Vertices[0].color.a == 128, "returning to full opacity restores the original fade on a fresh mesh");
            effect.Active = false;
            Plugin.Instance.BackgroundOpacity.Value = 0;
            mesh = Mesh(255);
            effect.ModifyMesh(mesh);
            check(mesh.Vertices[0].color.a == 255, "disabled opacity leaves the image mesh alone");
            var maskObject = new GameObject();
            var maskImage = maskObject.AddComponent<Image>();
            maskObject.AddComponent<Mask>();
            BackgroundOpacity.Attach(maskImage);
            check(maskObject.GetComponent<BackgroundOpacity>() == null && maskImage.DirtyCalls == 0, "stencil masks are excluded so transparent chat does not erase child text");
            Plugin.Instance.BackgroundOpacity.Value = 100;
        }
    }
}
