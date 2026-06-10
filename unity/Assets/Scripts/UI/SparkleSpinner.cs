using UnityEngine;

namespace Sparq.UI
{
    /// <summary>
    /// Slow rotation — gives a subtle "shine" feel to completed stage halos.
    /// </summary>
    public class SparkleSpinner : MonoBehaviour
    {
        public float degreesPerSec = 22f;
        private void Update() => transform.Rotate(0, 0, degreesPerSec * Time.deltaTime);
    }
}
