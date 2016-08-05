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
            this.SetVisible(false); // Start out disabled

            this.lodMeshes = new LODMesh[detailLevels.Length];
            for (int c = 0; c < detailLevels.Length; c++)
            {
                this.lodMeshes[c] = new LODMesh(detailLevels[c].lod, this.UpdateTerrainChunk);
            }
            
            mapGenerator.RequestMapData(this.position, this.OnMapDataReceived); // Actions<> are cool
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            this.mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            this.meshRenderer.material.mainTexture = texture;

            this.UpdateTerrainChunk();
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

                    terrainChunksVisibleLastUpdate.Add(this);
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
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            this.mesh = meshData.CreateMesh();
            this.hasMesh = true;
            this.updateCallback();
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

    // Nice naming, wubber
    const float viewerMoveThresholdForChunkUpdate = 25.0f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public LODInfo[] detailLevels;
    public static float maxViewDist;
    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPos;
    Vector2 viewerPosOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDist;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDist = this.detailLevels[this.detailLevels.Length - 1].visibleDistThreshold;

        this.chunkSize = MapGenerator.mapChunkSize - 1;
        this.chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);

        this.UpdateVisibleChunks();
    }

    void Update()
    {
        viewerPos = new Vector2(viewer.position.x, viewer.position.z);
        if((this.viewerPosOld - viewerPos).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            this.viewerPosOld = viewerPos;
            this.UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        for(int c = 0; c < terrainChunksVisibleLastUpdate.Count; c++)
        {
            terrainChunksVisibleLastUpdate[c].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();
        
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
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, this.detailLevels, this.transform, this.mapMaterial));
                }
            }
        }
    }
}
