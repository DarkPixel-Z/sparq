using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sparq.UI
{
    /// <summary>
    /// Diagnostic: logs every left-click anywhere in the scene, walking the
    /// EventSystem raycast results to show exactly which GameObject(s) got hit.
    /// </summary>
    public class GlobalClickSniffer : MonoBehaviour
    {
        private static GlobalClickSniffer _instance;

        public static void Ensure()
        {
            if (_instance != null) return;
            var go = new GameObject("GlobalClickSniffer");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<GlobalClickSniffer>();
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            var es = EventSystem.current;
            if (es == null) { Debug.Log("[Sniffer] click but NO EventSystem"); return; }
            var pe = new PointerEventData(es) { position = Input.mousePosition };
            var hits = new List<RaycastResult>();
            es.RaycastAll(pe, hits);
            if (hits.Count == 0)
            {
                Debug.Log($"[Sniffer] click at {Input.mousePosition} — 0 hits (NOTHING under cursor)");
                return;
            }
            var sb = new System.Text.StringBuilder();
            sb.Append($"[Sniffer] click at {Input.mousePosition} → {hits.Count} hit(s):\n");
            for (int i = 0; i < hits.Count && i < 6; i++)
            {
                var h = hits[i];
                sb.Append($"  [{i}] {h.gameObject.name}");
                var canvas = h.gameObject.GetComponentInParent<Canvas>();
                if (canvas != null) sb.Append($"  (canvas:{canvas.name} sort:{canvas.sortingOrder})");
                var btn = h.gameObject.GetComponent<UnityEngine.UI.Button>()
                       ?? h.gameObject.GetComponentInParent<UnityEngine.UI.Button>();
                if (btn != null) sb.Append($"  ⤴ Button:{btn.gameObject.name} interactable:{btn.interactable}");
                sb.Append('\n');
            }
            Debug.Log(sb.ToString());
        }
    }
}
