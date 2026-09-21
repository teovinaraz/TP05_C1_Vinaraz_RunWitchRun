using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleWorldScroll : MonoBehaviour
{
    [SerializeField] private float factor = 1f;
    [SerializeField] private float idleSpeed = 0f;

    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);
    }

    private void Update()
    {
        // la bruja no avanza, asi que las particulas se mueven hacia atras con el mundo
        float speed = GameManager.Instance != null ? GameManager.Instance.WorldSpeed : idleSpeed;
        var velocity = ps.velocityOverLifetime;
        velocity.x = new ParticleSystem.MinMaxCurve(-speed * factor);
    }
}
