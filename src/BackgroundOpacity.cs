using UnityEngine;
using UnityEngine.UI;

namespace ChatPlus
{
    // Multiply only the image's vertex alpha, independently of its shader. Preserve native fades and text.
    internal sealed class BackgroundOpacity : BaseMeshEffect
    {
        Image _image;

        internal static void Attach(Image image)
        {
            // Stencil masks must keep writing their stencil, even when decorative backgrounds are transparent.
            if (image == null || image.GetComponent<Mask>() != null) return;
            var opacity = image.GetComponent<BackgroundOpacity>() ?? image.gameObject.AddComponent<BackgroundOpacity>();
            opacity._image = image;
            opacity.Refresh();
        }

        internal void Refresh() => _image?.SetVerticesDirty();

        public override void ModifyMesh(VertexHelper mesh)
        {
            Plugin plugin = Plugin.Instance;
            if (!IsActive() || plugin == null || plugin.BackgroundOpacity.Value == 100) return;
            float opacity = Mathf.Clamp01(plugin.BackgroundOpacity.Value / 100f);
            var vertex = new UIVertex();
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                Color32 color = vertex.color;
                color.a = (byte)Mathf.RoundToInt(color.a * opacity);
                vertex.color = color;
                mesh.SetUIVertex(vertex, i);
            }
        }
    }
}
