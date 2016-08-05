using UnityEngine;
using System.Collections;
using System; // Actions
using System.Threading; // duh
using System.Collections.Generic; // For queue

// Oh ok, so structs are pretty much how you'd expect them to be declared
[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}

// Threading: on start() within EndlessTerrain, it will request for map data from this class, when this guy's done, commit it to EndlessTerrain to render the chunks (using callbacks and queues, this guy queues up its action onto the main thread, and every Update() here, if the queue is finally 0, call EndlessTerrain that it's done with the stuff
[System.Serializable]
public class MapGenerator : MonoBehaviour {

    struct MapThreadInfo<T> // Templating, nice one
    {
        // read only vars, as they should be immutable, but they're public structs, so lol
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

    public enum DrawMode
    {
        NOISEMAP,
        COLORMAP,
        MESH
    }
    public DrawMode drawMode;

    public Noise.NormalizeMode normalizeMode;

    public const int mapChunkSize = 241; // -1 so its actually 240

    [Range(0, 6)]
    public int editorPreviewLOD;
    public float noiseScale;

    public int octaves;

    // Should be in range of 0 to 1
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve; // Cool variable that you can tweak in the editor

    public bool autoUpdate;

    public TerrainType[] regions;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor()
    {
        MapData mapData = this.GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (this.drawMode == DrawMode.NOISEMAP)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (this.drawMode == DrawMode.COLORMAP)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (this.drawMode == DrawMode.MESH)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, this.meshHeightMultiplier, this.meshHeightCurve, this.editorPreviewLOD), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
    }

    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate // dele my gate
        {
            this.MapDataThread(centre, callback);
        };
        new Thread(threadStart).Start(); // Boom, new thread being used for this now, methods called while this thread is in use will be run on the thread
    }

    void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = this.GenerateMapData(centre);
        // Lock this mapdatathreadinfoqueue so that no other thread can intervene while its doing its business
        lock (this.mapDataThreadInfoQueue)
        {
            this.mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData)); // Enqueue - add
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };
        new Thread(threadStart).Start(); // Boom yet again, for mesh data now
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, this.meshHeightMultiplier, this.meshHeightCurve, lod);
        lock(this.meshDataThreadInfoQueue)
        {
            this.meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update()
    {
        if(this.mapDataThreadInfoQueue.Count > 0)
        {
            for(int c = 0; c < this.mapDataThreadInfoQueue.Count; c++)
            {
                MapThreadInfo<MapData> threadInfo = this.mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter); // callbacks are a mystifying thing
            }
        }

        if(this.meshDataThreadInfoQueue.Count > 0)
        {
            for(int c = 0; c < this.meshDataThreadInfoQueue.Count; c++)
            {
                MapThreadInfo<MeshData> threadInfo = this.meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData(Vector2 centre)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, this.seed, this.noiseScale, this.octaves, this.persistance, this.lacunarity, centre + this.offset, this.normalizeMode);
        
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for(int y = 0; y < mapChunkSize; y++)
        {
            for(int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for(int c = 0; c < this.regions.Length; c++)
                {
                    if(currentHeight >= regions[c].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[c].color;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        return new MapData(noiseMap, colorMap);
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
