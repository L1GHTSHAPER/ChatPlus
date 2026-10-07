using UnityEngine;
using UnityEngine.UI;

namespace ChatPlus
{
    // A material tint changes only this image, including its stencil material. The game's fade still multiplies
    // its vertex alpha; text, buttons and children keep their own opacity.
    internal sealed class BackgroundOpacity : MonoBehaviour, IMaterialModifier
    {
        Image _image;
        Material _source;
        Material _tinted;

        internal static void Attach(Image image)
        {
            if (image == null) return;
            var opacity = image.GetComponent<BackgroundOpacity>() ?? image.gameObject.AddComponent<BackgroundOpacity>();
            opacity._image = image;
            opacity.Refresh();
        }

        internal void Refresh() => _image?.SetMaterialDirty();

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null || plugin.BackgroundOpacity.Value == 100 || baseMaterial == null || !baseMaterial.HasProperty("_Color"))
                return baseMaterial;
            if (_tinted == null || _source != baseMaterial)
            {
                if (_tinted != null) Destroy(_tinted);
                _source = baseMaterial;
                _tinted = new Material(baseMaterial) { name = "ChatPlus.BackgroundOpacity", hideFlags = HideFlags.HideAndDontSave };
            }
            Color tint = baseMaterial.GetColor("_Color");
            tint.a *= Mathf.Clamp01(plugin.BackgroundOpacity.Value / 100f);
            _tinted.SetColor("_Color", tint);
            return _tinted;
        }

        void OnDestroy()
        {
            if (_tinted != null) Destroy(_tinted);
        }
    }
}
