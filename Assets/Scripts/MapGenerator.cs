using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;

public class MapGenerator : MonoBehaviour {
    public enum DrawMode {NoiseMap, ColorMap, MeshMap, FalloffMap};
    public DrawMode drawMode;

    public Noise.NormalizeMode normalizedMode;
    
    // size - 1 so that the color and mesh maps match and work with calculations
    // Noise map will render full 241 chunk size
    // *POTENTIAL BUG* need to check mapChunkSize, maybe try 239
    public const int mapChunkSize = 239;
    [Range(0,6)]
    public int editorLOD;
    public float noiseScale;

    public int octaves;
    // Must be in range from 0 to 1
    [Range(0,1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public bool useFalloff;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

    public TerrainType[] regions;

    float[,] falloffMap;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshGenerator.MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshGenerator.MeshData>>();

    void Awake() {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    public void DrawMapInEditor() {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        // Display the height map as a monochrome 2D texture
        if (drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        // Display the height map as a 2D texture with color *BUG HERE*
        else if (drawMode == DrawMode.ColorMap)
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        // Display the height map with meshes and textures
        else if (drawMode == DrawMode.MeshMap) 
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorLOD), 
            TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.FalloffMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback) {
        ThreadStart threadStart = delegate {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback) {
        MapData mapData = GenerateMapData(center);
        lock(mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshGenerator.MeshData> callback) {
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshGenerator.MeshData> callback) {
        MeshGenerator.MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshGenerator.MeshData>(callback, meshData));
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
                MapThreadInfo<MeshGenerator.MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData(Vector2 center) {
        // Generate noise map
        // *POTENTIAL BUG* Check on mapChunkSize
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, seed, noiseScale, octaves, persistance, lacunarity, center + offset, normalizedMode);

        // Assign terrain colors to the height/noise map
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        // For every position in the noise map
        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                if (useFalloff){
                    noiseMap[x,y] = Mathf.Clamp01(noiseMap[x,y] - falloffMap[x,y]);
                }
                // Get the current height
                float currentHeight = noiseMap[x,y];
                // Find the color for that height range
                for (int i = 0; i < regions.Length; i++) {
                    if (currentHeight >= regions[i].height)
                        // Assign that color into a 1D array
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                    // Break once we've reached a value less than the regions height
                    else
                        break;
                }
            }
        }

        if (useFalloff) {
            // Correct outside edges for falloff mode
            // (I think the mesh borders were normalizing with the extra border values)
            for (int i = 0; i <= 240; i++) {
                noiseMap[i, 240] = 0;
                noiseMap[240, i] = 0;
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    // This function is called automatically whenever one of the scripts variables is changed in the inspector
    void OnValidate()
    {  
        if (lacunarity < 1)
            lacunarity = 1;

        if (octaves < 1)
            octaves = 1;

        // Cannot divide by 0
        if (noiseScale <= 0)
            noiseScale = 0.0001f;

        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    // Generic so it can handle both map data and meshes
    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}

public struct MapData {
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap) {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
