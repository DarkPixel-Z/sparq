using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sparq.UI
{
    /// <summary>
    /// Bulletproof click handler for the home lobby's left-rail hex icons that
    /// don't participate in Unity UI raycasting (a GlobalClickSniffer confirmed
    /// "0 hits" at their screen positions, even with overlay canvases on top).
    ///
    /// Instead of fighting the prefab's dead raycast subtree, this component
    /// polls Input each frame and, if a tap lands inside a registered hotspot's
    /// normalised screen region, fires that hotspot's action. Currently handles:
    ///   • FRIENDS ghost hex (~rel 0.10, 0.70) → WorldPanel
    ///   • MAP / explore hex  (~rel 0.10, 0.62) → WorldExplorePanel (idle map)
    ///
    /// To avoid hijacking taps when a popup is open, it checks (via RaycastAll —
    /// which DOES work for the panels) whether a high-sort panel canvas is under
    /// the cursor; only then does it stand down.
    /// </summary>
    public class FriendsHexHotspot : MonoBehaviour
    {
        private struct Hotspot
        {
            public Rect region;          // normalised screen rect (0,0 = bottom-left)
            public string label;
            public System.Action action;
        }

        // A canvas at/above this sort is treated as an open popup panel.
        private const int PANEL_SORT_THRESHOLD = 9000;

        private static readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private readonly List<Hotspot> _spots = new List<Hotspot>();

        private void Awake()
        {
            // FRIENDS ghost hex — opens the social/World panel (Friends tab).
            _spots.Add(new Hotspot
            {
                region = new Rect(0.02f, 0.655f, 0.16f, 0.11f),
                label = "FRIENDS",
                action = () => { try { Sparq.UI.WorldPanel.Show(); } catch (System.Exception ex) { Debug.LogError(ex.Message); } }
            });
            // MAP hex — opens the chapter stage map (Forest of Trials with the
            // shield-style stage nodes). Used to go straight to WorldExplorePanel
            // free-explore which skipped the stage progression and confused
            // testers (showed up as "wrong map" feedback). Now routes through
            // StageMapPanel like the PLAY button does — explore mode opens
            // after picking a specific stage.
            _spots.Add(new Hotspot
            {
                region = new Rect(0.02f, 0.545f, 0.16f, 0.105f),
                label = "MAP",
                action = () => { try { Sparq.UI.StageMapPanel.Show(); } catch (System.Exception ex) { Debug.LogError(ex.Message); } }
            });
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            float w = Screen.width, h = Screen.height;
            if (w <= 0f || h <= 0f) return;
            Vector2 rel = new Vector2(Input.mousePosition.x / w, Input.mousePosition.y / h);

            // Which hotspot (if any) did the tap land in?
            int idx = -1;
            for (int i = 0; i < _spots.Count; i++)
                if (_spots[i].region.Contains(rel)) { idx = i; break; }
            if (idx < 0) return;

            // Stand down ONLY if a popup panel is genuinely under the cursor.
            var es = EventSystem.current;
            if (es != null)
            {
                _hits.Clear();
                var pe = new PointerEventData(es) { position = Input.mousePosition };
                es.RaycastAll(pe, _hits);
                foreach (var hit in _hits)
                {
                    var canvas = hit.gameObject.GetComponentInParent<Canvas>();
                    if (canvas != null && canvas.sortingOrder >= PANEL_SORT_THRESHOLD)
                    {
                        Debug.Log($"[FriendsHexHotspot] Tap in '{_spots[idx].label}' region but panel '{canvas.name}' (sort {canvas.sortingOrder}) on top — standing down.");
                        return;
                    }
                }
            }

            Debug.Log($"[FriendsHexHotspot] '{_spots[idx].label}' hotspot tapped (rel {rel}).");
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            _spots[idx].action?.Invoke();
        }
    }
}
