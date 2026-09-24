using UnityEngine;

[CreateAssetMenu(fileName = "BiomeData", menuName = "Run Witch Run/Biome Data")]
public class BiomeData : ScriptableObject
{
    [System.Serializable]
    public class Layer
    {
        public string layerName;
        public Sprite sprite;
        public float speedFactor = 1f;
        public float ambientSpeed;
    }

    public Layer[] layers;
}
