using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Wandering chibi NPCs that live on the home backdrop. Each picks random
    /// patrol points, idle-bounces, occasionally shows a chat bubble. Adds the
    /// "world is alive" feel that polished mobile RPGs (Top Heroes etc.) have.
    /// </summary>
    public class HomeNpc : MonoBehaviour
    {
        public Vector2 patrolMin = new Vector2(120, 80);
        public Vector2 patrolMax = new Vector2(900, 220);
        public float walkSpeed = 35f;
        public float bounceAmplitude = 4f;
        public float bounceSpeed = 6f;
        public bool   twitchy = false;        // squirrel mode — frantic look-around
        public bool   majestic = false;       // dragon mode — slow float + sparkles
        public string[] customChat = null;

        private RectTransform _rt;
        private Image _img;
        private Vector2 _target;
        private float _restUntil;
        private float _nextBubble;
        private bool _facingRight = true;

        private static readonly string[] CHAT = {
            "Hi, friend!",
            "Done a quest yet?",
            "I love it here!",
            "Mochi's getting hungry...",
            "You've got this!",
            "Karu's cool, huh?",
            "Take a stretch break!",
            "Quest streak feels great!",
            "Wisp says hi!",
            "Drink some water 💧",
        };

        public void Init(RectTransform rt, Image img)
        {
            _rt = rt;
            _img = img;
            PickNewTarget();
            _nextBubble = Time.time + Random.Range(8f, 18f);
        }

        private void Update()
        {
            if (_rt == null) return;

            // Idle bounce always
            float by = Mathf.Sin(Time.time * bounceSpeed + (_rt.GetInstanceID() % 7)) * bounceAmplitude;

            // Dragon-mode: slow majestic float + idle sparkle particles
            if (majestic)
            {
                // Gentle figure-8 hover
                float hx = Mathf.Sin(Time.time * 0.8f) * 6f;
                _rt.localPosition = new Vector3(_rt.localPosition.x, _rt.localPosition.y, 0); // reset
                // Wing-flap-like rotation drift
                float drift = Mathf.Sin(Time.time * 1.2f) * 6f;
                _rt.localRotation = Quaternion.Euler(0, 0, drift);
                // Small breath rhythm
                float scale = 1f + Mathf.Sin(Time.time * 1.6f) * 0.04f;
                _rt.localScale = new Vector3((_facingRight ? 1f : -1f) * scale, scale, 1f);
                // Spawn an occasional sparkle
                if (Random.value < 0.04f) SpawnSparkle();
            }

            // Squirrel-mode: frantic head look-around (rapid rotation jitter)
            if (twitchy)
            {
                // Sharp twitches: noise-driven rotation + occasional sudden snap
                float jitter = (Mathf.PerlinNoise(Time.time * 4f, _rt.GetInstanceID() * 0.13f) - 0.5f) * 24f;
                // Random startled snaps every few seconds
                float snap = (Mathf.Sin(Time.time * 17f) > 0.95f) ? Random.Range(-15f, 15f) : 0f;
                _rt.localRotation = Quaternion.Euler(0, 0, jitter + snap);
                // Tiny scale jiggle so the body looks alert
                float s = 1f + Mathf.Sin(Time.time * 11f) * 0.04f;
                _rt.localScale = new Vector3((_facingRight ? 1f : -1f) * s, s, 1f);
            }

            if (Time.time < _restUntil)
            {
                _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x, _target.y + by);
            }
            else
            {
                Vector2 cur = _rt.anchoredPosition;
                Vector2 dir = (_target - new Vector2(cur.x, _target.y)).normalized;
                if (dir.x > 0.05f && !_facingRight) { _facingRight = true; FlipFacing(); }
                else if (dir.x < -0.05f && _facingRight) { _facingRight = false; FlipFacing(); }

                Vector2 next = new Vector2(cur.x + dir.x * walkSpeed * Time.deltaTime, _target.y + by);
                _rt.anchoredPosition = next;

                if (Mathf.Abs(next.x - _target.x) < 4f)
                {
                    _restUntil = Time.time + Random.Range(1.2f, 3.5f);
                    PickNewTarget();
                }
            }

            // Chat bubbles disabled — user request ("remove pop up sayings of the bugs")
            // The patrolling/bouncing animation still runs; just no text popups.
            // To re-enable, uncomment this block.
            // if (Time.time >= _nextBubble)
            // {
            //     var pool = (customChat != null && customChat.Length > 0) ? customChat : CHAT;
            //     ShowBubble(pool[Random.Range(0, pool.Length)]);
            //     _nextBubble = Time.time + (twitchy ? Random.Range(6f, 12f) : Random.Range(15f, 30f));
            // }
        }

        private void PickNewTarget()
        {
            _target = new Vector2(
                Random.Range(patrolMin.x, patrolMax.x),
                Random.Range(patrolMin.y, patrolMax.y));
        }

        private void FlipFacing()
        {
            if (_rt == null) return;
            var s = _rt.localScale;
            s.x = Mathf.Abs(s.x) * (_facingRight ? 1f : -1f);
            _rt.localScale = s;
        }

        private void ShowBubble(string text)
        {
            // Make a rounded white pill above the NPC
            var canvas = transform.parent;
            if (canvas == null) return;

            var bubble = new GameObject("ChatBubble", typeof(RectTransform), typeof(Image));
            bubble.transform.SetParent(canvas, false);
            var brt = bubble.GetComponent<RectTransform>();
            brt.anchorMin = _rt.anchorMin; brt.anchorMax = _rt.anchorMax;
            brt.pivot = new Vector2(0.5f, 0);
            brt.anchoredPosition = _rt.anchoredPosition + new Vector2(0, 90);
            // Auto-size based on text length
            float w = Mathf.Clamp(text.Length * 13 + 36, 140, 320);
            brt.sizeDelta = new Vector2(w, 56);
            var bImg = bubble.GetComponent<Image>();
            bImg.sprite = LoadRoundedSprite(20);
            bImg.type = Image.Type.Sliced;
            bImg.color = new Color(1f, 0.97f, 0.85f, 0.96f);
            bImg.raycastTarget = false;

            // Tail (small triangle below bubble pointing down)
            var tail = new GameObject("Tail", typeof(RectTransform), typeof(Image));
            tail.transform.SetParent(bubble.transform, false);
            var trt = tail.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0); trt.anchorMax = new Vector2(0.5f, 0);
            trt.pivot = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, 0);
            trt.sizeDelta = new Vector2(20, 12);
            trt.localRotation = Quaternion.Euler(0, 0, 45);
            tail.GetComponent<Image>().color = new Color(1f, 0.97f, 0.85f, 0.96f);
            tail.GetComponent<Image>().raycastTarget = false;

            // Text on a CHILD GameObject (can't have two Graphic components on one)
            var txtGO = new GameObject("Txt", typeof(RectTransform));
            txtGO.transform.SetParent(bubble.transform, false);
            var txtRT = txtGO.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(8, 4); txtRT.offsetMax = new Vector2(-8, -4);
            var tm = txtGO.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = 18;
            tm.fontStyle = FontStyles.Bold;
            tm.color = new Color(0.20f, 0.10f, 0.05f);
            tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;

            StartCoroutine(BubbleLife(brt, bImg, tm));
        }

        private IEnumerator BubbleLife(RectTransform brt, Image img, TMP_Text tm)
        {
            // Pop in
            float t = 0f, dur = 0.18f;
            while (t < dur && brt != null)
            {
                t += Time.deltaTime;
                float k = t / dur;
                brt.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, k);
                yield return null;
            }
            // Hold
            yield return new WaitForSeconds(2.5f);
            // Fade out
            t = 0f; dur = 0.4f;
            while (t < dur && brt != null)
            {
                t += Time.deltaTime;
                float k = t / dur;
                if (img != null) { var c = img.color; c.a = (1f - k) * 0.96f; img.color = c; }
                if (tm  != null) { var c = tm.color; c.a = 1f - k; tm.color = c; }
                yield return null;
            }
            if (brt != null) Destroy(brt.gameObject);
        }

        // Tiny sparkle floating up from the dragon
        private void SpawnSparkle()
        {
            if (_rt == null || _rt.parent == null) return;
            var go = new GameObject("Sparkle", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_rt.parent, false);
            var srt = go.GetComponent<RectTransform>();
            srt.anchorMin = _rt.anchorMin; srt.anchorMax = _rt.anchorMax;
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = _rt.anchoredPosition + new Vector2(
                Random.Range(-50f, 50f),
                Random.Range(20f, 80f));
            srt.sizeDelta = new Vector2(8 + Random.Range(0, 8), 8 + Random.Range(0, 8));
            var img = go.GetComponent<Image>();
            img.sprite = LoadCircleSprite();
            img.color = new Color(1f, 0.95f, 0.55f, 0.95f);
            img.raycastTarget = false;
            StartCoroutine(SparkleLife(srt, img));
        }

        private IEnumerator SparkleLife(RectTransform srt, Image img)
        {
            float t = 0f, dur = 0.9f;
            Vector2 start = srt.anchoredPosition;
            float driftX = Random.Range(-15f, 15f);
            while (t < dur && srt != null)
            {
                t += Time.deltaTime;
                float k = t / dur;
                srt.anchoredPosition = start + new Vector2(driftX * k, 60f * k);
                if (img != null) { var c = img.color; c.a = (1f - k) * 0.95f; img.color = c; }
                srt.localScale = Vector3.one * (1f - k * 0.5f);
                yield return null;
            }
            if (srt != null) Destroy(srt.gameObject);
        }

        private static Sprite _circleSp;
        private static Sprite LoadCircleSprite()
        {
            if (_circleSp != null) return _circleSp;
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            float r = s * 0.46f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                tex.SetPixel(x, y, d <= r ? Color.white : new Color(0,0,0,0));
            }
            tex.Apply();
            _circleSp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
            return _circleSp;
        }

        // ─────── Sprite helpers ───────
        private static System.Collections.Generic.Dictionary<int, Sprite> _roundedCache
            = new System.Collections.Generic.Dictionary<int, Sprite>();
        private static Sprite LoadRoundedSprite(int radius)
        {
            if (_roundedCache.TryGetValue(radius, out var sp) && sp != null) return sp;
            int size = radius * 2 + 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside; int dx = 0, dy = 0;
                if (x < radius && y < radius) { dx = radius - x; dy = radius - y; inside = dx*dx+dy*dy <= radius*radius; }
                else if (x >= size-radius && y < radius) { dx = x-(size-radius-1); dy = radius-y; inside = dx*dx+dy*dy <= radius*radius; }
                else if (x < radius && y >= size-radius) { dx = radius-x; dy = y-(size-radius-1); inside = dx*dx+dy*dy <= radius*radius; }
                else if (x >= size-radius && y >= size-radius) { dx = x-(size-radius-1); dy = y-(size-radius-1); inside = dx*dx+dy*dy <= radius*radius; }
                else inside = true;
                tex.SetPixel(x, y, inside ? Color.white : new Color(0,0,0,0));
            }
            tex.Apply();
            sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            _roundedCache[radius] = sp;
            return sp;
        }
    }

}
