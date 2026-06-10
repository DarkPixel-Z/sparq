using UnityEngine;
using UnityEngine.UI;

namespace Sparq.UI
{
    /// <summary>
    /// Swaps each tab button's sprite based on which is currently active.
    /// Pair with TabGroup — when a tab is clicked, that button shows the
    /// "select" sprite while others show the "normal" sprite.
    /// </summary>
    public class TabSpriteSwap : MonoBehaviour
    {
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite selectSprite;
        [SerializeField] private Button[] tabButtons;

        private void Start()
        {
            if (tabButtons == null) return;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int idx = i;
                if (tabButtons[i] != null)
                    tabButtons[i].onClick.AddListener(() => SwapTo(idx));
            }
            SwapTo(0);
        }

        private void SwapTo(int activeIdx)
        {
            if (tabButtons == null) return;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                var img = tabButtons[i].GetComponent<Image>();
                if (img == null) continue;
                img.sprite = (i == activeIdx) ? selectSprite : normalSprite;
                img.type = Image.Type.Sliced;
            }
        }
    }
}
