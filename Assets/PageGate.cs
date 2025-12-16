using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Put this on the invisible wall between two areas.
    /// It disables the collider once the player owns ALL the required pages.
    /// While the player is touching the gate without the pages,
    /// the LostPageUI counter text shows the lockedMessage.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PageGate : MonoBehaviour
    {
        [Header("Progression")]
        public ItemData[] requiredPages;   // Drag Page_01, Page_02, etc
        [TextArea]
        public string lockedMessage = "You feel something is missing... Go back and find the pages.";

        private bool isOpen = false;

        private void Reset()
        {
            // Helpful default: make collider trigger
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isOpen)
                return;

            // Only react to the player
            PlayerCharacter player = other.GetComponent<PlayerCharacter>();
            if (player == null)
                return;

            if (PlayerHasAllRequiredPages(player))
                OpenGate();
            else
                ShowLockedMessage(player);
        }

        private void OnTriggerExit(Collider other)
        {
            // When player leaves the gate area, restore normal counter text
            PlayerCharacter player = other.GetComponent<PlayerCharacter>();
            if (player == null)
                return;

            LostPageUI ui = player.GetComponent<LostPageUI>();
            if (ui != null)
                ui.RefreshCounterText();
        }

        private bool PlayerHasAllRequiredPages(PlayerCharacter player)
        {
            PlayerCharacterInventory inv = player.Inventory;
            if (inv == null || requiredPages == null || requiredPages.Length == 0)
                return false;

            foreach (ItemData page in requiredPages)
            {
                if (page != null && !inv.HasItem(page, 1))
                    return false;
            }
            return true;
        }

        private void OpenGate()
        {
            isOpen = true;
            GetComponent<Collider>().enabled = false;   // player can now pass
            Debug.Log("[PageGate] Opened gate because player has all required pages.");
        }

        private void ShowLockedMessage(PlayerCharacter player)
        {
            Debug.Log("[PageGate] Locked: " + lockedMessage);

            // Also push this message into the LostPageUI counter
            LostPageUI ui = player.GetComponent<LostPageUI>();
            if (ui != null)
                ui.ShowTemporaryMessage(lockedMessage);
        }
    }
}
