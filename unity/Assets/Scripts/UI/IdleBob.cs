using UnityEngine;

namespace Sparq.UI
{
    /// <summary>
    /// Subtle Y-axis bob loop for hero / pet idle ambience.
    /// Adds life without being distracting.
    /// </summary>
    public class IdleBob : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.08f;
        [SerializeField] private float frequency = 1.2f;
        [SerializeField] private float phase     = 0f;

        private Vector3 _basePos;

        private void Awake() => _basePos = transform.position;

        private void OnEnable() => _basePos = transform.position;

        private void Update()
        {
            float y = Mathf.Sin((Time.time + phase) * frequency * Mathf.PI * 2f) * amplitude;
            transform.position = new Vector3(_basePos.x, _basePos.y + y, _basePos.z);
        }

        public void SetParams(float amp, float freq, float ph)
        {
            amplitude = amp; frequency = freq; phase = ph;
            _basePos = transform.position;
        }
    }
}
