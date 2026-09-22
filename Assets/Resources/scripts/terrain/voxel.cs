using System;
public enum voxelmaterial : byte
{
    air = 0,
    dirt = 1,
    grass = 2,
    sand = 3
}
[Serializable]
public struct voxel
{
    public voxelmaterial material;
    public bool issolid
    {
        get
        {
            return material != voxelmaterial.air;
        }
    }
    public bool isvisible
    {
        get
        {
            return material != voxelmaterial.air;
        }
    }
}