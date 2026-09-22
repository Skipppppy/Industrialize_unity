using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VoxelSaveData
{
    public int x;
    public int y;
    public int z;

    public byte material;
}

[Serializable]
public class ItemSaveData
{
    public string prefabPath;

    public float px;
    public float py;
    public float pz;

    public float rx;
    public float ry;
    public float rz;
}

[Serializable]
public class PlayerSaveData
{
    public float px;
    public float py;
    public float pz;

    public float rx;
    public float ry;
    public float rz;
}

[Serializable]
public class WorldSaveData
{
    public List<VoxelSaveData> voxels =
        new List<VoxelSaveData>();

    public List<ItemSaveData> items =
        new List<ItemSaveData>();

    public PlayerSaveData player =
        new PlayerSaveData();
}