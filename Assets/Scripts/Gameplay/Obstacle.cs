using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Obstacle : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float swayDegrees = 3f;
    [SerializeField] private float swaySpeed = 1.4f;

    private float phase;

    private void OnEnable()
    {
        phase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        if (visualRoot == null)
        {
            return;
        }
        float angle = Mathf.Sin(Time.time * swaySpeed + phase) * swayDegrees;
        visualRoot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
