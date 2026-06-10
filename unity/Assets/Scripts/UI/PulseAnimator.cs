using UnityEngine;

namespace Sparq.UI
{
    /// <summary>
    /// Subtle scale pulse — used to draw the eye to "play this next" stage nodes.
    /// </summary>
    public class PulseAnimator : MonoBehaviour
    {
        public float minScale = 0.96f;
        public float maxScale = 1.10f;
        public float speed = 1.6f;
        public float phase = 0f;

        private Vector3 _baseScale = Vector3.one;

        private void Awake() { _baseScale = transform.localScale; }
        private void OnEnable() { _baseScale = transform.localScale; }

        private void Update()
        {
            float t = (Mathf.Sin((Time.time + phase) * speed * Mathf.PI) + 1f) * 0.5f;
            float s = Mathf.Lerp(minScale, maxScale, t);
            transform.localScale = _baseScale * s;
        }
    }
}
