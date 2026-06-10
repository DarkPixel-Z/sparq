using UnityEngine;
using UnityEngine.UI;

namespace Sparq.UI
{
    /// <summary>
    /// Tiny utility — toggles a target GameObject's active state when this
    /// Button is clicked. Lets editor scripts wire things via SerializedObject
    /// so the reference survives Play mode.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PanelToggle : MonoBehaviour
    {
        public GameObject target;
        public bool setActiveOnClick = true; // false = SetActive(false)

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (target == null) return;
            target.SetActive(setActiveOnClick ? true : !target.activeSelf);
        }
    }
}
