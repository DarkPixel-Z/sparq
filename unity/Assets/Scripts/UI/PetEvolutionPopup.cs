// PetEvolutionPopup.cs — "Your pet evolved!" celebration.
//
// Fires from the lobby when PetService.TryConsumeEvolutionAdvance() reports a
// new stage. Shows the pet's sprite (bigger), the new stage name, and the stat
// bump, so the long-term care payoff is felt as a moment, not a stat change.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class PetEvolutionPopup
    {
        private static readonly Color CARD_BG  = new Color(0.17f, 0.17f, 0.21f, 1f);
        private static readonly Color CREAM    = new Color(1f, 0.97f, 0.85f, 1f);
        private static readonly Color INK_SOFT = new Color(0.80f, 0.82f, 0.90f, 1f);
        private static readonly Color GOLD     = new Color(0.99f, 0.80f, 0.30f, 1f);
        private static readonly Color BTN_GREEN= new Color(0.32f, 0.74f, 0.42f, 1f);

        private static GameObject _root;

        public static void Show(int newStage)
        {
            if (_root != null) return;
            EnsureEventSystem();

            string stageName = Sparq.Systems.PetService.EvolutionStageName(newStage);
            int statBumpPct  = Mathf.RoundToInt(
                (Sparq.Systems.PetService.EvolutionStatMult(newStage) - 1f) * 100f);

            // Active pet's sprite + nickname (best-effort — fall back gracefully)
            string petName = "Your pet";
            Sprite petSp = null;
            try
            {
                var pet = Sparq.Systems.PetService.Active();
                if (pet != null)
                {
                    petName = string.IsNullOrEmpty(pet.nickname) ? "Your pet" : pet.nickname;
                    var sp = Sparq.Systems.PetService.FindSpecies(pet.speciesId);
                    if (sp != null && !string.IsNullOrEmpty(sp.spritePath))
                        petSp = Sparq.UI.HeroPortrait.LoadCropped(sp.spritePath);
                }
            }
            catch {}

            _root = new GameObject("Sparq_PetEvolutionPopup",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Stretch(_root.GetComponent<RectTransform>());
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 16400;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort) maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 20;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            var dim = NewGO("Dim", _root.transform, typeof(Image));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.07f, 0.92f);

            // Card
            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(820, 1020);
            card.GetComponent<Image>().color = CARD_BG;

            // Gold accent rule top
            var accent = NewGO("Accent", card.transform, typeof(Image));
            var aRT = accent.GetComponent<RectTransform>();
            aRT.anchorMin = new Vector2(0, 1); aRT.anchorMax = new Vector2(1, 1);
            aRT.pivot = new Vector2(0.5f, 1); aRT.anchoredPosition = Vector2.zero; aRT.sizeDelta = new Vector2(0, 6);
            accent.GetComponent<Image>().color = GOLD;
            accent.GetComponent<Image>().raycastTarget = false;

            // Title
            var title = MakeText(card.transform, "Title", "PET EVOLVED!", 54, FontStyles.Bold, GOLD);
            try { title.outlineWidth = 0.25f; title.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var tRT = title.rectTransform;
            tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.offsetMin = new Vector2(20, -110); tRT.offsetMax = new Vector2(-20, -34);
            title.alignment = TextAlignmentOptions.Center;

            // Pet sprite (big, centred). Named petGO to avoid colliding with
            // the `pet` local in the try block above (CS0136).
            var petGO = NewGO("Pet", card.transform, typeof(Image));
            var pRT = petGO.GetComponent<RectTransform>();
            pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
            pRT.pivot = new Vector2(0.5f, 0.5f);
            pRT.anchoredPosition = new Vector2(0, 100);
            pRT.sizeDelta = new Vector2(360, 360);
            var pImg = petGO.GetComponent<Image>();
            if (petSp != null) { pImg.sprite = petSp; pImg.preserveAspect = true; }
            else pImg.color = new Color(0.5f, 0.7f, 1f);   // pleasant placeholder
            pImg.raycastTarget = false;

            // Subtitle: name + new stage
            var sub = MakeText(card.transform, "Sub",
                $"<color=#FFD466>{petName}</color> is now a <color=#FFD466>{stageName}</color>!",
                36, FontStyles.Bold, CREAM);
            sub.richText = true;
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0.5f, 1);
            sRT.offsetMin = new Vector2(40, -700); sRT.offsetMax = new Vector2(-40, -620);
            sub.alignment = TextAlignmentOptions.Center;

            // Body line — stat bump (only if Adult/Elder; Juvenile is +0%)
            string bodyText = statBumpPct > 0
                ? $"Battle stats +{statBumpPct}% from here on. Keep up the care!"
                : "Your pet just grew up — bigger, stronger, and ready for more.";
            var body = MakeText(card.transform, "Body", bodyText, 28, FontStyles.Normal, INK_SOFT);
            var bRT = body.rectTransform;
            bRT.anchorMin = new Vector2(0, 1); bRT.anchorMax = new Vector2(1, 1);
            bRT.pivot = new Vector2(0.5f, 1);
            bRT.offsetMin = new Vector2(56, -820); bRT.offsetMax = new Vector2(-56, -710);
            body.alignment = TextAlignmentOptions.Top;
            body.textWrappingMode = TextWrappingModes.Normal;

            // Continue button
            var btn = NewGO("Continue", card.transform, typeof(Image), typeof(Button));
            var nRT = btn.GetComponent<RectTransform>();
            nRT.anchorMin = new Vector2(0.5f, 0); nRT.anchorMax = new Vector2(0.5f, 0);
            nRT.pivot = new Vector2(0.5f, 0);
            nRT.anchoredPosition = new Vector2(0, 50);
            nRT.sizeDelta = new Vector2(520, 130);
            btn.GetComponent<Image>().color = BTN_GREEN;
            var b = btn.GetComponent<Button>(); b.targetGraphic = btn.GetComponent<Image>();
            b.onClick.AddListener(Hide);
            var lbl = MakeText(btn.transform, "L", "Continue", 42, FontStyles.Bold, Color.white);
            try { lbl.outlineWidth = 0.22f; lbl.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;

            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Victory); } catch {}
            Debug.Log($"[PetEvolution] Stage {newStage} ({stageName}) celebration shown.");
        }

        public static void Hide()
        {
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
        }

        // ── primitives ──
        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static GameObject NewGO(string name, Transform parent, params System.Type[] comps)
        {
            var go = new GameObject(name, new System.Type[] { typeof(RectTransform) });
            go.transform.SetParent(parent, false);
            foreach (var c in comps) go.AddComponent(c);
            return go;
        }

        private static TMP_Text MakeText(Transform parent, string name, string text, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text; tm.fontSize = size; tm.fontStyle = style; tm.color = color;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }

        private static void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (existing != null && existing.isActiveAndEnabled) return;
            var go = existing != null ? existing.gameObject : new GameObject("EventSystem");
            if (existing == null)
            {
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();   // Old Input Manager only — Input System package not installed.
            }
            go.SetActive(true);
            var es = go.GetComponent<EventSystem>();
            if (es != null) es.enabled = true;
        }
    }
}
