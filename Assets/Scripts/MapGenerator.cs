using System;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;

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

    // Changed to static so that it can be accessed from other scripts
    public TerrainType[] regions;

    // Queue of height map data threads and mesh data threads
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    // Generate the map data for infinite terrain
    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    // Thread to generate the height map data
    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = new MapData {
            heightMap = NoiseMap.GenerateNoiseMap(numVertices, seed, noiseScale, octaves, persistance, lacunarity, center + offset),
        };

        mapData.colorMap = TextureGenerator.CreateColorMap(mapData.heightMap, regions);

        lock (mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			MeshDataThread(mapData, lod, callback);
		};

		new Thread (threadStart).Start ();
	}

	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

    void Update() {
        if (mapDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}

		if (meshDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}
    }

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

struct MapThreadInfo<T>
{
    public readonly Action<T> callback;
    public readonly T parameter;

    public MapThreadInfo(Action<T> callback, T parameter)
    {
        this.callback = callback;
        this.parameter = parameter;
    }

}

public struct MapData {
	public float[,] heightMap;
	public Color[] colorMap;

	public MapData (float[,] heightMap, Color[] colorMap)
	{
		this.heightMap = heightMap;
		this.colorMap = colorMap;
	}
}