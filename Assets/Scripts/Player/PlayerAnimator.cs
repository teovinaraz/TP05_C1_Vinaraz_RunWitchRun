using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private Sprite deadSprite;
    [SerializeField] private float runFramesPerSecond = 9f;
    [Header("Hover de la escoba")]
    [SerializeField] private Transform broomTransform;
    [SerializeField] private float broomBobAmplitude = 0.06f;
    [SerializeField] private float broomBobSpeed = 12f;

    private float frameTimer;
    private Vector3 broomStart;

    private void Awake()
    {
        if (broomTransform != null)
        {
            broomStart = broomTransform.localPosition;
        }
    }

    private void Update()
    {
        if (player.IsDead)
        {
            spriteRenderer.sprite = deadSprite;
            return;
        }

        if (player.IsBroomActive || !player.IsGrounded)
        {
            spriteRenderer.sprite = jumpSprite;
        }
        else
        {
            float speedRatio = player.CurrentSpeed / player.Data.initialSpeed;
            frameTimer += Time.deltaTime * runFramesPerSecond * speedRatio;
            spriteRenderer.sprite = runFrames[Mathf.FloorToInt(frameTimer) % runFrames.Length];
        }

        if (broomTransform != null && player.IsBroomActive)
        {
            broomTransform.localPosition = broomStart + Vector3.up * Mathf.Sin(Time.time * broomBobSpeed) * broomBobAmplitude;
        }
    }
}
