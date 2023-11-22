using System;
using UnityEngine;

public class MapGenerator : MonoBehaviour {
    public enum DrawMode {HeightMap, ColorMap}
    // MeshMap, FalloffMap};
    public enum ChunkSizeOption {Size128 = 128, Size256 = 256, Size512 = 512};
    public DrawMode drawMode;
    
    public ChunkSizeOption mapChunkSizeOption  = ChunkSizeOption.Size256;
    private int mapChunkSize;
    
    //[Range(0,6)]
    //public int editorLOD;
    public float noiseScale;

    [Range(0,20)]
    public int octaves;
    // Must be in range from 0 to 1
    [Range(0,1)]
    public float persistance;
    [Range(0,27)]
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    //public bool useFalloff;

    //public float meshHeightMultiplier;
    //public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

    public TerrainType[] regions;

    // Disabled for now
    //float[,] falloffMap;

    /*Queue<MapThreadInfo<float[,]>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<float[,]>>();
    Queue<MapThreadInfo<MeshGenerator.MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshGenerator.MeshData>>();*/

    // Generate the physical maps
    public void DrawMapInEditor() {
        // Create an instance of the MapDisplay script
        MapDisplay display = FindObjectOfType<MapDisplay>();

        // Generate the height map data
        float[,] heightMap = GenerateHeightMap(Vector2.zero);

        // Draw and display the height map as a monochrome 2D texture
        if (drawMode == DrawMode.HeightMap)
           display.DrawTexture(TextureGenerator.CreateHeightMapTexture(heightMap), true);

        // Draw and display the height map as a 2D texture with color
        else if (drawMode == DrawMode.ColorMap)
            display.DrawTexture(TextureGenerator.CreateColorMapTexture(heightMap, regions), false);
            
        // Display the height map with meshes and textures
        //else if (drawMode == DrawMode.MeshMap) 
            //display.DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap, meshHeightMultiplier, meshHeightCurve, editorLOD), 
            //TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));

        //else if (drawMode == DrawMode.FalloffMap)
            //display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
    }

    // Generate the height map 
    float[,] GenerateHeightMap(Vector2 center) {
        // Generate noise map
        float[,] heightMap = NoiseMap.GenerateNoiseMap(mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, center + offset);

        // Debug
        //DebugMap(heightMap);

        return heightMap;
    }

    void DebugMap(float[,] map) {
        // Overwrite the edges of the map with 0s for comparison with other maps
        // 0 = black, 1 = white
        for (int i = 0; i < mapChunkSize; i++) {
            // Bottom side [i,0]
            map[i, 0] = 1;
            // Top side [i,255]
            map[i, mapChunkSize - 1] = 0;
            // Right side [255,i]
            map[mapChunkSize - 1, i] = 1;
            // Left side [0,i] 
            map[0, i] = 0;
        }   
        // Mark origin corner
        map[0, 0] = 1;
        map[0, 1] = 1;
        map[0, 2] = 1;
        map[1, 0] = 1;
        map[2, 0] = 1;

        map[255, 255] = 0;
        map[255, 254] = 0;
        map[255, 253] = 0;
        map[254, 255] = 0;
        map[253, 255] = 0;

        // [0,255]........[255,255]
        // ....................
        // [0,0]..........[255,0]
    }

    void Awake() {
        //falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    // This function is called automatically whenever one of the scripts variables is changed in the inspector
    void OnValidate()
    {  
        mapChunkSize = (int)mapChunkSizeOption;

        // Maintain correct minimum values for parameters
        if (lacunarity < 1)
            lacunarity = 1;
        if (octaves < 1)
            octaves = 1;
        // Cannot divide by 0
        if (noiseScale <= 0)
            noiseScale = 0.0001f;

        //falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    void Update() {
        /*if (mapDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<float[,]> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MeshGenerator.MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        } */
    }

    /*public void RequestMapData(Vector2 center, Action<float[,]> callback) {
        ThreadStart threadStart = delegate {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }


    void MapDataThread(Vector2 center, Action<float[,]> callback) {
        float[,] mapData = GenerateNoiseMapData(center);
        lock(mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<float[,]>(callback, mapData));
        }
    }

    public void RequestMeshData(float[,] mapData, int lod, Action<MeshGenerator.MeshData> callback) {
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(float[,] mapData, int lod, Action<MeshGenerator.MeshData> callback) {
        MeshGenerator.MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData, meshHeightMultiplier, meshHeightCurve, lod);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshGenerator.MeshData>(callback, meshData));
        }
    } */

    // Generic so it can handle both map data and meshes
    /*struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }*/
}

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color color;
}