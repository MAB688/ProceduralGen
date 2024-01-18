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

    NativeArray<float> mapData;
    //float[,] mapData;
    //public MeshData meshData;
    //JobHandle secondHandle;

    public struct MapDataJob : IJob
    {
        public int numVertices;
        public int seed;
        public float noiseScale;
        public int octaves;
        public float persistance;
        public float lacunarity;
        public Vector2 offset;
        public NativeArray<float> mapData;

        public void Execute()
        {
            mapData = FlattenArray(NoiseMap.GenerateNoiseMap(numVertices, seed, noiseScale, octaves, persistance, lacunarity, offset));
        }
    }

    /*public struct MeshDataJob : IJob
    {
        public NativeArray<float> mapData;
        public int meshHeightMultiplier;
        public AnimationCurve meshHeightCurve;
        public int editorLOD;
        public MeshData meshData;

        public void Execute()
        {
            meshData = MeshGenerator.GenerateTerrainMesh(Expand(mapData),
            meshHeightMultiplier, meshHeightCurve, editorLOD);
        }
    } */

    public void ScheduleJobs(Action<MeshData> callback)
    {
        mapData = new NativeArray<float>(numVertices * numVertices, Allocator.TempJob);
        //meshData = new MeshData(numVertices);

        MapDataJob mapDataJob = new MapDataJob
        {
            numVertices = numVertices,
            seed = seed,
            noiseScale = noiseScale,
            octaves = octaves,
            persistance = persistance,
            lacunarity = lacunarity,
            offset = offset,
            mapData = mapData
        };

        JobHandle firstHandle = mapDataJob.Schedule();
        firstHandle.Complete();

        /* MeshDataJob meshDataJob = new MeshDataJob {
            mapData = mapData,
            meshHeightMultiplier = meshHeightMultiplier,
            meshHeightCurve = meshHeightCurve,
            editorLOD = editorLOD,
            meshData = meshData
        };

        secondHandle = meshDataJob.Schedule(firstHandle);
        secondHandle.Complete();
        mapData.Dispose(); */

        MeshData meshData = MeshGenerator.GenerateTerrainMesh(ExpandArray(mapData), meshHeightMultiplier, meshHeightCurve, editorLOD);
        callback?.Invoke(meshData);
    }

    static NativeArray<float> FlattenArray(float[,] arrayToFlatten)
    {   
        int rows = arrayToFlatten.GetLength(0);
        int columns = arrayToFlatten.GetLength(1);

        NativeArray<float> flattenedArray = new NativeArray<float>(rows * columns, Allocator.Temp);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                int index = i * columns + j;
                flattenedArray[index] = arrayToFlatten[i, j];
            }
        }

        return flattenedArray;
    }

    float[,] ExpandArray(NativeArray<float> flattenedArray)
    {
        int length = flattenedArray.Length - 1;
        int columns = numVertices;
        int rows = length / columns;
        float[,] expandedArray = new float[rows, columns];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                int index = i * columns + j;
                expandedArray[i, j] = flattenedArray[index];
            }
        }

        return expandedArray;
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}