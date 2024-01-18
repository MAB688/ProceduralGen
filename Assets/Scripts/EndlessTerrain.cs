using UnityEngine;
using System.Collections.Generic;
public class EndlessTerrain : MonoBehaviour {

	public const float maxViewDst = 450;
	public Transform viewer;
    public Material mapMaterial;

    static MapGenerator mapGenerator;

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
        mapGenerator = FindObjectOfType<MapGenerator>();
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
					terrainChunks.Add(viewedChunk, new TerrainChunk(viewedChunk, chunkSize, transform, mapMaterial));
				}
			}
		}
	}

	public class TerrainChunk {
		GameObject meshObject;
		Vector2 position;
		Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;


        // Constructor
		public TerrainChunk(Vector2 coord, int size, Transform parent, Material material) {
			// Position of the chunk
            position = coord * size;
            // Bounds of the chunk
			bounds = new Bounds(position, Vector2.one * size);
            // Position of the chunk in 3D space
			Vector3 positionV3 = new Vector3(position.x,0,position.y);

            // Create a plane for the chunk
			// meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            // Set the position, scale, and parent of the chunk
			meshObject.transform.position = positionV3;
			// meshObject.transform.localScale = Vector3.one * size /10f;
			meshObject.transform.parent = parent;

            // The chunk is intially not visible
			SetVisible(false);

            // Request the height map for the chunk
            mapGenerator.ScheduleJobs(OnCallBack);
		}

        private void OnCallBack(MeshData meshData) {
            meshFilter.mesh = meshData.CreateMesh();
        }

        // If the viwer is close enough, make the chunk visible
		public void UpdateTerrainChunk() {
			float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance (viewerPosition));
			bool visible = viewerDstFromNearestEdge <= maxViewDst;
			SetVisible(visible);
		}

		public void SetVisible(bool visible) {
			meshObject.SetActive(visible);
		}

		public bool IsVisible() {
			return meshObject.activeSelf;
		}
	}
}