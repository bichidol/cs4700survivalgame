using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Spawns ONE zombie based on how many distinct story pages
    /// (pagesByIndex) the player owns.
    /// pagesByIndex[0] = Page_01, zombiesByIndex[0] = Zombie 1, etc.
    /// - In normal mode: at most one spawn per night for current progress.
    /// - In testMode: at most one spawn per page index, even if timer fires multiple times.
    /// </summary>
    public class NightZombieSpawner : MonoBehaviour
    {
        [Header("Zombies & Pages (same order)")]
        public Character[] zombiesByIndex;   // 0..5 → your 6 zombie prefabs
        public ItemData[] pagesByIndex;      // 0..5 → Page_01..Page_06 (unique IDs!)

        [Header("Spawn around player")]
        public float minSpawnRadius = 10f;
        public float maxSpawnRadius = 18f;
        public LayerMask groundMask = ~0;

        [Header("Area restriction (optional)")]
        public int firstPageIndex = 0;       // inclusive
        public int lastPageIndex = 5;        // inclusive

        [Header("TEST MODE (ignore night)")]
        public bool testMode = false;
        public float testSpawnInterval = 60f;

        [Header("DEBUG")]
        public bool spawnOnTopOfPlayer = false; // if true, ignore radius and spawn exactly at player
        public bool logDetailedPages = true;    // log per-page HasItem info

        private bool hasSpawnedThisNight = false;
        private bool wasNightLastFrame = false;
        private float testTimer = 0f;

        // remember which page index we already spawned for
        // -1 means nothing spawned yet
        private int lastSpawnedIndex = -1;

        private void Update()
        {
            TheGame game = TheGame.Get();
            if (game == null)
                return;

            // Don't spawn if player is in a "wrong" damage zone
            if (PageDamageZone.IsPlayerInUnsafeZone)
                return;

            // ---------- TEST MODE ----------
            if (testMode)
            {
                testTimer += Time.deltaTime;
                if (testTimer >= testSpawnInterval)
                {
                    testTimer = 0f;
                    TrySpawnForThisProgress();  // lastSpawnedIndex blocks duplicates
                }
                return;
            }

            // ---------- NORMAL NIGHT LOGIC ----------
            bool isNight = game.IsNight();

            // Day -> night transition
            if (isNight && !wasNightLastFrame)
                hasSpawnedThisNight = false;

            if (isNight && !hasSpawnedThisNight)
            {
                if (TrySpawnForThisProgress())
                    hasSpawnedThisNight = true;
            }

            wasNightLastFrame = isNight;
        }

        private bool TrySpawnForThisProgress()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();
            if (player == null)
                return false;

            PlayerCharacterInventory inv = player.Inventory;
            if (inv == null)
                return false;

            if (zombiesByIndex == null || zombiesByIndex.Length == 0 ||
                pagesByIndex == null || pagesByIndex.Length == 0)
            {
                Debug.LogWarning("[NightZombieSpawner] Missing zombiesByIndex or pagesByIndex setup.");
                return false;
            }

            // --- How many of the specific story pages does the player own? ---
            int pagesOwned = 0;
            for (int i = 0; i < pagesByIndex.Length; i++)
            {
                ItemData page = pagesByIndex[i];
                bool has = page != null && inv.HasItem(page, 1);
                if (logDetailedPages)
                {
                    string pid = page != null ? page.id : "NULL";
                    Debug.Log($"[NightZombieSpawner] Has page {i} ({pid}): {has}");
                }

                if (has)
                    pagesOwned++;
            }

            int nextIndex = pagesOwned; // 0 when own 0 pages, 1 when own 1 page, etc.
            Debug.Log($"[NightZombieSpawner] pagesOwned={pagesOwned}, nextIndex={nextIndex}, lastSpawnedIndex={lastSpawnedIndex}");

            // All pages done? no more zombies
            if (nextIndex >= zombiesByIndex.Length)
            {
                Debug.Log("[NightZombieSpawner] All zombies already spawned.");
                return false;
            }

            // Don't spawn again for the same index
            if (nextIndex == lastSpawnedIndex)
            {
                // We've already spawned the zombie for this page count,
                // wait until player picks that page and pagesOwned goes up.
                return false;
            }

            // Check if this spawner is responsible for that index (for areas)
            if (nextIndex < firstPageIndex || nextIndex > lastPageIndex)
            {
                Debug.Log($"[NightZombieSpawner] nextIndex {nextIndex} outside my range [{firstPageIndex}, {lastPageIndex}]");
                return false;
            }

            Character prefab = zombiesByIndex[nextIndex];
            if (prefab == null)
            {
                Debug.LogWarning($"[NightZombieSpawner] zombiesByIndex[{nextIndex}] is null.");
                return false;
            }

            Vector3 spawnPos = spawnOnTopOfPlayer
                ? player.GetPosition()
                : FindSpawnPositionAround(player.GetPosition());

            Debug.Log($"[NightZombieSpawner] Spawning {prefab.name} for index {nextIndex} at {spawnPos}");

            Instantiate(prefab.gameObject, spawnPos, Quaternion.identity);

            lastSpawnedIndex = nextIndex; // remember we spawned for this page count

            return true;
        }

        private Vector3 FindSpawnPositionAround(Vector3 center)
        {
            for (int i = 0; i < 10; i++)
            {
                float radius = Random.Range(minSpawnRadius, maxSpawnRadius);
                Vector2 circle = Random.insideUnitCircle.normalized * radius;
                Vector3 posAbove = new Vector3(center.x + circle.x, center.y + 20f, center.z + circle.y);

                if (Physics.Raycast(posAbove, Vector3.down, out RaycastHit hit, 40f, groundMask))
                {
                    Debug.Log($"[NightZombieSpawner] Found ground spawn at {hit.point}");
                    return hit.point;
                }
            }

            // Fallback: just in front of player
            Vector3 fallback = center + playerForwardFlat() * minSpawnRadius;
            Debug.Log($"[NightZombieSpawner] Using fallback spawn at {fallback}");
            return fallback;
        }

        private Vector3 playerForwardFlat()
        {
            PlayerCharacter player = PlayerCharacter.GetFirst();
            if (player != null)
            {
                Vector3 f = player.GetFacing();
                f.y = 0f;
                if (f.sqrMagnitude > 0.001f)
                    return f.normalized;
            }
            return Vector3.forward;
        }
    }
}
