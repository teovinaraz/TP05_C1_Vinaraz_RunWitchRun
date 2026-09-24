using UnityEngine;

public enum PowerUpType
{
    MagicBroom,
    Invincibility,
    ExtraLife
}

public class PowerUp : Pickup
{
    [SerializeField] private PowerUpType type = PowerUpType.MagicBroom;

    public PowerUpType Type => type;

    protected override void OnCollected(PlayerController player)
    {
        switch (type)
        {
            case PowerUpType.MagicBroom:
                player.ActivateBroom();
                break;
            case PowerUpType.Invincibility:
                player.ActivateInvincibility();
                break;
            case PowerUpType.ExtraLife:
                player.AddExtraLife();
                break;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayPowerUp();
        }
    }
}
