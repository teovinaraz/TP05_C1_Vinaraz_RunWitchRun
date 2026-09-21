using UnityEngine;

public class Collectible : Pickup
{
    protected override void OnCollected(PlayerController player)
    {
        GameManager.Instance.AddScore(player.Data.moonGemPoints);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGem();
        }
    }
}
