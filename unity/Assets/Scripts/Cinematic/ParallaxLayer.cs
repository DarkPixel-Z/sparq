using UnityEngine;

namespace Sparq.Cinematic
{
    /// <summary>
    /// Slow horizontal drift driven by time, simulating wind / distant motion.
    /// Different speeds per layer give a "depth" feel without needing real camera movement.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] public float driftSpeed = 0.2f;   // world units / sec
        [SerializeField] public float swayAmplitude = 0.05f; // up/down sway
        [SerializeField] public float swayFrequency = 0.4f;

        private Vector3 _basePos;
        private float   _t;

        private void Awake()
        {
            _basePos = transform.position;
            _t = Random.Range(0f, 10f); // each layer offset to avoid sync
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float dx = Mathf.Sin(_t * driftSpeed * Mathf.PI * 0.4f) * 0.5f;
            float dy = Mathf.Sin(_t * swayFrequency * Mathf.PI * 2f) * swayAmplitude;
            transform.position = _basePos + new Vector3(dx, dy, 0f);
        }
    }
}
