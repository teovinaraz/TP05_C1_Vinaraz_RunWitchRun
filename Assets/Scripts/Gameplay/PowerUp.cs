using UnityEngine;

public enum PowerUpType
{
    MagicBroom
}

public class PowerUp : Pickup
{
    [SerializeField] private PowerUpType type = PowerUpType.MagicBroom;

    protected override void OnCollected(PlayerController player)
    {
        switch (type)
        {
            case PowerUpType.MagicBroom:
                player.ActivateBroom();
                break;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPowerUp();
        }
    }
}
