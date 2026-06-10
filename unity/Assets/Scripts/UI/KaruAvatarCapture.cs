using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Sparq.UI
{
    /// <summary>
    /// At runtime, takes a snapshot of the live Karu (Bear) in the scene
    /// using a temporary camera + RenderTexture. Applies the result to the
    /// PlayerHUD avatar Image so it matches exactly what the player sees.
    /// </summary>
    public class KaruAvatarCapture : MonoBehaviour
    {
        public Image targetAvatar;

        private void Start()
        {
            StartCoroutine(CaptureWhenReady());
        }

        private IEnumerator CaptureWhenReady()
        {
            // Wait a few frames so Karu has fully loaded + breathing started
            yield return null;
            yield return null;
            yield return null;
            yield return new WaitForSeconds(0.2f);

            var karu = GameObject.Find("Karu");
            if (karu == null) yield break;

            // Compute bounds of all SpriteRenderers in Karu
            var renderers = karu.GetComponentsInChildren<SpriteRenderer>();
            if (renderers == null || renderers.Length == 0) yield break;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null) bounds.Encapsulate(renderers[i].bounds);
            }
            // Pad slightly
            bounds.Expand(0.3f);

            // Create temp camera
            var camGO = new GameObject("[KaruSnapCam]");
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = bounds.size.y * 0.5f;
            cam.aspect = bounds.size.x / bounds.size.y;
            cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.cullingMask = -1;

            int W = 256, H = 256;
            var rt = RenderTexture.GetTemporary(W, H, 16, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            yield return null; // ensure renderers tick
            cam.Render();

            // Read pixels
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            // Cleanup
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            Destroy(camGO);

            // Apply to avatar
            if (targetAvatar != null)
            {
                targetAvatar.sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
                targetAvatar.color = Color.white;
                targetAvatar.preserveAspect = true;
            }
        }
    }
}
