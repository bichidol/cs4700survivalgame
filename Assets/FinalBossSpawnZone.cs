using UnityEngine;

namespace SurvivalEngine
{
    /// <summary>
    /// Spawns the final boss once, when the player has all pages
    /// and enters this zone. Not tied to day/night.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FinalBossSpawnZone : MonoBehaviour
    {
        [Header("Boss")]
        public Character bossPrefab;        // huge monster prefab
        public Transform spawnPoint;        // where it appears

        [Header("Audio")]
        public AudioClip spawnSound;        // roar / noise
        public float soundVolume = 1f;

        [Header("UI Message")]
        [TextArea]
        public string bossMessage = "Something massive is moving behind the mountain...";

        private bool spawned = false;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (spawned)
                return;

            PlayerCharacter player = other.GetComponent<PlayerCharacter>();
            if (player == null)
                return;

            LostPageUI pagesUI = player.GetComponent<LostPageUI>();
            if (pagesUI == null || !pagesUI.HasAllPages())
            {
                // Player came here too early: optional hint (could be removed if you prefer silence)
                if (pagesUI != null)
                {
                    pagesUI.ShowTemporaryMessage(
                        "You feel something watching you...\nBut the truth is still hidden in the missing pages."
                    );
                }
                return;
            }

            // Spawn the boss
            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            if (bossPrefab != null)
                Instantiate(bossPrefab.gameObject, pos, rot);

            // Play sound
            if (spawnSound != null)
                AudioSource.PlayClipAtPoint(spawnSound, pos, soundVolume);

            spawned = true;

            // Override top text with a scary message
            if (pagesUI != null)
                pagesUI.ShowTemporaryMessage(bossMessage);

            Debug.Log("[FinalBossSpawnZone] Final boss spawned.");
        }
    }
}
