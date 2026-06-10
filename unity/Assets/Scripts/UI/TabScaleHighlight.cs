using UnityEngine;
using UnityEngine.UI;

namespace Sparq.UI
{
    /// <summary>
    /// Scales the active tab up slightly to highlight it without dimming the others.
    /// Pair with TabGroup — listens to the same buttons and updates scale on click.
    /// </summary>
    public class TabScaleHighlight : MonoBehaviour
    {
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private float activeScale   = 1.08f;
        [SerializeField] private float inactiveScale = 1.0f;

        private void Start()
        {
            if (tabButtons == null) return;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int idx = i;
                if (tabButtons[i] != null)
                    tabButtons[i].onClick.AddListener(() => SetActive(idx));
            }
            SetActive(0);
        }

        public void SetActive(int activeIdx)
        {
            if (tabButtons == null) return;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                float s = (i == activeIdx) ? activeScale : inactiveScale;
                tabButtons[i].transform.localScale = new Vector3(s, s, 1f);
            }
        }
    }
}
