using UnityEngine;

// Perlin values are always between 0.0 and 1.0

public static class Noise
{
    public static float[,] GenerateNoiseMap(int mapSize, int userSeed, float userNoiseScale, int numOctaves, float userPersistance, float userLacunarity, Vector2 userOffset) {
        // Since we will be generating square chunks, width and height (mapSize) are equal

        // An octave is a single layer of noise 
        // Different octaves (layers of noise) are added together to create the final noise map
        // Each octave adds an additional layer of detail to the noise map
        // The octives variable is the number of octaves

        // Intialize a 2D array of floats with "mapSize" rows and "mapSize" columns
        // Each float value in the array represents the noise height at that position
        float[,] noiseMap = new float[mapSize, mapSize];

        // Intialize a random number generator with a userSeed
        System.Random rng = new System.Random(userSeed);

        // Initalize an array of Vector2's with "numOctave" length
        // A vector2 is an object with an x and y coordinate

        Vector2[] octaveOffsets = new Vector2[numOctaves];

        // Initalize a variable to hold the maximum global noise height
        float maxGlobalNoiseHeight = 0;

        // Initalize a variable to hold the amplitude
        // Amplitude determines the range of values that the noise can produce
        // A higher amplitude will result in a greater range of values 
        // This can lead to more extreme features in the terrain
        float amplitude = 1;
        
        // Initialize a variable to hold the frequency
        // Frequency determines the size of the features
        // Higher frequency creates smaller, closley spaced features
        // Lower frequency creates larger, widely spaced features
        float frequency = 1;
        
        // For each octave:
        for (int i = 0; i < numOctaves; i++) {
            // Generate a random x and y offset (to create more diverse octaves)
            // The userOffset variable is a Vector2 (contains x and y value)
            // The variable allows the user to control the starting point of noise generation
            // By changing the values of userOffset, the user can "scroll" or "move" through the noise map
            float offsetX = rng.Next(-100000, 100000) - userOffset.x;
            float offsetY = rng.Next(-100000, 100000) - userOffset.y;
            
            // Add each octave's offset to the array
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            // Increase the max global noise height by the amplitude
            // This value will be used for global normalization later 
            maxGlobalNoiseHeight += amplitude;

            // Decrease the amplitude (persistance is a value between 0 and 1)
            // Persistance determines how much each octave (noise layer) contributes to the final noise map
            // By reducing each subsequent octave's impact, this leads to a more natural-looking final noise map
            amplitude *= userPersistance;
        }
        

        // Calculate the center of each axis 
        // These values represent the x and y coordinates of the center of the noise map
        float mapCenterX = mapSize / 2f;
        float mapCenterY = mapSize / 2f;

        // For each row in the noise map:
        for (int y = 0; y < mapSize; y++) {
            // For each column in the noise map:
            for (int x = 0; x < mapSize; x++) {
                // For each (x,y) position in the noise map:
                // Reset amplitude, frequency, and noiseHeight

                // Amplitude will decrease each octave
                amplitude = 1;

                // Frequency will increase each octave
                frequency = 1;

                // Nose height will be the final value we assign to each position (x,y)
                float noiseHeight = 0;

                // For each ocatave:
                for (int i = 0; i < numOctaves; i++) {
                    // Grab our random offsets generated earlier
                    // These are used to add variety to each noise layer
                    float offsetX = octaveOffsets[i].x;
                    float offsetY = octaveOffsets[i].y;

                    // Calculate the centered coordinates
                    // These are used to center the noise map around the center of the map
                    // Instead of the top left corner of the map
                    float centeredX = x - mapCenterX;
                    float centeredY = y - mapCenterY;

                    // Add the offsets to the centered coordinates
                    float adjustedX = centeredX + offsetX;
                    float adjustedY = centeredY + offsetY;

                    // Calculate the sample coordinates we will use to generate Perlin values:
                    // Divide the adjusted coordinates by the userNoiseScale (zoom)
                    // This scales the coordinates, affecting the spacing of features
                    // Multiply by the frequency to affect the size of the features
                    float sampleX = ((adjustedX / userNoiseScale) * frequency);
                    float sampleY = ((adjustedY / userNoiseScale) * frequency);

                    // Use the samples to generate Perlin values
                    // Perlin values are always between 0.0 and 1.0
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);

                    // Multiply by 2 and subtract 1 to get values between -1.0 and 1.0
                    // This allows for better noise generation
                    perlinValue = (perlinValue * 2) - 1;

                    // Multiply the perlin value by the amplitude
                    // Add the result to the noise height
                    noiseHeight += perlinValue * amplitude;

                    // Amplitude will decrease each octave (0 < persistance < 1)
                    amplitude *= userPersistance;

                    // Frequency will increase each octave (lacunarity > 1)
                    // Lacunarity determines how much the frequency increases for each subsequent octave
                    frequency *= userLacunarity;
                }  

                // Store the noise height in the noiseMap array at postion (x,y)
                noiseMap[x,y] = noiseHeight;
            }
        }

        // After each position in the noise map has been assigned a noiseHeight,
        // For each position in the noise map:
        for (int y = 0; y < mapSize; y++) {
            for (int x = 0; x < mapSize; x++) {
                // Normalize each value in the noise map so none are negative

                // Shift the height from [-1,1] to [0,2], making it positive
                float shiftedHeight = noiseMap[x,y] + 1;

                // Calculate a scaling factor to normalize the height
                float scalingFactor = maxGlobalNoiseHeight / 0.9f;

                // Normalize the height in a range of approximately within the range of 0 to 0.9
                float normalizedHeight = shiftedHeight / scalingFactor;

                // Set the noise map value to the normalized height
                // Clamp the value to ensure it is between 0 and 1
                noiseMap[x,y] = Mathf.Clamp(normalizedHeight, 0, 1);
            }
        }
        return noiseMap;
    }
}