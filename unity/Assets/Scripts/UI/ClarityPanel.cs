using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sparq.Systems;

namespace Sparq.UI
{
    /// <summary>
    /// "Tome of Clarity" — fullscreen panel listing wisdom cards.
    /// Tap a card to expand into a fullscreen reading view with a Practice button (+5 XP).
    /// </summary>
    public class ClarityPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform deckRoot;     // grid parent
        [SerializeField] private GameObject    cardDetailRoot; // detail sub-panel
        [SerializeField] private TMP_Text      detailTitle;
        [SerializeField] private TMP_Text      detailBody;
        [SerializeField] private TMP_Text      detailPracticedCount;
        [SerializeField] private Button        detailPracticeBtn;
        [SerializeField] private Button        detailCloseBtn;
        [SerializeField] private Button        panelCloseBtn;
        [SerializeField] private TMP_Text      headerLabel;

        private string _selectedCardId;

        private void Start()
        {
            if (panelCloseBtn   != null) panelCloseBtn.onClick.AddListener(Hide);
            if (detailCloseBtn  != null) detailCloseBtn.onClick.AddListener(CloseDetail);
            if (detailPracticeBtn != null) detailPracticeBtn.onClick.AddListener(OnPractice);
            if (cardDetailRoot != null) cardDetailRoot.SetActive(false);
            RefreshHeader();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            CloseDetail();
            RefreshHeader();
        }

        public void Hide() => gameObject.SetActive(false);

        public void OpenCard(string cardId)
        {
            var card = ClarityService.FindById(cardId);
            if (card == null) return;
            _selectedCardId = cardId;
            if (detailTitle != null) detailTitle.text = card.title;
            if (detailBody  != null) detailBody.text  = card.body;
            if (detailPracticedCount != null)
                detailPracticedCount.text = $"Practiced {ClarityService.PracticedCount(cardId)}×";
            if (cardDetailRoot != null) cardDetailRoot.SetActive(true);
        }

        public void CloseDetail()
        {
            if (cardDetailRoot != null) cardDetailRoot.SetActive(false);
            _selectedCardId = null;
        }

        private void OnPractice()
        {
            if (string.IsNullOrEmpty(_selectedCardId)) return;
            ClarityService.Practice(_selectedCardId);
            if (detailPracticedCount != null)
                detailPracticedCount.text = $"Practiced {ClarityService.PracticedCount(_selectedCardId)}×";
            RefreshHeader();

            // Toast
            try
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                    XPFloater.Spawn(canvas.transform,
                        transform.position + new Vector3(0, 80, 0),
                        "+5 XP — Wisdom gained",
                        new Color(1f, 0.85f, 0.4f));
            } catch {}
        }

        private void RefreshHeader()
        {
            if (headerLabel != null)
                headerLabel.text = $"Tome of Clarity  ·  {ClarityService.TotalPracticed} practiced";
        }
    }
}
