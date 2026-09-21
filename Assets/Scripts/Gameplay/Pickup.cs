using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class Pickup : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private ParticleSystem collectBurst;
    [SerializeField] private int burstCount = 14;
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float despawnDelay = 0.7f;

    private Collider2D pickupCollider;
    private Vector3 visualStart;
    private float phase;
    private bool collected;

    protected virtual void Awake()
    {
        pickupCollider = GetComponent<Collider2D>();
        if (visualRoot != null)
        {
            visualStart = visualRoot.localPosition;
        }
    }

    protected virtual void OnEnable()
    {
        collected = false;
        phase = Random.value * Mathf.PI * 2f;
        pickupCollider.enabled = true;
        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(true);
            visualRoot.localPosition = visualStart;
        }
    }

    protected virtual void OnDisable()
    {
        CancelInvoke();
    }

    private void Update()
    {
        if (collected || visualRoot == null)
        {
            return;
        }
        visualRoot.localPosition = visualStart + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobAmplitude);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDead)
        {
            return;
        }

        collected = true;
        pickupCollider.enabled = false;
        if (visualRoot != null)
        {
            visualRoot.gameObject.SetActive(false);
        }
        if (collectBurst != null)
        {
            collectBurst.Emit(burstCount);
        }

        OnCollected(player);
        Invoke(nameof(Despawn), despawnDelay);
    }

    private void Despawn()
    {
        ScrollingObject scrolling = GetComponent<ScrollingObject>();
        if (scrolling != null)
        {
            scrolling.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected abstract void OnCollected(PlayerController player);
}
