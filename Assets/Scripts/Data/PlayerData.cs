using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Run Witch Run/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("Carrera")]
    public float initialSpeed = 7f;
    public float maxSpeed = 13f;
    public float speedIncreasePerSecond = 0.08f;

    [Header("Salto")]
    public float jumpForce = 13f;
    public float gravityScale = 3.5f;
    public float fallGravityMultiplier = 1.4f;
    public float lowJumpGravityMultiplier = 2.2f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.12f;

    [Header("Hitbox")]
    public Vector2 hitboxSize = new Vector2(0.7f, 1.5f);
    public Vector2 hitboxOffset = new Vector2(0f, 0.8f);

    [Header("Deteccion de suelo")]
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.2f);

    [Header("Muerte")]
    public float deathBounce = 6f;

    [Header("Escoba Magica")]
    public float broomDuration = 6f;
    public float broomSpeedMultiplier = 1.5f;
    public float speedBlendRate = 2.5f;

    [Header("Invencibilidad")]
    public float invincibilityDuration = 5f;
    public float extraLifeGraceTime = 1.5f;

    [Header("Puntaje")]
    public int moonGemPoints = 10;
    public float survivalPointsPerSecond = 10f;

    public float EstimatedAirTime
    {
        get
        {
            float g = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
            float rise = jumpForce / g;
            float height = jumpForce * jumpForce / (2f * g);
            float fall = Mathf.Sqrt(2f * height / (g * fallGravityMultiplier));
            return rise + fall;
        }
    }

    public float ClearableSpan(float speed, float obstacleHeight)
    {
        float g = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
        float apex = jumpForce * jumpForce / (2f * g);
        float margin = apex - obstacleHeight;
        if (margin <= 0.1f)
        {
            return 0f;
        }
        float timeAbove = Mathf.Sqrt(2f * margin / g) + Mathf.Sqrt(2f * margin / (g * fallGravityMultiplier));
        return Mathf.Max(0f, speed * timeAbove - hitboxSize.x);
    }

    public float JumpDistance(float speed)
    {
        return speed * EstimatedAirTime;
    }
}
