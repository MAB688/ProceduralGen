# Procedural World Generation Project 
### (Currently undergoing refactoring)

This project focuses on procedural generation to create realistic and dynamic terrain for use in Unity. The main components include a Map Generator, Mesh Generator, and an Endless Terrain system to handle infinite terrain generation.

## Features

- **Procedural Generation:** Constructs Perlin noise to generate realistic and diverse height maps.
- **Infinite Terrain:** Implements an Endless Terrain system to handle dynamically loading and unloading terrain chunks.
- **Multi-threading:** Utilizes multi-threading for efficient and parallelized terrain generation.
- **Level of Detail:** Uses a dynamic LOD system to increase performance by rendering less detailed distant terrain

## Getting Started

### Prerequisites

- Unity 3D (Version 2022.3.7f1)

### Installation

1. Clone the repository.
2. Open the project in Unity.

## How to Use

- Adjust parameters on the Map Generator in the Unity Inspector to customize terrain generation.
- Press play and move around the viewer to render infinite chunks

## Code Overview

### MapGenerator.cs

- Responsible for generating height maps using Perlin noise.
- Utilizes multi-threading for parallelized execution.

### MeshGenerator.cs

- Generates terrain meshes based on the height map.
- Implements LOD (Level of Detail) to optimize performance.

### EndlessTerrain.cs

- Handles the continuous generation and updating of terrain chunks.
- Efficiently loads and unloads chunks based on the viewer's position.

## License

This project is licensed under the [MIT License](LICENSE).

