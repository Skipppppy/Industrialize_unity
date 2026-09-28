using UnityEngine;
[System.Serializable]
public class terrainpoint
{
    public Vector3 position;
    public float height;
    public int material;
    public terrainpoint(Vector3 _position, float _height, int _material)
    {
        position = _position;
        height = _height;
        material = _material;
    }
}