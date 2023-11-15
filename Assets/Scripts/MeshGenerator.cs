using System.Numerics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public static class MeshGenerator {
    
    public static MeshData GenerateTerrainMesh(float[,] noiseMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail) {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        // If level of detail == 0, set the increment to 1 otherwise set it to an increment of 2
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        int borderedSize = noiseMap.GetLength(0);
        // Since we are just creating square meshes, we only need one length/width variable
        int meshSize = borderedSize - (2 * meshSimplificationIncrement);
        // Used for variables we want to remain constant (do not want to change with LOD)
        int meshSizeUnsimplified = borderedSize - 2;

        // Used to center the mesh data (subtract 1 because we count from 0)
        float halfWidth = (meshSizeUnsimplified - 1) / 2f;
        float halfHeight = (meshSizeUnsimplified - 1) / 2f;
        
        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine);

        int[,] vertexIndicesMap = new int[borderedSize,borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = 0;

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
                // This will be true if any of the statements are true
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if (isBorderVertex) {
                    vertexIndicesMap[x,y] = borderVertexIndex;
                    // We want border indices to be negative
                    borderVertexIndex--;
                } else {
                    // We will use this 2D map in the next loop
                    vertexIndicesMap[x,y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }
        
        // Altered source code here to correct mirroring and dimension differences:
        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
                int vertexIndex = vertexIndicesMap[x,y];

                // Each vertex needs a UV map to tell it where each texture should go
                // By subtracting the x-coordinate, this reverses the UV mapping along the x-axis, which corrects the mirroring issue
                // Subtract meshSimplificationIncrement to ensure UVs are properly centered
                Vector2 percent = new Vector2(1f - ((x - meshSimplificationIncrement) / (float) (meshSize - 1)), (y - meshSimplificationIncrement) / (float) (meshSize - 1));

                // heightMultiplier adjusts the y axis for proper 3D
                // heightMap[(meshSize - 1) - x, y] reverses the order of the heightmap data along the x-axis to allign with the unmirrowed UV map
                float height = heightCurve.Evaluate(noiseMap[(meshSize - 1) - x, y]) * heightMultiplier;

                Vector3 vertexPostion = new Vector3((percent.x * meshSizeUnsimplified) - halfWidth, height, halfHeight - (percent.y * meshSizeUnsimplified));

                meshData.AddVertex(vertexPostion, percent, vertexIndex);

                // Ignore right and bottom edges of the map *POSSIBLE BUG?*
                if ( x < borderedSize - 1 && y < borderedSize - 1) {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x,y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
                    meshData.AddTriangle(a,d,c);
                    meshData.AddTriangle(a,b,c);
                }
                vertexIndex++;
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

        int TriangleIndex;
        int borderTriangleIndex;

        int triangleIndex;

        public MeshData(int verticesPerLine) {
            // Array of vectors to store the vertices
            vertices = new Vector3[verticesPerLine * verticesPerLine];
            // Allows us to add textures to the meshes
            UVs = new Vector2[verticesPerLine * verticesPerLine];
            // Array of coordinates to store the triangles
            triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

            // Initialization border vertices array
            // Multply to get the side values, add 4 to include the corners
            borderVertices = new Vector3[(verticesPerLine * 4) + 4];
            borderTriangles = new int[24 * verticesPerLine];
        }

        public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
            // Check if this is a border index
            if (vertexIndex < 0) {
                borderVertices[-vertexIndex - 1] = vertexPosition;

            // Non-border index
            } else {
                vertices[vertexIndex] = vertexPosition;
                UVs[vertexIndex] = uv;
            }
        }

        // Helper method (I thought there was a bug here at one point?)
        public void AddTriangle(int a, int b, int c) {
            if (a < 0 || b < 0 || c < 0) {
                borderTriangles[borderTriangleIndex] = a;
                borderTriangles[borderTriangleIndex + 1] = b;
                borderTriangles[borderTriangleIndex + 2] = c;

                borderTriangleIndex += 3;
                
            } else {
                triangles[triangleIndex] = a;
                triangles[triangleIndex + 1] = b;
                triangles[triangleIndex + 2] = c;

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