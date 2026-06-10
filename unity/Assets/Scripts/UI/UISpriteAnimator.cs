using UnityEngine;
using UnityEngine.UI;

namespace Sparq.UI
{
    /// <summary>
    /// Cycles a UI Image's sprite through a frame array at a given FPS.
    /// Lightweight runtime alternative to using an Animator Controller on UI.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UISpriteAnimator : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 8f;
        public bool loop = true;

        private Image _img;
        private float _t;
        private int   _frame;

        private void Awake()
        {
            _img = GetComponent<Image>();
        }

        public void SetFrames(Sprite[] f, float framesPerSecond = 8f)
        {
            frames = f;
            fps    = framesPerSecond;
            _frame = 0;
            _t     = 0f;
            if (frames != null && frames.Length > 0 && _img != null)
                _img.sprite = frames[0];
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0 || _img == null) return;
            _t += Time.deltaTime;
            float frameDur = 1f / Mathf.Max(0.01f, fps);
            while (_t >= frameDur)
            {
                _t -= frameDur;
                _frame++;
                if (_frame >= frames.Length)
                {
                    if (loop) _frame = 0;
                    else { _frame = frames.Length - 1; return; }
                }
                _img.sprite = frames[_frame];
            }
        }
    }
}
