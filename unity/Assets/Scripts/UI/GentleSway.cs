using UnityEngine;

namespace Sparq.UI
{
    /// <summary>Subtle rotation oscillation — like wind through trees.</summary>
    public class GentleSway : MonoBehaviour
    {
        public float amplitude = 3f;        // degrees
        public float speed = 0.6f;
        public float phase = 0f;

        private void Update()
        {
            float angle = Mathf.Sin((Time.time + phase) * speed * Mathf.PI * 2f) * amplitude;
            transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}
