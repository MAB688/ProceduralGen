using UnityEngine;

// Perlin values are always between 0.0 and 1.0

public static class Noise
{
    public enum NormalizeMode {Local, Global};
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float noiseScale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode) {
        // Upscale the noise map so the color and mesh maps match in dimension
        // In MapGenerator: Alters noise map and mesh map, does not affect color map
        mapWidth++;
        mapHeight++;

        // If each row in the array will be the same length, use this syntax
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random rng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        // The higher the frequency the further apart the sample points will be
        float frequency = 1;
        
        // Create randomized offsets to use with grabbing samples from the noise map
        for (int i = 0; i < octaves; i++) {
            float offsetX = rng.Next(-100000, 100000) - offset.x;
            float offsetY = rng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }
        
        // Track max/min values to use in normalization after
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        // Used so that noise scale zooms into center instead of top-right corner
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        // Iterate through every position in the noise map
        for (int y = 0; y < mapHeight; y++) {
            // Grab samples from each position in the noise map
            for (int x = 0; x < mapWidth; x++) {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                // The number of octaves determines how many times we will add to the noiseHeight
                for (int i = 0; i < octaves; i++) {
                    // Divide the coordinates by scale to get proper float values
                    float sampleX = ((((x - halfWidth) + octaveOffsets[i].x) / noiseScale) * frequency);
                    float sampleY = ((((y - halfHeight) + octaveOffsets[i].y) / noiseScale) * frequency);

                    // Use the samples to generate Perlin values
                    float perlinValue = (Mathf.PerlinNoise(sampleX, sampleY) * 2) - 1;
                    // This will sometimes be negative
                    noiseHeight += perlinValue * amplitude;

                    // Amplitude will decrease each octave (0 < persistance < 1)
                    amplitude *= persistance;
                    // Frequency will increase each octave (lacunarity > 1)
                    frequency *= lacunarity;
                }  
                
                // Track min/max noise heights
                // Check both for first coords
                if (y == 0 && x == 0) {
                    if (noiseHeight > maxLocalNoiseHeight)
                        maxLocalNoiseHeight = noiseHeight;
                    if (noiseHeight < minLocalNoiseHeight)
                        minLocalNoiseHeight = noiseHeight;
                }
                else {
                    if (noiseHeight > maxLocalNoiseHeight)
                        maxLocalNoiseHeight = noiseHeight;
                    else if (noiseHeight < minLocalNoiseHeight)
                        minLocalNoiseHeight = noiseHeight;
                }

                // Fill every space in the noise map
                noiseMap[x,y] = noiseHeight;
            }
        }

        // Normalize each value in the noise map so none are negative
        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {
                if (normalizeMode == NormalizeMode.Local) 
                    // This will return a value between 0 and
                    noiseMap[x,y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x,y]);
                else {
                    float normalizedHeight = (noiseMap[x,y] + 1) / (maxPossibleHeight / 0.9f);
                    noiseMap[x,y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }
        return noiseMap;
    }
}
