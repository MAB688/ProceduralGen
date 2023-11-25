using UnityEngine;
using System.Collections.Generic;
public class EndlessTerrain : MonoBehaviour {

	public const float maxViewDst = 450;
	public Transform viewer;

	public static Vector2 viewerPosition;
	int chunkSize;
	int numChunksVisible;

    // Dictionary of terrain chunks and their 2D positions
	Dictionary<Vector2, TerrainChunk> terrainChunks = new Dictionary<Vector2, TerrainChunk>();

    // List of terrain chunks that were visible in the previous frame
    // We use this list to "despawn" previously visible chunks
	List<TerrainChunk> prevVisibleTerrainChunks = new List<TerrainChunk>();

    // On game start this code is executed
	void Start() {
        // Get the chunk size from the map generator
        MapGenerator mapGenerator = FindObjectOfType<MapGenerator>();
		chunkSize = mapGenerator.numVertices;

        // Calculate the number of chunks that can be visible at once
		numChunksVisible = Mathf.RoundToInt(maxViewDst / chunkSize);
	}

    // On every frame this code is executed
	void Update() {
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
		UpdateVisibleChunks ();
	}
		
	void UpdateVisibleChunks() {
        // Hide all terrain chunks that were visible in the previous frame
		for (int i = 0; i < prevVisibleTerrainChunks.Count; i++) {
			prevVisibleTerrainChunks[i].SetVisible(false);
		}
		
        // Clear the list of terrain chunks that were visible in the previous frame
        prevVisibleTerrainChunks.Clear();
		
        // Get the 2D position of the chunk that the viewer is currently in
		int viewerX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
		int viewerY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        // Loop through all chunks that are visible
		for (int yOffset = -numChunksVisible; yOffset <= numChunksVisible; yOffset++) {
			for (int xOffset = -numChunksVisible; xOffset <= numChunksVisible; xOffset++) {
                // Get the 2D position of the visible chunk
				Vector2 viewedChunk = new Vector2(viewerX + xOffset, viewerY + yOffset);

                // If the terrain chunk dictionary contains the chunk
				if (terrainChunks.ContainsKey(viewedChunk)) {
                    // Determine whether the terrain chunk should be visible
					terrainChunks[viewedChunk].UpdateTerrainChunk();
                    
                    // If the terrain chunk is determined to be visible
					if (terrainChunks[viewedChunk].IsVisible()) {
                        // Add the terrain chunk to the list of visible chunks for the previous (this) frame
						prevVisibleTerrainChunks.Add(terrainChunks[viewedChunk]);
					}
                // If the terrain chunk dictionary does not contain the chunk
                } else {
                    // Add the chunk to the dictionary, it cannot be in the viewer's range yet
					terrainChunks.Add(viewedChunk, new TerrainChunk(viewedChunk, chunkSize, transform));
				}
			}
		}
	}

	public class TerrainChunk {
		GameObject meshObject;
		Vector2 position;
		Bounds bounds;

        // Constructor
		public TerrainChunk(Vector2 coord, int size, Transform parent) {
			// Position of the chunk
            position = coord * size;
            
            // Bounds of the chunk
			bounds = new Bounds(position, Vector2.one * size);

            // Position of the chunk in 3D space
			Vector3 positionV3 = new Vector3(position.x,0,position.y);

            // Create a plane for the chunk
			meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            // Set the position, scale, and parent of the chunk
			meshObject.transform.position = positionV3;
			meshObject.transform.localScale = Vector3.one * size /10f;
			meshObject.transform.parent = parent;

            // The chunk is intially not visible
			SetVisible(false);
		}

        // If the viwer is close enough, make the chunk visible
		public void UpdateTerrainChunk() {
			float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance (viewerPosition));
			bool visible = viewerDstFromNearestEdge <= maxViewDst;
			SetVisible (visible);
		}

		public void SetVisible(bool visible) {
			meshObject.SetActive (visible);
		}

		public bool IsVisible() {
			return meshObject.activeSelf;
		}
	}
}

/*

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
        // Fix getting chunk size from map generator
        chunkSize = 256;
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

            // Fix getting chunk size
            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, 256, 256);
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
*/