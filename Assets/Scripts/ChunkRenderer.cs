using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;



[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

public class ChunkRenderer : MonoBehaviour
{
    public const int ChunkWidth = 32;
    public const int ChunkWidthSq = ChunkWidth * ChunkWidth;
    public const int ChunkHeight = 128;

    public ChunkData ChunkData;
    public GameWorld ParentWorld;

    public BlockDatabase Blocks;

    private Mesh chunkMesh;

    private ChunkData leftChunk;
    private ChunkData rightChunk;
    private ChunkData fwdChunk;
    private ChunkData backChunk;

    private List<Vector3> verticies = new List<Vector3>();
    private List<Vector2> uvs = new List<Vector2>();


    private static int[] triangles;

    private static ProfilerMarker MeshingMarker = new ProfilerMarker(ProfilerCategory.Loading, "Meshing");

    public static void InitTriangles()
    {
        triangles = new int[65536*6/4];

        int vertwxNum = 4;
        for (int i = 0; i < triangles.Length; i+=6)
        {
            triangles[i] = vertwxNum - 4;
            triangles[i+1] = vertwxNum - 3;
            triangles[i+2] = vertwxNum - 2;

            triangles[i+3] = vertwxNum - 3;
            triangles[i+4] = vertwxNum - 1;
            triangles[i+5] = vertwxNum - 2;
            vertwxNum += 4;
        }
        // TODO
    }

    private void Start()
    {
        ParentWorld.ChunkDatas.TryGetValue(ChunkData.ChunkPosition + Vector2Int.left , out leftChunk);
        ParentWorld.ChunkDatas.TryGetValue(ChunkData.ChunkPosition + Vector2Int.right , out rightChunk);
        ParentWorld.ChunkDatas.TryGetValue(ChunkData.ChunkPosition + Vector2Int.up , out fwdChunk);
        ParentWorld.ChunkDatas.TryGetValue(ChunkData.ChunkPosition + Vector2Int.down , out backChunk);

        chunkMesh = new Mesh();

        RegenerateMesh();

        GetComponent<MeshFilter>().mesh = chunkMesh;
    }

    private void RegenerateMesh()
    {
        MeshingMarker.Begin();

        verticies.Clear();
        uvs.Clear();

        for (int y = 0; y < ChunkHeight; y++)
        {
            for (int x = 0; x < ChunkWidth; x++)
            {
                for (int z = 0; z < ChunkWidth; z++)
                {
                    GenerateBlock(x, y, z);
                }
            }
        }

        chunkMesh.triangles = Array.Empty<int>();
        chunkMesh.vertices = verticies.ToArray();
        chunkMesh.uv = uvs.ToArray();
        chunkMesh.SetTriangles(triangles,0,verticies.Count*6/4,0);

        chunkMesh.Optimize();

        chunkMesh.RecalculateNormals();
        chunkMesh.RecalculateBounds();
        GetComponent<MeshCollider>().sharedMesh = chunkMesh;

        MeshingMarker.End();
    }

    public void SpawnBlock(Vector3Int blockPosition)
    {
        int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
        ChunkData.Blocks[index] = BlockType.Wood;
        RegenerateMesh();
    }

    public void DestroyBlock(Vector3Int blockPosition)
    {
        int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
        ChunkData.Blocks[index] = BlockType.Air;
        RegenerateMesh();
    }

    private void GenerateBlock(int x, int y, int z)
    {
        Vector3Int blockPosition = new Vector3Int(x, y, z);

        if (GetBlockAtPosition(blockPosition) == 0) return;

        BlockType blockType = GetBlockAtPosition(blockPosition);

        if (GetBlockAtPosition(blockPosition + Vector3Int.left) == 0)
        {
            GenerateRightSide(blockPosition);
            AddUvs(blockType, Vector3Int.left);
        }

        if (GetBlockAtPosition(blockPosition + Vector3Int.right) == 0) 
        {
            GenerateLeftSide(blockPosition);
            AddUvs(blockType, Vector3Int.right);
        }

        if (GetBlockAtPosition(blockPosition + Vector3Int.back) == 0) 
        {
            GenerateFrontSide(blockPosition);
            AddUvs(blockType, Vector3Int.back);
        }

        if (GetBlockAtPosition(blockPosition + Vector3Int.forward) == 0)
        {
            GenerateBackSide(blockPosition);
            AddUvs(blockType, Vector3Int.forward);
        }

        if (GetBlockAtPosition(blockPosition + Vector3Int.up) == 0)
        {
            GenerateTopSide(blockPosition);
            AddUvs(blockType, Vector3Int.up);
        }

        if (GetBlockAtPosition(blockPosition + Vector3Int.down) == 0)
        {
            GenerateBottomSide(blockPosition);
            AddUvs(blockType, Vector3Int.down);
        }

    }

    private BlockType GetBlockAtPosition(Vector3Int blockPosition)
    {
        if (blockPosition.x >= 0 && blockPosition.x < ChunkWidth &&
           blockPosition.y >= 0 && blockPosition.y < ChunkHeight &&
           blockPosition.z >= 0 && blockPosition.z < ChunkWidth)
        {
            int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
            return ChunkData.Blocks[index];
        }
        else
        {
            if (blockPosition.y < 0 || blockPosition.y >= ChunkHeight) return BlockType.Air;

            if (blockPosition.x < 0)
            {
                if(leftChunk == null)
                {
                    return BlockType.Air;
                }

                blockPosition.x += ChunkWidth;
                int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
                return leftChunk.Blocks[index];
            }
            if (blockPosition.x >= ChunkWidth)
            {
                if (rightChunk == null)
                {
                    return BlockType.Air;
                }

                blockPosition.x -= ChunkWidth;
                int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
                return rightChunk.Blocks[index];
            }

            if (blockPosition.z < 0)
            {
                if (backChunk == null)
                {
                    return BlockType.Air;
                }

                blockPosition.z += ChunkWidth;
                int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
                return backChunk.Blocks[index];
            }
            if (blockPosition.z >= ChunkWidth)
            {
                if (fwdChunk == null)
                {
                    return BlockType.Air;
                }

                blockPosition.z -= ChunkWidth;
                int index = blockPosition.x + blockPosition.y * ChunkWidthSq + blockPosition.z * ChunkWidth;
                return fwdChunk.Blocks[index];
            }
            
            return BlockType.Air;
        }
    }

    private void GenerateRightSide(Vector3Int blockPosition)
    {
        verticies.Add(new Vector3(0, 0, 0) + blockPosition);
        verticies.Add(new Vector3(0, 0, 1) + blockPosition);
        verticies.Add(new Vector3(0, 1, 0) + blockPosition);
        verticies.Add(new Vector3(0, 1, 1) + blockPosition);

    }    
    private void GenerateLeftSide(Vector3Int blockPosition)
    {
        verticies.Add((new Vector3(1, 0, 0) + blockPosition) );
        verticies.Add((new Vector3(1, 1, 0) + blockPosition));
        verticies.Add((new Vector3(1, 0, 1) + blockPosition) );
        verticies.Add((new Vector3(1, 1, 1) + blockPosition) );
    }
    private void GenerateFrontSide(Vector3Int blockPosition)
    {
        verticies.Add((new Vector3(0, 0, 0) + blockPosition) );
        verticies.Add((new Vector3(0, 1, 0) + blockPosition) );
        verticies.Add((new Vector3(1, 0, 0) + blockPosition) );
        verticies.Add((new Vector3(1, 1, 0) + blockPosition) );
    }
    private void GenerateBackSide(Vector3Int blockPosition)
    {
        verticies.Add((new Vector3(0, 0, 1) + blockPosition) );
        verticies.Add((new Vector3(1, 0, 1) + blockPosition) );
        verticies.Add((new Vector3(0, 1, 1) + blockPosition) );
        verticies.Add((new Vector3(1, 1, 1) + blockPosition) );
    }
    private void GenerateTopSide(Vector3Int blockPosition)
    {
        verticies.Add((new Vector3(0, 1, 0) + blockPosition) );
        verticies.Add((new Vector3(0, 1, 1) + blockPosition) );
        verticies.Add((new Vector3(1, 1, 0) + blockPosition) );
        verticies.Add((new Vector3(1, 1, 1) + blockPosition) );
    }
    private void GenerateBottomSide(Vector3Int blockPosition)
    {
        verticies.Add((new Vector3(0, 0, 0) + blockPosition) );
        verticies.Add((new Vector3(1, 0, 0) + blockPosition) );
        verticies.Add((new Vector3(0, 0, 1) + blockPosition) );
        verticies.Add((new Vector3(1, 0, 1) + blockPosition) );
    }


    private void AddUvs(BlockType blockType,Vector3Int normal)
    {
        Vector2 uv;

        BlockInfo info = Blocks.GetInfo(blockType);
        if (info != null)
        {
            uv = info.GetPixelOffset(normal)/512;
        }
        else
        {
            uv = new Vector2(320f / 512, 480f / 512);
        }

        for (int i = 0; i < 4; i++)
        {

            uvs.Add(uv);
        }

    }
}
