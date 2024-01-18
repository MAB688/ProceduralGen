using UnityEngine;
using System.Collections;

public static class MeshGenerator {
    
    public static MeshData GenerateTerrainMesh(float[,] heightMap, int heightMultiplier, AnimationCurve heightCurve, int LOD) {
		// Total vertices = verticesPerLine x verticesPerLine (0 to verticesPerLine - 1)
        int verticesPerLine = heightMap.GetLength(0);

		// Used to center the vertices around a common point
        float topLeftX = (verticesPerLine - 1) / -2f;
        float topLeftZ = (verticesPerLine - 1) / 2f;

		// Used to skip vertices and simplify mesh
		int incrementLOD = (LOD == 0) ? 1 : LOD * 2;

		// Subtract 1 because we count from 0
		int verticesPerLineLOD = ((verticesPerLine - 1) / incrementLOD) + 1;

        // The size of the mesh is the number of vertices per line altered by the LOD
		MeshData meshData = new MeshData(verticesPerLineLOD);

        int vertexIndex = 0;

		// For each value in the height map, create a vertex
		for (int y = 0; y < verticesPerLine; y += incrementLOD) {
			for (int x = 0; x < verticesPerLine; x += incrementLOD) {
				// Collect 3D position of vertices 
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap [x, (verticesPerLine - 1) - y]) * heightMultiplier, topLeftZ - y);
                // Calculate UVs for texture mapping
				meshData.uvs[vertexIndex] = new Vector2(1f - (x / (float)verticesPerLine), y / (float)verticesPerLine);

				// Build triangles, collection of 3 vertices, to form structure of mesh
                if (x < verticesPerLine - 1 && y < verticesPerLine - 1) {
					meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLineLOD + 1, vertexIndex + verticesPerLineLOD);
					meshData.AddTriangle(vertexIndex + verticesPerLineLOD + 1, vertexIndex, vertexIndex + 1);
				}
                vertexIndex++;
            }
        }
        return meshData;
    }
}

public class MeshData {
	public Vector3[] vertices;
	public int[] triangles;
	public Vector2[] uvs;

	int triangleIndex;

	public MeshData(int verticesPerLine) {
		vertices = new Vector3[verticesPerLine * verticesPerLine];
		uvs = new Vector2[verticesPerLine * verticesPerLine];
		triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
	}

	public void AddTriangle(int vertexA, int vertexB, int vertexC) {
		triangles[triangleIndex] = vertexA;
		triangles[triangleIndex + 1] = vertexB;
		triangles[triangleIndex + 2] = vertexC;
		triangleIndex += 3;
	}

	public Mesh CreateMesh() {
		Mesh mesh = new Mesh();
		// 16 bit index only allows upto 255 vertices
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mesh.RecalculateNormals();
		return mesh;
	}

}