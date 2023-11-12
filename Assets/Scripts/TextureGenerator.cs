using UnityEngine;
using System.Collections;

public static class TextureGenerator {
    // Create a color texture using a 1D array of colors
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height) {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }

    // Create a monochrome texture using a 1D array of values between 0 and 1
    public static Texture2D TextureFromHeightMap(float[,] heightMap) {
        // 0 to indicate the first dimension
        int width = heightMap.GetLength(0);
        // 1 to indicate the second dimension
        int height = heightMap.GetLength(1);

        // Store every color into a 1D array
        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                colorMap[(y * width) + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColorMap(colorMap, width, height);
    }
}