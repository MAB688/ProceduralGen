using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

// Size of height map will come in as +2 to account for the border overlap needed to smooth the seams between chunks
// A mesh is a collection of vertices, edges, and faces that define the shape of a 3D object
// A vertex is a point in 3D space, they are connected by edges to form faces
// A tringale is the simplest face, it is defined by 3 vertices
// A UV is a texture coordinate that determine how a 2D texture will be mapped onto a 3D mesh
// UVs are assigned per vertex
public static class MeshGenerator {
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int lod) {
        // Allows us to set a height curve in the inspector
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        // The total size, including the border part and mesh part
        // 258 = 256 + 2
        int totalSize = heightMap.GetLength(0);

        // The size of the mesh without any lod reductions or border (this part will be rendered)
        // Used for variables we want to remain constant (do not want to change with LOD)
        // 256 = 258 - 2
        int meshSize = totalSize - 2;

        // If level of detail == 0, set lod to 1 otherwise set it to an increment of 2
        int lodIncrement = (lod == 0) ? 1 : lod * 2;

        // The size of the mesh with lod reductions, but no border (this part will be rendered)
        // The higher the lodReduction, the less detailed the mesh is
        int lodMeshSize = totalSize - (2 * lodIncrement);
        
        // The LOD setting determines how many vertices there are per line
        int verticesPerLine = (lodMeshSize - 1) / (lodIncrement + 1);

        // DEBUG
        //Debug.Log("Vertice per line: " + verticesPerLine);

        // Initialize a meshData object
        MeshData meshData = new MeshData(verticesPerLine);

        // Initialize a 2D array of ints to hold the vertex indices
        // of both the rendered mesh and the border
        int[,] vertexIndicesMap = new int[totalSize, totalSize];
        
        // Initialize a variable to hold the mesh vertex index
        int meshVertexIndex = 0;
        
        // Initialize a variable to hold the border vertex index
        // The border will not be rendered, but is used to smooth the seams between chunks
        int borderVertexIndex = -1;

        // Iterate over the size of the total size to fill the vertexIndicesMap array
        // Skip vertices depending on the LOD setting
        for (int y = 0; y < totalSize; y += lodIncrement) {
            for (int x = 0; x < totalSize; x += lodIncrement) {
                // If the vertex is on the border of the map, it is a border vertex
                if (y == 0 || y == totalSize - 1 || x == 0 || x == totalSize - 1) {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    // Border indices will be negative values starting from -1
                    borderVertexIndex--;
                // Otherwise, it is rendered mesh vertex
                } else {
                    // These interior indices will be positive values starting from 0
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        /* Debug - Good
        for (int y = 0; y < totalSize; y++) {
            for (int x = 0; x < totalSize; x++) {
                Debug.Log("Vertex Map [" + x + ", " + y + "]: " + vertexIndicesMap[x, y]);
            }
        } */

        // Used to center the mesh data (subtract 1 because we count from 0)
        float topLeftX = (meshSize - 1) / -2f;
        float topLeftZ = (meshSize - 1) / 2f;

        /* Debug - Good
        Debug.Log("Top Left X: " + topLeftX);
        Debug.Log("Top Left Z: " + topLeftZ);
        */
        
        // Iterate over each vertex to build the triangles
        // Skip vertices depending on the LOD setting
        for (int y = 0; y < totalSize; y += lodIncrement) {
            for (int x = 0; x < totalSize; x += lodIncrement) {
                // Get the vertex index at the current position
                int vertexIndex = vertexIndicesMap[x,y];
                
                // DEBUG
                // Debug.Log("Vertex Map [" + x + ", " + y + "]: " + "Vertex Index = " + vertexIndex);

                // Calculate the relative position of this vertex's x coordinate in the mesh
                // Subtract lodIncrement to ensure UVs are properly centered
                float relX = (x - lodIncrement) / (float) (meshSize);
                // Calculate the relative position of this vertex's y coordinate  in the mesh
                float relY = (y - lodIncrement) / (float) (meshSize);

                // Subtract the x-coordinate to reverse the UV mapping along the x-axis, correcting mirroring
                Vector2 uvCoords = new Vector2(relX, relY);

                // Calculate the height of the vertex, or the y-coordinate
                // heightMap[(meshSize - 1) - x, y] reverses the order of the heightmap data along the x-axis to allign with the unmirrowed UV map
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;

                // Calculate the position (3D coordinate) of the vertex
                Vector3 vertexPostion = new Vector3(topLeftX + (uvCoords.x * meshSize) , height, topLeftZ - (uvCoords.y * meshSize));

                /* DEBUG
                Debug.Log("Vertex Position: " + vertexPostion);*/

                // Add the vertex to the meshData object
                meshData.AddVertex(vertexPostion, uvCoords, vertexIndex);

                // Build a triangle using the vertex we just calculated as the base
                // Cannot run if on the right or bottom edge of the map
                if (x < totalSize - 1 && y < totalSize - 1) {
                    // DEBUG
                    // Debug.Log("Building Triangle for [" + x + ", " + y + "]");

                    // The lod increment is used to determine the size of the triangles
                    // The larger the triangle, the less detailed the mesh is
                    int vertexA = vertexIndicesMap[x, y];
                    int vertexB = vertexIndicesMap[x + lodIncrement, y];
                    int vertexC = vertexIndicesMap[x, y + lodIncrement];
                    int vertexD = vertexIndicesMap[x + lodIncrement, y + lodIncrement];

                    /* DEBUG
                    if (x == 5 && y == 1) {
                        Debug.Log("Vertex A: " + vertexA);
                        Debug.Log("Vertex B: " + vertexB);
                        Debug.Log("Vertex C: " + vertexC);
                        Debug.Log("Vertex D: " + vertexD);
                    } */

                    meshData.AddTriangle(vertexA, vertexD, vertexC);
                    meshData.AddTriangle(vertexD, vertexA, vertexB);
                }
            }
        }
        // Return meshData to allow for multi-threading during world generation
        return meshData;
    }

    public class MeshData {
        Vector3[] vertices;
        int[] triangles;
        Vector2[] UVs;

        Vector3[] borderVertices;
        int[] borderTriangles;

        int borderTriangleIndex;
        int triangleIndex;

        public MeshData(int verticesPerLine) {
            // Array to store 3D coordinates of the vertices
            vertices = new Vector3[verticesPerLine * verticesPerLine];
            
            // Array to store 2D coordinates of the UVs for texture mapping
            UVs = new Vector2[verticesPerLine * verticesPerLine];
            
            // Array to store the indices of the vertices that make up each triangle
            triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

            // DEBUG
            //Debug.Log("Size of triangles array: " + triangles.Length);

            // Array to store the 3D coordinates of the border vertices
            // Multply to get the side values, add 4 to include the corners
            borderVertices = new Vector3[(verticesPerLine * 4) + 4];

            // Array to store the indices of the vertices that make up each border triangle
            // Each edge of the border is made up of 6 vertices and there are 4 edges, so multiply by 24
            borderTriangles = new int[24 * verticesPerLine];
        }

        // Adds a vertex to the appropriate array based on the vertex index
        public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
            // Check if this is a border index
            if (vertexIndex < 0) {
                // Convert the negative index to a positive index and subtract 1 to account for the 0 index
                // Store the vertex's position in the borderVertices array
                borderVertices[(-vertexIndex) - 1] = vertexPosition;

            // Non-border index
            } else {
                // Store the vertex's position in the vertices array
                vertices[vertexIndex] = vertexPosition;
                // Store the vertex's UV in the UVs array
                UVs[vertexIndex] = uv;
            }
        }

        // Helper method (I thought there was a bug here at one point?)
        public void AddTriangle(int vertA, int vertB, int vertC) {

            if (vertA < 0 || vertB < 0 || vertC < 0) {
                // DEBUG
                //Debug.Log("Adding Border Triangle: " + vertA + ", " + vertB + ", " + vertC);

                // Assign the vertices of the border triangle
                // Every 3 vertices makes up a triangle
                borderTriangles[borderTriangleIndex] = vertA;
                borderTriangles[borderTriangleIndex + 1] = vertB;
                borderTriangles[borderTriangleIndex + 2] = vertC;

                borderTriangleIndex += 3;
                
            } else {
                /* DEBUG
                Debug.Log("Adding Triangle: " + vertA + ", " + vertB + ", " + vertC);
                Debug.Log("Triangle Index [" + triangleIndex + "] = " + vertA);
                Debug.Log("Triangle Index + 1 [" + (triangleIndex + 1) + "] = " + vertB);
                Debug.Log("Triangle Index + 2 [" + (triangleIndex + 2) + "] = " + vertC); */


                // Assign the vertices of a normal triangle
                triangles[triangleIndex] = vertA;
                triangles[triangleIndex + 1] = vertB;
                triangles[triangleIndex + 2] = vertC;

                triangleIndex += 3;
            }
        }

        // Normals are the direction planes face
        // Used to synchronize lighting between chunks
        Vector3[] CalculateNormals() {
            Vector3[] vertexNormals = new Vector3[vertices.Length];
            // Triangles array stores sets of 3 vertices
            // Divide by 3 to get the number of triangles
            int triangleCount = triangles.Length / 3;
            for (int i = 0; i < triangleCount; i++) {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = triangles[normalTriangleIndex];
                int vertexIndexB = triangles[normalTriangleIndex + 1];
                int vertexIndexC = triangles[normalTriangleIndex + 2];

                Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                vertexNormals[vertexIndexA] += triangleNormal;
                vertexNormals[vertexIndexB] += triangleNormal;
                vertexNormals[vertexIndexC] += triangleNormal;
            }

            int borderTriangleCount = borderTriangles.Length / 3;
            for (int i = 0; i < borderTriangleCount; i++) {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = borderTriangles[normalTriangleIndex];
                int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
                int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

                Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                if (vertexIndexA >= 0 )
                    vertexNormals[vertexIndexA] += triangleNormal;
                if (vertexIndexB >= 0 )
                    vertexNormals[vertexIndexB] += triangleNormal;
                if (vertexIndexC >= 0 )
                    vertexNormals[vertexIndexC] += triangleNormal;
            }

            // Normalize each value in the array
            for (int i = 0; i < vertexNormals.Length; i++) {
                vertexNormals[i].Normalize();
            }
            return vertexNormals;
        }

        Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
            Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
            Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
            Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

            // Cross product
            Vector3 sideAB = pointB - pointA;
            Vector3 sideAC = pointC - pointA;
            return Vector3.Cross(sideAB, sideAC).normalized;
        }

        public Mesh CreateMesh() {
            Mesh mesh = new Mesh();
            // Bypass mesh rendering limits (not supported on all platforms)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = UVs;
            mesh.normals = CalculateNormals();
            return mesh;
        }
    }
}