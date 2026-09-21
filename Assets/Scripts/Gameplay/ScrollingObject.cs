using System;
using UnityEngine;

public class ScrollingObject : MonoBehaviour
{
    [SerializeField] private float despawnX = -14f;

    private Action<GameObject> releaseCallback;

    public void Init(Action<GameObject> release)
    {
        releaseCallback = release;
    }

    private void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        transform.position += Vector3.left * (GameManager.Instance.WorldSpeed * Time.deltaTime);
        if (transform.position.x < despawnX)
        {
            Despawn();
        }
    }

    public void Despawn()
    {
        if (releaseCallback != null)
        {
            releaseCallback(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
