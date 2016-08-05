using UnityEngine;
using System.Collections;

public static class Noise {
    
    public static float[,] GenerateNoiseMap(int w, int h, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
    {
        float[,] noiseMap = new float[w, h];

        System.Random module = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int c = 0; c < octaves; c++)
        {
            float offsetX = module.Next(-100000, 100000) + offset.x;
            float offsetY = module.Next(-100000, 100000) + offset.y;
            octaveOffsets[c] = new Vector2(offsetX,offsetY);
        }

        if(scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = w / 2.0f;
        float halfHeight = h / 2.0f;

        for(int y = 0; y < h; y++)
        {
            for(int x = 0; x < w; x++)
            {
                float amplitude = 1.0f;
                float frequency = 1.0f;
                float noiseHeight = 0.0f;

                for (int c = 0; c < octaves; c++)
                {
                    float sampleX = (x-halfWidth) / scale * frequency + octaveOffsets[c].x;
                    float sampleY = (y-halfHeight) / scale * frequency + octaveOffsets[c].y;

                    // * 2 - 1 to make it so theres values below 0, but then we'd have to normalise it when returning the noiseMap
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                // Do this so we can "compress" or normalize it when returning
                if(noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if(noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }
                

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // If noise map val == min noise height, itll return 0, to 1... and all the inbetweens, normalizing it
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }
        return noiseMap;
    }
}
