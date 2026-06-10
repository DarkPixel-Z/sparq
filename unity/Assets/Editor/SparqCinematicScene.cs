using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Upgrades the Home scene into a cinematic "world":
    ///   • Gradient sky background (screen-space canvas, below gameplay UI)
    ///   • God rays from the top
    ///   • Floating ambient particles
    ///   • Vignette overlay on top
    ///   • Idle breathing on Karu + Volt portrait
    ///   • Gentle camera breathing
    ///
    /// Safe to run multiple times — rebuilds the cinematic objects fresh.
    /// </summary>
    public static class SparqCinematicScene
    {
        private const string SUNRAYS_PATH = "Assets/Free Asset - 2D Handcrafted Art/Sprite/SunRays.psd";
        private const string DUST_PATH    = "Assets/Free Asset - 2D Handcrafted Art/Sprite/Dust1.psd";
        private const string FOG_PATH     = "Assets/Free Asset - 2D Handcrafted Art/Sprite/Fog.psd";

        [MenuItem("Sparq/15. Cinematic Scene (parallax + FX)")]
        public static void BuildCinematic()
        {
            // Remove previous cinematic group if present
            var old = GameObject.Find("[Cinematic]");
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject("[Cinematic]");

            BuildSkyCanvas(root);
            BuildGodRays(root);
            BuildAmbientParticles(root);
            BuildVignette(root);

            ApplyBreathing();
            ApplyCameraBreathing();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Sparq Cinematic] Cinematic scene built.");
            EditorUtility.DisplayDialog("Sparq Cinematic",
                "✅ Cinematic scene applied!\n\n" +
                "• Gradient sky behind everything\n" +
                "• God rays from the top\n" +
                "• Floating dust/magic particles\n" +
                "• Vignette darkens the edges\n" +
                "• Karu + Hellhound breathe on idle\n" +
                "• Camera slowly drifts + zooms\n\n" +
                "Hit ▶ Play to feel it.", "OK");
        }

        // ---------- 1. Sky gradient background ----------
        private static void BuildSkyCanvas(GameObject root)
        {
            var skyGO = new GameObject("SkyCanvas");
            skyGO.transform.SetParent(root.transform, false);

            var canvas = skyGO.AddComponent<Canvas>();
            // Screen Space Camera at far plane so world-space Karu/Una render in front
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 90f; // far away — world sprites at z=0 are in front
            canvas.sortingOrder = -100;

            skyGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            skyGO.AddComponent<GraphicRaycaster>().enabled = false;

            // Full-screen gradient image built from a tiny procedural texture
            var bgGO = new GameObject("SkyGradient");
            bgGO.transform.SetParent(skyGO.transform, false);
            var rt = bgGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var img = bgGO.AddComponent<RawImage>();
            img.texture = MakeVerticalGradient(
                top:    new Color(0.32f, 0.12f, 0.45f, 1f),   // deep violet
                middle: new Color(0.18f, 0.08f, 0.35f, 1f),   // purple mid
                bottom: new Color(0.06f, 0.04f, 0.18f, 1f));  // near-black base
            img.raycastTarget = false;

            // Horizon band (magenta glow)
            var horizonGO = new GameObject("Horizon");
            horizonGO.transform.SetParent(skyGO.transform, false);
            var hrt = horizonGO.AddComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 0.35f);
            hrt.anchorMax = new Vector2(1, 0.55f);
            hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
            var hImg = horizonGO.AddComponent<RawImage>();
            hImg.texture = MakeVerticalGradient(
                top:    new Color(1f, 0.35f, 0.75f, 0f),
                middle: new Color(1f, 0.35f, 0.75f, 0.25f),
                bottom: new Color(1f, 0.35f, 0.75f, 0f));
            hImg.raycastTarget = false;
        }

        // ---------- 2. God rays ----------
        private static void BuildGodRays(GameObject root)
        {
            var rayCanvasGO = new GameObject("RaysCanvas");
            rayCanvasGO.transform.SetParent(root.transform, false);
            var canvas = rayCanvasGO.AddComponent<Canvas>();
            // Also Screen Space Camera, nearer than sky but still behind Karu
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 50f;
            canvas.sortingOrder = -50;
            rayCanvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            rayCanvasGO.AddComponent<GraphicRaycaster>().enabled = false;

            var rayGO = new GameObject("GodRays");
            rayGO.transform.SetParent(rayCanvasGO.transform, false);
            var rt = rayGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.4f);
            rt.anchorMax = new Vector2(0.9f, 1f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var img = rayGO.AddComponent<Image>();
            var raySprite = AssetDatabase.LoadAssetAtPath<Sprite>(SUNRAYS_PATH);
            if (raySprite != null)
            {
                img.sprite = raySprite;
                img.color = new Color(1f, 0.85f, 0.6f, 0.12f); // very subtle
            }
            else
            {
                img.color = new Color(1f, 0.85f, 0.6f, 0.05f);
            }
            img.raycastTarget = false;
            img.preserveAspect = true;
        }

        // ---------- 3. Ambient floating particles (world-space) ----------
        private static void BuildAmbientParticles(GameObject root)
        {
            var pGO = new GameObject("AmbientParticles");
            pGO.transform.SetParent(root.transform, false);
            pGO.transform.position = new Vector3(0, 0, 5);

            var ps = pGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 10f;
            main.loop = true;
            main.startLifetime = 8f;
            main.startSpeed = 0.3f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startColor = new Color(1f, 0.95f, 0.7f, 0.7f);
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.02f; // drift up

            var emission = ps.emission;
            emission.rateOverTime = 6;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10f, 8f, 0.1f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.95f, 0.7f), 0f),
                    new GradientColorKey(new Color(0.8f, 0.6f, 1f), 1f) },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.8f, 0.2f),
                    new GradientAlphaKey(0.6f, 0.7f),
                    new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = grad;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.4f;

            // Renderer: use default-particle material
            var renderer = pGO.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            renderer.material = mat;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 10;
        }

        // ---------- 4. Vignette ----------
        private static void BuildVignette(GameObject root)
        {
            var vigCanvasGO = new GameObject("VignetteCanvas");
            vigCanvasGO.transform.SetParent(root.transform, false);
            var canvas = vigCanvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // above gameplay UI
            vigCanvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            vigCanvasGO.AddComponent<GraphicRaycaster>().enabled = false;

            var vigGO = new GameObject("Vignette");
            vigGO.transform.SetParent(vigCanvasGO.transform, false);
            var rt = vigGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var img = vigGO.AddComponent<RawImage>();
            img.texture = MakeRadialVignette(512, 0.65f);
            img.color = new Color(0f, 0f, 0f, 0.55f);
            img.raycastTarget = false;
        }

        // ---------- 5. Breathing on Karu + Hellhound ----------
        private static void ApplyBreathing()
        {
            var karu = GameObject.Find("Karu");
            if (karu != null && karu.GetComponent<Sparq.Cinematic.IdleBreathing>() == null)
                karu.AddComponent<Sparq.Cinematic.IdleBreathing>();

            var una = GameObject.Find("Una");
            if (una != null && una.GetComponent<Sparq.Cinematic.IdleBreathing>() == null)
            {
                var b = una.AddComponent<Sparq.Cinematic.IdleBreathing>();
                // offset phase so Una and Karu don't breathe in sync (looks weird)
                var field = typeof(Sparq.Cinematic.IdleBreathing).GetField("phaseOffset",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(b, 0.7f);
            }

            var volt = GameObject.Find("VoltPortrait");
            if (volt != null && volt.GetComponent<Sparq.Cinematic.IdleBreathing>() == null)
            {
                var b = volt.AddComponent<Sparq.Cinematic.IdleBreathing>();
                var field = typeof(Sparq.Cinematic.IdleBreathing).GetField("phaseOffset",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(b, 1.4f);
                // Volt should breathe slightly harder — menacing
                var ampField = typeof(Sparq.Cinematic.IdleBreathing).GetField("amplitude",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                ampField?.SetValue(b, 0.035f);
            }
        }

        // ---------- 6. Camera breathing ----------
        private static void ApplyCameraBreathing()
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (cam.GetComponent<Sparq.Cinematic.CameraBreathing>() == null)
                cam.gameObject.AddComponent<Sparq.Cinematic.CameraBreathing>();
            // Keep a nice dark-purple clear color in case the sky canvas ever fails
            cam.backgroundColor = new Color(0.06f, 0.04f, 0.18f, 1f);
        }

        // ---------- Texture helpers ----------
        private static Texture2D MakeVerticalGradient(Color top, Color middle, Color bottom)
        {
            const int h = 256;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color c = (t < 0.5f)
                    ? Color.Lerp(bottom, middle, t * 2f)
                    : Color.Lerp(middle, top, (t - 0.5f) * 2f);
                tex.SetPixel(0, y, c);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D MakeRadialVignette(int size, float softEdge)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxR; // 0 center..1 edge
                float a = Mathf.Clamp01((d - softEdge) / (1f - softEdge));
                a = Mathf.Pow(a, 2.2f);
                tex.SetPixel(x, y, new Color(0, 0, 0, a));
            }
            tex.Apply();
            return tex;
        }
    }
}
