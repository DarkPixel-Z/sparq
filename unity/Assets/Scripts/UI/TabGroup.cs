using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Tab switcher — pairs a button with a content GameObject.
    /// When a tab is clicked, only its content is active and that tab gets the
    /// active visual treatment. References survive Play mode (SerializeField).
    /// </summary>
    public class TabGroup : MonoBehaviour
    {
        [System.Serializable]
        public class Entry
        {
            public Button button;
            public GameObject content;
        }

        [SerializeField] private Entry[] tabs;
        [SerializeField] private Color activeBg   = new Color(1f, 0.82f, 0.32f);
        [SerializeField] private Color inactiveBg = new Color(1f, 1f, 1f, 0.10f);
        [SerializeField] private Color activeFg   = new Color(0.10f, 0.08f, 0.18f);
        [SerializeField] private Color inactiveFg = new Color(1f, 0.95f, 0.82f);
        [SerializeField] private int defaultIndex = 0;

        private void Start()
        {
            if (tabs == null) return;
            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                if (tabs[i].button != null)
                    tabs[i].button.onClick.AddListener(() => Switch(idx));
            }
            Switch(defaultIndex);
        }

        public void Switch(int idx)
        {
            if (tabs == null) return;
            for (int i = 0; i < tabs.Length; i++)
            {
                bool active = (i == idx);
                if (tabs[i].content != null) tabs[i].content.SetActive(active);
                if (tabs[i].button != null)
                {
                    var img = tabs[i].button.GetComponent<Image>();
                    if (img != null) img.color = active ? activeBg : inactiveBg;
                    foreach (var tm in tabs[i].button.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.color = active ? activeFg : inactiveFg;
                    }
                }
            }
        }
    }
}
