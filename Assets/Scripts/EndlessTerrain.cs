using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class EndlessTerrain : MonoBehaviour {

    const float scale = 5f;

    const float ViewerChunkUpdateThreshold = 25f;
    const float sqrViewerChunkUpdateThreshold = ViewerChunkUpdateThreshold * ViewerChunkUpdateThreshold;

    public LODInfo[] detailLevels;
    public static float maxViewDist;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisible;

    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> prevTerrainChunksVisible = new List<TerrainChunk>();

    void Start() {
        mapGenerator = FindObjectOfType<MapGenerator>();
        maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
        // Altered from source code
        chunkSize = MapGenerator.mapChunkSize;
        chunksVisible = Mathf.RoundToInt(maxViewDist / chunkSize);

        UpdateVisibleChunks();  
    }

    void Update() {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerChunkUpdateThreshold) {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();  
        }
    }

    public void UpdateVisibleChunks() {

        for (int i = 0; i < prevTerrainChunksVisible.Count; i++) {
            prevTerrainChunksVisible[i].SetVisible(false);
        }

        prevTerrainChunksVisible.Clear();
            
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++) {
            for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDict.ContainsKey(viewedChunkCoord))
                    terrainChunkDict[viewedChunkCoord].UpdateTerrainChunk();
                else
                    terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
            }
        }
    }

    public class TerrainChunk {

        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material) {
            this.detailLevels = detailLevels;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            // Add component returns the component it adds
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * scale; 
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++) {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData) {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk() {
            if (mapDataReceived) { 
                float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDistFromNearestEdge <= maxViewDist;

                if (visible) {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length; i++) {
                        if (viewerDistFromNearestEdge > detailLevels[i].visibleDistThreshold)
                            lodIndex = i + 1;
                        else 
                            break;
                    }
                    if (lodIndex != previousLODIndex) {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh) {
                            previousLODIndex = lodIndex;    
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                            lodMesh.RequestMesh(mapData);
                    }

                    prevTerrainChunksVisible.Add(this);
                }
                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }
    }
    
    class LODMesh {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback) {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshGenerator.MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData) {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod,  OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo {
        public int lod;
        // Once viewer is out of threshold, switch to lower lod
        public float visibleDistThreshold;
    }

}