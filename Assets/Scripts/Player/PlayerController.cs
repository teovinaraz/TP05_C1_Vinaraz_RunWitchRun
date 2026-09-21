using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Datos")]
    [SerializeField] private PlayerData data;

    [Header("Referencias")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private GameObject broomVisual;

    [Header("Efectos")]
    [SerializeField] private ParticleSystem runDust;
    [SerializeField] private ParticleSystem jumpBurst;
    [SerializeField] private ParticleSystem landBurst;
    [SerializeField] private ParticleSystem magicAura;
    [SerializeField] private ParticleSystem broomTrail;
    [SerializeField] private int jumpBurstCount = 8;
    [SerializeField] private int landBurstCount = 10;
    [SerializeField] private float auraRateNormal = 6f;
    [SerializeField] private float auraRateBroom = 40f;
    [SerializeField] private float broomTrailRate = 90f;
    [SerializeField] private float runDustRate = 14f;
    [SerializeField] private float minAirTimeForLandEffect = 0.15f;

    private Rigidbody2D rb;
    private readonly Collider2D[] groundHits = new Collider2D[8];
    private ContactFilter2D groundFilter;

    private float baseSpeed;
    private float speedMultiplier = 1f;
    private float broomTimeLeft;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float airTime;
    private bool jumpHeld;
    private bool isGrounded;
    private bool wasGrounded;
    private bool dustOn;

    public PlayerData Data => data;
    public bool IsGrounded => isGrounded;
    public bool IsDead { get; private set; }
    public bool IsBroomActive { get; private set; }
    public float BaseSpeed => baseSpeed;
    public float CurrentSpeed => baseSpeed * speedMultiplier;
    public float BroomTimeLeft => broomTimeLeft;
    public float BroomTimeNormalized => data.broomDuration > 0f ? Mathf.Clamp01(broomTimeLeft / data.broomDuration) : 0f;
    public float MaxSpeedMultiplier => data.broomSpeedMultiplier;

    public event Action BroomStarted;
    public event Action BroomEnded;

    private Vector2 Velocity
    {
        get
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }
        set
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = value;
#else
            rb.velocity = value;
#endif
        }
    }

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError("PlayerController: falta asignar PlayerData", this);
            enabled = false;
            return;
        }

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = data.gravityScale;
        rb.freezeRotation = true;
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.size = data.hitboxSize;
        box.offset = data.hitboxOffset;
        groundFilter = new ContactFilter2D();
        groundFilter = ContactFilter2D.noFilter;
        baseSpeed = data.initialSpeed;
        SetBroomVisuals(false);
    }

    private void Start()
    {
        if (magicAura != null)
        {
            var emission = magicAura.emission;
            emission.rateOverTime = auraRateNormal;
        }
    }

    private void Update()
    {
        if (IsDead || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
        {
            return;
        }

        float dt = Time.deltaTime;
        UpdateSpeed(dt);
        UpdateBroom(dt);
        ReadInput(dt);
        TryJump();
        UpdateLanding(dt);
        UpdateRunDust();
    }

    private void FixedUpdate()
    {
        if (IsDead)
        {
            return;
        }

        isGrounded = CheckGround();

        float scale = data.gravityScale;
        // cae mas rapido de lo que sube y el salto se acorta si se suelta el boton
        if (Velocity.y < 0f)
        {
            scale *= data.fallGravityMultiplier;
        }
        else if (Velocity.y > 0f && !jumpHeld)
        {
            scale *= data.lowJumpGravityMultiplier;
        }
        rb.gravityScale = scale;
    }

    private void UpdateSpeed(float dt)
    {
        baseSpeed = Mathf.MoveTowards(baseSpeed, data.maxSpeed, data.speedIncreasePerSecond * dt);
        float target = IsBroomActive ? data.broomSpeedMultiplier : 1f;
        speedMultiplier = Mathf.MoveTowards(speedMultiplier, target, data.speedBlendRate * dt);
    }

    private void ReadInput(float dt)
    {
        bool pressed = GameInput.JumpPressed;
        jumpHeld = GameInput.JumpHeld;

        jumpBufferCounter = pressed ? data.jumpBufferTime : jumpBufferCounter - dt;
        coyoteCounter = isGrounded ? data.coyoteTime : coyoteCounter - dt;
    }

    private void TryJump()
    {
        // solo salta si toca el suelo (o lo toco hace un instante)
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            Jump();
        }
    }

    private void Jump()
    {
        Velocity = new Vector2(0f, data.jumpForce);
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        isGrounded = false;
        wasGrounded = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayJump();
        }
        if (jumpBurst != null)
        {
            jumpBurst.Emit(jumpBurstCount);
        }
    }

    private bool CheckGround()
    {
        int count = Physics2D.OverlapBox(groundCheck.position, data.groundCheckSize, 0f, groundFilter, groundHits);
        for (int i = 0; i < count; i++)
        {
            if (groundHits[i] != null && groundHits[i].GetComponent<GroundSurface>() != null)
            {
                // tiene que estar cayendo o quieta, si no cuenta como suelo justo al saltar
                return Velocity.y <= 0.05f;
            }
        }
        return false;
    }

    private void UpdateLanding(float dt)
    {
        if (!isGrounded)
        {
            airTime += dt;
        }
        else
        {
            if (!wasGrounded && airTime > minAirTimeForLandEffect)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayLand();
                }
                if (landBurst != null)
                {
                    landBurst.Emit(landBurstCount);
                }
            }
            airTime = 0f;
        }
        wasGrounded = isGrounded;
    }

    private void UpdateRunDust()
    {
        if (runDust == null || dustOn == isGrounded)
        {
            return;
        }
        dustOn = isGrounded;
        var emission = runDust.emission;
        emission.rateOverTime = dustOn ? runDustRate : 0f;
    }

    public void ActivateBroom()
    {
        broomTimeLeft = data.broomDuration;
        if (IsBroomActive)
        {
            return;
        }
        IsBroomActive = true;
        SetBroomVisuals(true);
        BroomStarted?.Invoke();
    }

    private void UpdateBroom(float dt)
    {
        if (!IsBroomActive)
        {
            return;
        }
        broomTimeLeft -= dt;
        if (broomTimeLeft <= 0f)
        {
            broomTimeLeft = 0f;
            IsBroomActive = false;
            SetBroomVisuals(false);
            BroomEnded?.Invoke();
        }
    }

    private void SetBroomVisuals(bool active)
    {
        if (broomVisual != null)
        {
            broomVisual.SetActive(active);
        }
        if (broomTrail != null)
        {
            var trailEmission = broomTrail.emission;
            trailEmission.rateOverTime = active ? broomTrailRate : 0f;
        }
        if (magicAura != null)
        {
            var emission = magicAura.emission;
            emission.rateOverTime = active ? auraRateBroom : auraRateNormal;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsDead || GameManager.Instance == null || !GameManager.Instance.IsPlaying)
        {
            return;
        }
        if (other.GetComponent<Obstacle>() != null)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;
        IsBroomActive = false;
        SetBroomVisuals(false);
        if (runDust != null)
        {
            var emission = runDust.emission;
            emission.rateOverTime = 0f;
        }
        rb.gravityScale = data.gravityScale;
        Velocity = new Vector2(0f, data.deathBounce);
        GameManager.Instance.EndGame();
    }
}
