using UnityEngine;
using System.Collections;

public class MeshData
{
    public Vector3[] vertices;
    public int[] indices;
    public Vector2[] uvs;

    int triangleIndex;

    // Constructor
    public MeshData(int meshW, int meshH)
    {
        this.vertices = new Vector3[meshW * meshH];
        this.indices = new int[(meshW - 1) * (meshH - 1) * 6];
        this.uvs = new Vector2[meshW * meshH];
    }

    public void AddTriangle(int a, int b, int c)
    {
        this.indices[triangleIndex] = a;
        this.indices[triangleIndex + 1] = b;
        this.indices[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = this.vertices;
        mesh.triangles = this.indices;
        mesh.uv = this.uvs;
        mesh.RecalculateNormals(); // Handy
        return mesh;
    }
}

public static class MeshGenerator {

    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        float topLeftX = (width - 1) / -2.0f;
        float topLeftZ = (height - 1) / 2.0f;

        int meshSimplificationIncrement = (levelOfDetail == 0)? 1 : levelOfDetail * 2;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        // For level of detail (LODs), you can selectively skip mesh generation, eg, increment y by 2, so it'll generate half in each dimension, 4x total improvement, and you can also skip more and more for lower details, 4, 8, etc... must be a factor of width or height - 1 (0 indexing) (no remainder). The sweet spot size of this would be a width and height of 241, because 240 is divisible by 1,2,4,8,12
        for(int y = 0; y < height; y += meshSimplificationIncrement)
        {
            for(int x = 0; x < width; x += meshSimplificationIncrement)
            {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y); // Evaluate based on animation curve

                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                if(x < width-1 && y < height-1)
                {
                    meshData.AddTriangle(
                        vertexIndex,
                        vertexIndex + verticesPerLine + 1,
                        vertexIndex + verticesPerLine
                        );

                    meshData.AddTriangle(
                        vertexIndex + verticesPerLine + 1,
                        vertexIndex,
                        vertexIndex + 1
                        );
                }
                vertexIndex++;
            }
        }
        return meshData;
    }
}