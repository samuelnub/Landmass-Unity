using UnityEngine;
using System.Collections;

public static class Noise {
    
    public enum NormalizeMode
    {
        LOCAL, // One-off
        GLOBAL // Chunk based
    }
    
    public static float[,] GenerateNoiseMap(int w, int h, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        float[,] noiseMap = new float[w, h];

        System.Random module = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0.0f;
        float amplitude = 1.0f;
        float frequency = 1.0f;

        for (int c = 0; c < octaves; c++)
        {
            float offsetX = module.Next(-100000, 100000) + offset.x;
            float offsetY = module.Next(-100000, 100000) - offset.y;
            octaveOffsets[c] = new Vector2(offsetX,offsetY);
            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if(scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = w / 2.0f;
        float halfHeight = h / 2.0f;

        for(int y = 0; y < h; y++)
        {
            for(int x = 0; x < w; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0.0f;

                for (int c = 0; c < octaves; c++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[c].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[c].y) / scale * frequency;

                    // * 2 - 1 to make it so theres values below 0, but then we'd have to normalise it when returning the noiseMap
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                // Do this so we can "compress" or normalize it when returning
                if(noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                else if(noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // If noise map val == min noise height, itll return 0, to 1... and all the inbetweens, normalizing it
                if (normalizeMode == NormalizeMode.LOCAL)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseMap[x,y] + 1) / (maxPossibleHeight);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight,0,int.MaxValue);
                }

            }
        }
        return noiseMap;
    }
}
