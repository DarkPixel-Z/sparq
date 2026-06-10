using UnityEngine;

namespace Sparq.Cinematic
{
    /// <summary>
    /// Loops a subtle scale breathe on a Transform (1.0 ↔ 1.0 + amplitude)
    /// at the given speed. Attach to anything alive — Karu, Hellhound, etc.
    /// </summary>
    public class IdleBreathing : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.025f;
        [SerializeField] private float speed     = 1.2f;
        [SerializeField] private float phaseOffset = 0f; // seconds

        private Vector3 _baseScale;
        private float   _t;

        private void Awake()  => _baseScale = transform.localScale;
        private void OnEnable() => _t = phaseOffset;

        private void Update()
        {
            _t += Time.deltaTime * speed;
            float k = Mathf.Sin(_t * Mathf.PI * 2f * 0.3f); // slow sine
            float s = 1f + k * amplitude;
            transform.localScale = _baseScale * s;
        }
    }
}
