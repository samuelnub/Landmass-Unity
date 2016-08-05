using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For dictionary (a map for your puny c++ brain)

public class EndlessTerrain : MonoBehaviour {

    // Nested class, whod'a thunk
    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;

            this.position = coord * size;
            this.bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0.0f, position.y);

            this.meshObject = new GameObject("Terrain Chunk");
            this.meshRenderer = meshObject.AddComponent<MeshRenderer>();
            this.meshFilter = meshObject.AddComponent<MeshFilter>();
            this.meshObject.transform.position = positionV3;
            this.meshObject.transform.parent = parent;
            this.meshRenderer.material = material;

            this.lodMeshes = new LODMesh[detailLevels.Length];
            for (int c = 0; c < detailLevels.Length; c++)
            {
                this.lodMeshes[c] = new LODMesh(detailLevels[c].lod);
            }

            this.SetVisible(false); // Start out disabled
            mapGenerator.RequestMapData(this.OnMapDataReceived); // Actions<> are cool
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            this.mapDataReceived = true;
        }
        
        public void UpdateTerrainChunk()
        {
            if (this.mapDataReceived)
            {
                // See if player is within render range, if not, deactivate this meshObject
                // oh cool, nested classes can use its parent classes' stuff
                float viewerDistFromNearestEdge = Mathf.Sqrt(this.bounds.SqrDistance(viewerPos));
                bool visible = viewerDistFromNearestEdge <= maxViewDist;

                if (visible)
                {
                    int lodIndex = 0;
                    for (int c = 0; c < this.detailLevels.Length - 1; c++)
                    {
                        if (viewerDistFromNearestEdge > this.detailLevels[c].visibleDistThreshold)
                        {
                            lodIndex = c + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (lodIndex != this.previousLODIndex)
                    {
                        LODMesh lodMesh = this.lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            this.previousLODIndex = lodIndex;
                            this.meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(this.mapData);
                        }
                    }
                }

                this.SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            this.meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }
    
    // Each terrainchunk will have an array of these
    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;

        public LODMesh(int lod)
        {
            this.lod = lod;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            this.mesh = meshData.CreateMesh();
            this.hasMesh = true;
        }

        public void RequestMesh(MapData mapData)
        {
            this.hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, this.OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistThreshold;
    }
    
    public LODInfo[] detailLevels;
    public static float maxViewDist;
    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPos;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDist;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDist = this.detailLevels[this.detailLevels.Length - 1].visibleDistThreshold;

        this.chunkSize = MapGenerator.mapChunkSize - 1;
        this.chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);

        // The middle chunk's centre is at 0,0, so the chunk of the left's centre is at -240,0, etc... but the number is -1,0
    }

    void Update()
    {
        viewerPos = new Vector2(viewer.position.x, viewer.position.z);
        this.UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {
        for(int c = 0; c < this.terrainChunksVisibleLastUpdate.Count; c++)
        {
            this.terrainChunksVisibleLastUpdate[c].SetVisible(false);
        }
        this.terrainChunksVisibleLastUpdate.Clear();
        
        int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x / this.chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPos.y / this.chunkSize);

        for(int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++)
        {
            for(int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if(terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    if(terrainChunkDictionary[viewedChunkCoord].IsVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDictionary[viewedChunkCoord]);
                    }
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, this.detailLevels, this.transform, this.mapMaterial));
                }
            }
        }
    }
}
