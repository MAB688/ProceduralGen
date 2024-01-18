using System;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using System.Data;
using Unity.VisualScripting;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { HeightMap, ColorMap, MeshMap };
    public DrawMode drawMode;

    // Height map values are used for vertices in the mesh
    // We lose a row and column of "squares" when converting height map values into vertices
    // EX: Size 256 will generate a 256 x 256 height map which 
    // generates a mesh with 256 x 256 vertices (mesh will appear smaller than height map)
    // By adding an extra vertice, we retain the same amount of squares in the mesh and make LOD options easier
    public enum NumVerticeOption { Size4 = 5, Size128 = 129, Size256 = 257, Size512 = 513, Size1024 = 1025 };
    public NumVerticeOption numberOfVertices = NumVerticeOption.Size256;
    [HideInInspector]
    public int numVertices;

    // Must be a factor of (numVertices - 1)
    public enum LODOption { LOD0 = 0, LOD2 = 2, LOD4 = 4, LOD8 = 8 };
    public LODOption lodOption = LODOption.LOD0;
    private int editorLOD;

    public float noiseScale;

    [Range(0, 20)]
    public int octaves;
    // Must be in range from 0 to 1
    [Range(0, 1)]
    public float persistance;
    [Range(0, 27)]
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public int meshHeightMultiplier;
    // The curve changes how much each height is affected by the multiplier
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

    public TerrainType[] regions;

    // Generate sample maps in the editor (not used in infinite generation)
    public void DrawMapInEditor()
    {
        // Create an instance of the MapDisplay script
        MapDisplay display = FindObjectOfType<MapDisplay>();

        // Generate the height map data
        float[,] heightMap = NoiseMap.GenerateNoiseMap(numVertices, seed, noiseScale, octaves, persistance, lacunarity, Vector2.zero + offset);

        // Draw and display the height map as a monochrome 2D texture
        if (drawMode == DrawMode.HeightMap)
            display.DrawTexture(TextureGenerator.CreateHeightMapTexture(heightMap), true);
        //display.DrawTexture(TextureGenerator.CreateHeightMapTexture(NoiseMap.GenerateNoiseMap(numVertices, seed, noiseScale, octaves, persistance, lacunarity, Vector2.zero + offset)), true);

        // Draw and display the height map as a 2D texture with color
        else if (drawMode == DrawMode.ColorMap)
            display.DrawTexture(TextureGenerator.CreateColorMapTexture(heightMap, regions), false);

        // Display the height map with meshes and textures (Color map not needed)
        else if (drawMode == DrawMode.MeshMap)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap, meshHeightMultiplier, meshHeightCurve, editorLOD), TextureGenerator.CreateColorMapTexture(heightMap, regions));
        }
    }

    // This function is called automatically whenever one of the scripts variables is changed in the inspector
    void OnValidate()
    {
        numVertices = (int)numberOfVertices;
        editorLOD = (int)lodOption;

        // Maintain correct minimum values for parameters
        if (lacunarity < 1)
            lacunarity = 1;
        if (octaves < 1)
            octaves = 1;
        // Cannot divide by 0
        if (noiseScale <= 0)
            noiseScale = 0.0001f;
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}