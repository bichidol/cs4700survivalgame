using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Trigger volume just beyond a wall/area.
    /// If the player doesn't own ALL required pages, they take damage
    /// and the LostPageUI counter shows a warning message.
    /// Also exposes IsPlayerInUnsafeZone so the spawner can pause.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PageDamageZone : MonoBehaviour
    {
        [Header("Required Pages to be Safe")]
        public ItemData[] requiredPages;   // e.g. Area2: Page_01 + Page_02

        [Header("Damage Settings")]
        public float damagePerSecond = 5f;

        [Header("Messages")]
        [TextArea]
        public string warningMessage = "You feel sick... You left pages behind. Go back.";

        public static bool IsPlayerInUnsafeZone = false;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            PlayerCharacter player = other.GetComponent<PlayerCharacter>();
            if (player == null)
                return;

            PlayerCharacterInventory inv = player.Inventory;
            if (inv == null || requiredPages == null || requiredPages.Length == 0)
                return;

            bool safe = HasAllRequiredPages(inv);

            LostPageUI ui = player.GetComponent<LostPageUI>();

            if (!safe)
            {
                IsPlayerInUnsafeZone = true;

                // Damage over time
                float dmg = damagePerSecond * Time.deltaTime;
                player.Attributes.AddAttribute(AttributeType.Health, -dmg);

                // Show warning instead of normal counter text
                if (ui != null)
                    ui.ShowTemporaryMessage(warningMessage);
            }
            else
            {
                // We are inside the zone but have all pages → no damage,
                // restore normal counter text.
                IsPlayerInUnsafeZone = false;

                if (ui != null)
                    ui.RefreshCounterText();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerCharacter player = other.GetComponent<PlayerCharacter>();
            if (player == null)
                return;

            IsPlayerInUnsafeZone = false;

            LostPageUI ui = player.GetComponent<LostPageUI>();
            if (ui != null)
                ui.RefreshCounterText();
        }

        private bool HasAllRequiredPages(PlayerCharacterInventory inv)
        {
            foreach (ItemData page in requiredPages)
            {
                if (page != null && !inv.HasItem(page, 1))
                    return false;
            }
            return true;
        }
    }
}
