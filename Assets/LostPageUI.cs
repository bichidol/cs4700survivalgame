using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SurvivalEngine
{
    /// <summary>
    /// Handles the story-page popup + pages counter.
    /// Uses a list of distinct ItemData pages (Page_01..Page_06).
    /// Title / desc / icon are read from the ItemData itself.
    /// </summary>
    public class LostPageUI : MonoBehaviour
    {
        [Header("Story pages (ItemData, in order)")]
        public ItemData[] pages; // Page_01, Page_02, ..., Page_06

        [Header("Popup UI")]
        public GameObject panelRoot;
        public Image pageImage;
        public TMP_Text pageTitleText;
        public TMP_Text pageBodyText;

        [Header("Top Counter UI")]
        public TMP_Text counterText;

        [Header("Debug / Test")]
        [Tooltip("If true, the player will start with ALL pages in inventory (no zombies needed).")]
        public bool giveAllPagesOnStart = false;

        private PlayerCharacterInventory inv;
        private bool[] collectedFlags;   // which pages we already showed a popup for

        void Start()
        {
            PlayerCharacter player = GetComponent<PlayerCharacter>();
            if (player != null)
                inv = player.Inventory;

            if (inv != null)
                inv.onTakeItem += OnTakeItem;

            if (panelRoot != null)
                panelRoot.SetActive(false);

            int total = pages != null ? pages.Length : 0;
            collectedFlags = new bool[total];

            // ---- DEBUG: give all pages at start (for fast final-boss testing) ----
            if (giveAllPagesOnStart && inv != null && pages != null)
            {
                for (int i = 0; i < pages.Length; i++)
                {
                    ItemData page = pages[i];
                    if (page != null && !inv.HasItem(page, 1))
                    {
                        inv.GainItem(page, 1);  // this uses your normal inventory system
                    }
                }
            }

            // sync with inventory in case player starts with some pages
            SyncCollectedFromInventory();
            RefreshCounterText();
        }

        private void OnDestroy()
        {
            if (inv != null)
                inv.onTakeItem -= OnTakeItem;
        }

        private void SyncCollectedFromInventory()
        {
            if (inv == null || pages == null || collectedFlags == null)
                return;

            for (int i = 0; i < pages.Length; i++)
            {
                ItemData page = pages[i];
                if (page != null && inv.HasItem(page, 1))
                    collectedFlags[i] = true;
            }
        }

        private int GetCollectedCount()
        {
            if (collectedFlags == null)
                return 0;

            int count = 0;
            for (int i = 0; i < collectedFlags.Length; i++)
            {
                if (collectedFlags[i])
                    count++;
            }
            return count;
        }

        private int GetPageIndex(ItemData data)
        {
            if (pages == null || data == null)
                return -1;

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] == data)
                    return i;
            }
            return -1;
        }

        private void OnTakeItem(Item item)
        {
            if (item == null || item.data == null)
                return;

            int index = GetPageIndex(item.data);
            if (index < 0)
                return; // not one of our story pages

            // mark collected & show popup only the first time
            if (!collectedFlags[index])
            {
                collectedFlags[index] = true;
                ShowPagePopup(item.data);
            }

            RefreshCounterText();
        }

        private void ShowPagePopup(ItemData page)
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (pageImage != null)
                pageImage.sprite = page.icon;

            if (pageTitleText != null)
                pageTitleText.text = page.title;

            if (pageBodyText != null)
                pageBodyText.text = page.desc;
        }

        public void ClosePopup()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        /// <summary>Used by PageDamageZone / boss to temporarily override the text.</summary>
        public void ShowTemporaryMessage(string msg)
        {
            if (counterText != null)
                counterText.text = msg;
        }

        /// <summary>
        /// Returns true if the player has all story pages (by ItemData).
        /// Used by FinalBossSpawnZone.
        /// </summary>
        public bool HasAllPages()
        {
            if (pages == null || pages.Length == 0)
                return false;

            SyncCollectedFromInventory(); // make sure we’re up to date with inventory
            return GetCollectedCount() >= pages.Length;
        }

        /// <summary>Restores the normal / progression counter text.</summary>
        public void RefreshCounterText()
        {
            if (counterText == null || pages == null)
                return;

            SyncCollectedFromInventory();

            int collected = GetCollectedCount();
            int total = pages.Length;

            // When all pages are collected, show the “be ready” hint instead of X/Y
            if (collected >= total)
            {
                counterText.text =
                    "All lost pages found.\n" +
                    "Be fully prepared before you go behind the mountain.";
            }
            else
            {
                counterText.text = $"Lost pages found: {collected} / {total}";
            }
        }
    }
}
