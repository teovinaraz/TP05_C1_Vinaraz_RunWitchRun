using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class SpawnEntry
{
    public string name = "Obstacle";
    public GameObject prefab;
    public float width = 1f;
    public float height = 1.2f;
    [Min(0f)] public float weight = 1f;
    public float unlockAfterSeconds = 0f;
}

public class EndlessSpawner : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerController player;

    [Header("Posicion")]
    [SerializeField] private float spawnX = 11f;
    [SerializeField] private float groundY = -2.5f;

    [Header("Prefabs")]
    [SerializeField] private List<SpawnEntry> obstacles = new List<SpawnEntry>();
    [SerializeField] private GameObject gemPrefab;
    [SerializeField] private GameObject broomPrefab;

    [Header("Separacion entre obstaculos (distancia borde a borde)")]
    [SerializeField] private float minGap = 4f;
    [SerializeField] private float maxGapStart = 16f;
    [SerializeField] private float maxGapEnd = 9f;
    [SerializeField] private float reactionTime = 0.25f;
    [SerializeField] private float jumpSafety = 1f;
    [SerializeField] private float extraRange = 3f;
    [SerializeField] private float initialDelayDistance = 20f;

    [Header("Dificultad progresiva")]
    [SerializeField] private float difficultyRampSeconds = 120f;

    [Header("Grupos de obstaculos (dos juntos, saltables de un solo salto)")]
    [SerializeField, Range(0f, 1f)] private float clusterChanceStart = 0.05f;
    [SerializeField, Range(0f, 1f)] private float clusterChanceEnd = 0.35f;
    [SerializeField] private float clusterGapMin = 0.6f;
    [SerializeField] private float clusterGapMax = 1.2f;
    [SerializeField, Range(0.3f, 1f)] private float clusterSpanUsage = 0.6f;

    [Header("Moon Gems")]
    [SerializeField, Range(0f, 1f)] private float gemChance = 0.65f;
    [SerializeField] private float gemSpacing = 1f;
    [SerializeField] private float gemRowHeight = 0.9f;
    [SerializeField] private float gemArcHeight = 2.9f;
    [SerializeField] private float gemArcHalfWidth = 1.8f;
    [SerializeField] private int gemsPerArc = 5;

    [Header("Escoba Magica")]
    [SerializeField] private float broomFirstDistance = 90f;
    [SerializeField] private float broomMinDistance = 140f;
    [SerializeField] private float broomMaxDistance = 240f;
    [SerializeField] private float broomHeight = 1.1f;

    private ObjectPool pool;
    private SpawnEntry pending;
    private float distanceUntilNext;
    private float distanceUntilBroom;

    private float Difficulty01
    {
        get
        {
            float time = GameManager.Instance != null ? GameManager.Instance.PlayTime : 0f;
            return Mathf.Clamp01(time / difficultyRampSeconds);
        }
    }

    private void Start()
    {
        pool = new ObjectPool(transform);
        distanceUntilNext = initialDelayDistance;
        distanceUntilBroom = broomFirstDistance;
        pending = PickEntry();
        ValidateEntries();
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying || pending == null)
        {
            return;
        }

        float travelled = gm.WorldSpeed * Time.deltaTime;
        distanceUntilNext -= travelled;
        distanceUntilBroom -= travelled;

        if (distanceUntilNext <= 0f)
        {
            SpawnNext();
        }
    }

    private void SpawnNext()
    {
        float overshoot = -distanceUntilNext;
        float x = spawnX - overshoot;

        SpawnEntry current = pending;
        Spawn(current.prefab, x, groundY);
        float lastX = x;
        float lastHalfWidth = current.width * 0.5f;

        if (TryPickCluster(current, out SpawnEntry second, out float clusterGap))
        {
            float x2 = x + current.width * 0.5f + clusterGap + second.width * 0.5f;
            Spawn(second.prefab, x2, groundY);
            lastX = x2;
            lastHalfWidth = second.width * 0.5f;
        }

        pending = PickEntry();
        float gap = ComputeGap();
        float nextCenterX = lastX + lastHalfWidth + gap + pending.width * 0.5f;
        distanceUntilNext = nextCenterX - spawnX;

        float gapStartX = lastX + lastHalfWidth;
        float gapEndX = nextCenterX - pending.width * 0.5f;
        SpawnCollectibles(gapStartX, gapEndX, nextCenterX);
    }

    private void SpawnCollectibles(float gapStartX, float gapEndX, float nextObstacleX)
    {
        bool broomDue = distanceUntilBroom <= 0f && broomPrefab != null && player != null && !player.IsBroomActive;
        if (broomDue)
        {
            float mid = (gapStartX + gapEndX) * 0.5f;
            Spawn(broomPrefab, mid, groundY + broomHeight);
            distanceUntilBroom = Random.Range(broomMinDistance, broomMaxDistance);
            return;
        }

        if (gemPrefab == null || Random.value > gemChance)
        {
            return;
        }

        float gapLength = gapEndX - gapStartX;
        bool rowFits = gapLength >= (gemsPerArc - 1) * gemSpacing + 3f;
        if (rowFits && Random.value < 0.5f)
        {
            int count = Random.Range(3, gemsPerArc + 1);
            float width = (count - 1) * gemSpacing;
            float start = (gapStartX + gapEndX) * 0.5f - width * 0.5f;
            for (int i = 0; i < count; i++)
            {
                Spawn(gemPrefab, start + i * gemSpacing, groundY + gemRowHeight);
            }
        }
        else
        {
            for (int i = 0; i < gemsPerArc; i++)
            {
                float u = gemsPerArc > 1 ? (i / (float)(gemsPerArc - 1)) * 2f - 1f : 0f;
                float gx = nextObstacleX + u * gemArcHalfWidth;
                float gy = groundY + Mathf.Lerp(gemRowHeight, gemArcHeight, 1f - u * u);
                Spawn(gemPrefab, gx, gy);
            }
        }
    }

    private void Spawn(GameObject prefab, float x, float y)
    {
        GameObject go = pool.Get(prefab, new Vector3(x, y, 0f));
        ScrollingObject scrolling = go.GetComponent<ScrollingObject>();
        if (scrolling != null)
        {
            scrolling.Init(pool.Release);
        }
    }

    private float ComputeGap()
    {
        PlayerData data = player.Data;
        // calculo con la velocidad de la escoba para que siempre se pueda esquivar
        float worstSpeed = player.BaseSpeed * player.MaxSpeedMultiplier;
        float safe = data.JumpDistance(worstSpeed) * jumpSafety + worstSpeed * reactionTime;
        safe = Mathf.Max(safe, minGap);

        float upper = Mathf.Lerp(maxGapStart, maxGapEnd, Difficulty01);
        upper = Mathf.Max(upper, safe + extraRange);
        return Random.Range(safe, upper);
    }

    private bool TryPickCluster(SpawnEntry first, out SpawnEntry second, out float gap)
    {
        second = null;
        gap = 0f;

        float chance = Mathf.Lerp(clusterChanceStart, clusterChanceEnd, Difficulty01);
        if (Random.value > chance)
        {
            return false;
        }

        SpawnEntry candidate = PickEntry();
        float candidateGap = Random.Range(clusterGapMin, clusterGapMax);
        float span = first.width + candidateGap + candidate.width;
        float tallest = Mathf.Max(first.height, candidate.height);
        float clearable = player.Data.ClearableSpan(player.BaseSpeed, tallest) * clusterSpanUsage;

        // si no entran en un salto, no se hace el grupo
        if (span > clearable)
        {
            return false;
        }

        second = candidate;
        gap = candidateGap;
        return true;
    }

    private SpawnEntry PickEntry()
    {
        float time = GameManager.Instance != null ? GameManager.Instance.PlayTime : 0f;
        float total = 0f;
        foreach (SpawnEntry e in obstacles)
        {
            if (e.prefab != null && e.unlockAfterSeconds <= time)
            {
                total += e.weight;
            }
        }

        if (total <= 0f)
        {
            return obstacles.Count > 0 ? obstacles[0] : null;
        }

        float roll = Random.value * total;
        foreach (SpawnEntry e in obstacles)
        {
            if (e.prefab == null || e.unlockAfterSeconds > time)
            {
                continue;
            }
            roll -= e.weight;
            if (roll <= 0f)
            {
                return e;
            }
        }
        return obstacles[0];
    }

    private void ValidateEntries()
    {
        if (player == null)
        {
            return;
        }
        foreach (SpawnEntry e in obstacles)
        {
            float clearable = player.Data.ClearableSpan(player.Data.initialSpeed, e.height);
            if (e.width > clearable)
            {
                Debug.LogWarning($"[EndlessSpawner] '{e.name}' (ancho {e.width}, alto {e.height}) puede ser dificil o imposible de saltar a la velocidad inicial (zona saltable: {clearable:0.00}). Ajusta PlayerData o el obstaculo.");
            }
        }
    }
}
