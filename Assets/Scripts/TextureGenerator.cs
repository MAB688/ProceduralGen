using UnityEngine;

public static class TextureGenerator {
    // Create a monochrome texture using a 1D array of values between 0 and 1
    public static Texture2D CreateHeightMapTexture(float[,] heightMap) {
        // Square chunks, so width and height are equal
        int size = heightMap.GetLength(0);

        // Store every color into a 1D array
        Color[] colorMap = new Color[size * size];
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {

            // Get the current height
            float currentHeight = heightMap[(size - 1) - x, (size - 1) - y];
            int index = (y * size) + x;

            colorMap[index] = Color.Lerp(Color.black, Color.white, currentHeight);
            }
        }
        return OutputTexture(colorMap, size);
    }

    public static Texture2D CreateColorMapTexture(float[,] heightMap, TerrainType[] regions) {
        int size = heightMap.GetLength(0);

        // Create a 1D array of colors
        Color[] colorMap = new Color[size * size];
        // For every position in the noise map, assign terrain colors
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                
                /* if (useFalloff){
                    noiseMapData[x,y] = Mathf.Clamp01(noiseMapData[x,y] - falloffMap[x,y]);
                } */
            
                // Get the current height
                float currentHeight = heightMap[(size - 1) - x, (size - 1) - y];
                int index = (y * size) + x;

                // Match the current height to a color region
                for (int i = 0; i < regions.Length; i++) {
                    // If the current height is in the regions height range
                    if (currentHeight >= regions[i].height)
                        // Assign that region's color for pos [x,y] in the 1D array
                        colorMap[index] = regions[i].color;
                    // Exit loop once the correct color is assigned
                    else
                        break;
                    }
                }
            }
        return OutputTexture(colorMap, size);
    }

    // Output a texture using a 1D array of colors
    public static Texture2D OutputTexture(Color[] colorMap, int size) {
        Texture2D texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }
}