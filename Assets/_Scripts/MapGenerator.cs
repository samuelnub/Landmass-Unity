using UnityEngine;
using System.Collections;

// Oh ok, so structs are pretty much how you'd expect them to be declared
[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}

[System.Serializable]
public class MapGenerator : MonoBehaviour {

    public enum DrawMode
    {
        NOISEMAP,
        COLORMAP,
        MESH
    }
    public DrawMode drawMode;
    
    public const int mapChunkSize = 241; // -1 so its actually 240

    [Range(0,6)]
    public int levelOfDetail;
    public float noiseScale;

    public int octaves;

    // Should be in range of 0 to 1
    [Range(0,1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve; // Cool variable that you can tweak in the editor

    public bool autoUpdate;

    public TerrainType[] regions;
    
    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, this.seed, this.noiseScale, this.octaves, this.persistance, this.lacunarity, this.offset);
        
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for(int y = 0; y < mapChunkSize; y++)
        {
            for(int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for(int c = 0; c < this.regions.Length; c++)
                {
                    if(currentHeight <= regions[c].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[c].color;
                        break;
                    }
                }
            }
        }

        // Get a ref to the map
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (this.drawMode == DrawMode.NOISEMAP)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        }
        else if (this.drawMode == DrawMode.COLORMAP)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        }
        else if(this.drawMode == DrawMode.MESH)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, this.meshHeightMultiplier, this.meshHeightCurve, this.levelOfDetail), TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
        }
    }

    // Handy function
    void OnValidate()
    {
        if(lacunarity < 1)
        {
            lacunarity = 1;
        }
        if(octaves < 0)
        {
            octaves = 0;
        }
    }
}
