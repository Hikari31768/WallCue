using System;
using UnityEngine;
namespace WallCue
{
    internal static class RenderDiagnostics
    {
        internal static void Log(ParametricBoxFrameController frame, MaterialPropertyBlockController frameBlock,
            ParametricBoxFakeGlowController glow, MaterialPropertyBlockController glowBlock)
        {
            try
            {
                Plugin.Log.Info("Frame diagnostics: active=" + frame.gameObject.activeInHierarchy + ", enabled=" + frame.enabled +
                    ", baseColor=" + frame.color + ", edgeSize=" + frame.edgeSize);
                Describe("frame", frameBlock);
                if (glow != null)
                {
                    Plugin.Log.Info("Glow diagnostics: active=" + glow.gameObject.activeInHierarchy + ", enabled=" + glow.enabled + ", baseColor=" + glow.color);
                    Describe("glow", glowBlock);
                }
            }
            catch (Exception e) { Plugin.Log.Warn("Render diagnostics unavailable: " + e.Message); }
        }
        private static void Describe(string label, MaterialPropertyBlockController block)
        {
            if (block == null) return;
            var renderers = block.renderers;
            Plugin.Log.Info(label + " renderer count=" + (renderers == null ? 0 : renderers.Length));
            if (renderers == null) return;
            int id = Shader.PropertyToID("_Color");
            var observed = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                var material = renderer.sharedMaterial;
                renderer.GetPropertyBlock(observed);
                Plugin.Log.Info(label + " renderer=" + renderer.name + ", enabled=" + renderer.enabled +
                    ", layer=" + renderer.gameObject.layer + ", active=" + renderer.gameObject.activeInHierarchy +
                    ", shader=" + (material != null && material.shader != null ? material.shader.name : "null") +
                    ", hasColor=" + (material != null && material.HasProperty(id)) + ", submittedColor=" + observed.GetColor(id));
            }
        }
    }
}
