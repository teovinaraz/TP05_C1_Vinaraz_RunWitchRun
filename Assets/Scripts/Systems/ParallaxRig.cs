using UnityEngine;

public class ParallaxRig : MonoBehaviour
{
    [SerializeField] private BiomeData biome;
    [SerializeField] private Transform[] layerRoots;

    private void Awake()
    {
        if (biome != null)
        {
            ApplyBiome(biome);
        }
    }

    public void ApplyBiome(BiomeData newBiome)
    {
        biome = newBiome;
        if (biome == null || biome.layers == null)
        {
            return;
        }

        for (int i = 0; i < layerRoots.Length && i < biome.layers.Length; i++)
        {
            Transform layerRoot = layerRoots[i];
            if (layerRoot == null)
            {
                continue;
            }

            BiomeData.Layer def = biome.layers[i];
            foreach (SpriteRenderer sr in layerRoot.GetComponentsInChildren<SpriteRenderer>())
            {
                sr.sprite = def.sprite;
            }

            ParallaxLayer parallax = layerRoot.GetComponent<ParallaxLayer>();
            if (parallax != null)
            {
                parallax.Configure(def.speedFactor, def.ambientSpeed);
            }
        }
    }
}
