using UnityEngine;

namespace Sparq.Cinematic
{
    /// <summary>
    /// Applies a subtle drift + zoom oscillation to the camera to make the
    /// scene feel "alive". Attach to Main Camera.
    /// </summary>
    public class CameraBreathing : MonoBehaviour
    {
        [SerializeField] private float panAmp    = 0.08f;  // world units
        [SerializeField] private float zoomAmp   = 0.04f;  // orthoSize delta
        [SerializeField] private float panSpeed  = 0.12f;
        [SerializeField] private float zoomSpeed = 0.09f;

        private Vector3 _basePos;
        private float   _baseOrtho;
        private Camera  _cam;

        private void Awake()
        {
            _basePos   = transform.position;
            _cam       = GetComponent<Camera>();
            if (_cam != null) _baseOrtho = _cam.orthographicSize;
        }

        private void Update()
        {
            float t = Time.time;
            float dx = Mathf.Sin(t * panSpeed * Mathf.PI * 2f) * panAmp;
            float dy = Mathf.Cos(t * panSpeed * Mathf.PI * 2f * 0.7f) * panAmp * 0.5f;
            transform.position = _basePos + new Vector3(dx, dy, 0);

            if (_cam != null && _cam.orthographic)
            {
                float dz = Mathf.Sin(t * zoomSpeed * Mathf.PI * 2f) * zoomAmp;
                _cam.orthographicSize = _baseOrtho + dz;
            }
        }
    }
}
