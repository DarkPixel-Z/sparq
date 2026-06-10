using UnityEngine;
using UnityEngine.UI;

namespace Sparq.UI
{
    /// <summary>
    /// Attaches to HomeNavButtons. Re-wires MAP/SHOP/BAG button onClicks at runtime
    /// — lambdas added via Editor code don't persist through scene save, so we need
    /// an actual MonoBehaviour living on the object.
    /// </summary>
    public class HomeNavBar : MonoBehaviour
    {
        private void Start()
        {
            int wired = 0;
            wired += WireButton("MapBtn",   "map",   OpenMap);
            wired += WireButton("ShopBtn",  "shop",  OpenShop);
            wired += WireButton("BagBtn",   "bag",   OpenBag);
            wired += WireButton("PetsBtn",  "pets",  OpenPets);
            wired += WireButton("WorldBtn", "world", OpenWorld);
            Debug.Log($"[HomeNavBar] Wired {wired}/5 top buttons.");
        }

        private int WireButton(string exactName, string keyword, UnityEngine.Events.UnityAction action)
        {
            // Try exact name first (recursive)
            var btn = FindByName(transform, exactName);
            // Fallback: keyword match across the whole scene
            if (btn == null)
            {
                foreach (var b in Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    string n = b.gameObject.name.ToLower();
                    if (n.Contains(keyword)
                        && !n.Contains("popup") && !n.Contains("addquest"))
                    {
                        btn = b; break;
                    }
                }
            }
            if (btn == null) return 0;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
            return 1;
        }

        private static Button FindByName(Transform t, string name)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                if (child.name == name)
                {
                    var b = child.GetComponent<Button>();
                    if (b != null) return b;
                    b = child.GetComponentInChildren<Button>(true);
                    if (b != null) return b;
                }
                var deep = FindByName(child, name);
                if (deep != null) return deep;
            }
            return null;
        }

        public void OpenMap()  { Sparq.UI.StageMapPanel.Show(); }
        public void OpenShop() { PopupManager.Instance?.OpenShop(); }
        public void OpenBag()  { Sparq.UI.BagPanel.Show(); }
        public void OpenEquipment() { Sparq.UI.EquipmentPanel.Show(); }
        public void OpenPets() { PetSelector.Show(); }
        public void OpenWorld()
        {
            // Disable the legacy static SocialPanel prefab if present, then open
            // the procedural WorldPanel (Friends / Guild / World / Browse / Heroes).
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") go.SetActive(false);
            }
            WorldPanel.Show();
        }
    }
}
