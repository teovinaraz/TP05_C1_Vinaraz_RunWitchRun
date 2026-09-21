using System.Linq;
using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private float speedFactor = 0.2f;
    [SerializeField] private float idleSpeed = 3f;
    [SerializeField] private float ambientSpeed = 0f;
    [SerializeField] private float leftLimit = -12f;

    private Transform[] tiles;
    private float tileWidth;

    private void Awake()
    {
        tiles = GetComponentsInChildren<SpriteRenderer>()
            .Select(r => r.transform)
            .OrderBy(t => t.position.x)
            .ToArray();
        tileWidth = tiles[0].GetComponent<SpriteRenderer>().bounds.size.x;
    }

    private void Update()
    {
        float world = GameManager.Instance != null ? GameManager.Instance.WorldSpeed : idleSpeed;
        float delta = (world * speedFactor + ambientSpeed) * Time.deltaTime;
        if (delta <= 0f)
        {
            return;
        }

        float total = tileWidth * tiles.Length;
        foreach (Transform tile in tiles)
        {
            Vector3 pos = tile.position;
            pos.x -= delta;
            // cuando un tile sale por la izquierda pasa al final de la fila
            while (pos.x + tileWidth * 0.5f < leftLimit)
            {
                pos.x += total;
            }
            tile.position = pos;
        }
    }
}
