using UnityEngine;

public static class MeshGenerator {
    
    public static MeshData GenerateTerrainMesh(float[,] noiseMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail) {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        int mapWidth = noiseMap.GetLength(0);
        int mapHeight = noiseMap.GetLength(1);

        // Used to center the mesh data (subtract 1 because we count from 0)
        float halfWidth = (mapWidth - 1) / 2f;
        float halfHeight = (mapHeight - 1) / 2f;

        // If level of detail == 0, set the increment to 1 otherwise set it to an increment of 2
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        // Replaces width in some places
        int verticesPerLine = (mapWidth - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        // Keep track of where we are in the 1D array heightMap
        int vertexIndex = 0;
        
        // Altered source code here to correct mirroring and dimension differences:
        for (int y = 0; y < mapHeight; y += meshSimplificationIncrement) {
            for (int x = 0; x < mapWidth; x += meshSimplificationIncrement) {
                // heightMultiplier adjusts the y axis for proper 3D
                // heightMap[(mapWidth - 1) - x, y] reverses the order of the heightmap data along the x-axis to allign with the unmirrowed UV map
                meshData.vertices[vertexIndex] = new Vector3(x - halfWidth, heightCurve.Evaluate(noiseMap[(mapWidth - 1) - x, y]) * heightMultiplier, halfHeight - y);
                // Each vertex needs a UV map to tell it where each texture should go
                // By subtracting the x-coordinate, this reverses the UV mapping along the x-axis, which correct the mirroring issue
                meshData.UVs[vertexIndex] = new Vector2(1f - (x / (float) (mapWidth - 1)), y / (float) (mapHeight - 1));

                // Ignore right and bottom edges of the map
                if ( x < mapWidth - 1 && y < mapHeight - 1) {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }
                vertexIndex++;
            }
        }
        // Return meshData to allow for multi-threading during world generation
        return meshData;
    }

    public class MeshData {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] UVs;

        int triangleIndex;

        public MeshData(int meshWidth, int meshHeight) {
            // Array of vectors to store the vertices
            vertices = new Vector3[meshWidth * meshHeight];
            // Allows us to add textures to the meshes
            UVs = new Vector2[meshWidth * meshHeight];
            // Array of coordinates to store the triangles
            triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        }

        // Helper method BUG HERE?
        public void AddTriangle(int a, int b, int c) {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;

            triangleIndex += 3;
        }

        public Mesh CreateMesh() {
            Mesh mesh = new Mesh();
            // Bypass mesh rendering limits (not supported on all platforms)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = UVs;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}