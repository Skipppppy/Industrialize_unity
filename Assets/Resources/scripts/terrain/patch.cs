using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
public class planetpatch : MonoBehaviour
{
    private voxelworld _world;
    private int _face;
    private int _lod;
    private int _x;
    private int _y;
    private MeshFilter _meshfilter;
    private MeshRenderer _meshrenderer;
    private MeshCollider _meshcollider;
    private planetpatch[] _children;
    private bool _built;
    public void initialize(voxelworld _world, int _face, int _lod, int _x, int _y)
    {
        this._world = _world;
        this._face = _face;
        this._lod = _lod;
        this._x = _x;
        this._y = _y;
        _meshfilter = gameObject.AddComponent<MeshFilter>();
        _meshrenderer = gameObject.AddComponent<MeshRenderer>();
        _meshcollider = gameObject.AddComponent<MeshCollider>();
        _meshrenderer.sharedMaterials = _world.getmaterials();
        gameObject.name = $"PlanetPatch_{_face}_{_lod}_{_x}_{_y}";
        build();
        _meshcollider.enabled = false;
    }
    public void updatelod(Vector3 _playerposition)
    {
        Vector3 _center = getcenterposition();
        float _distance = Vector3.Distance(_playerposition, _center);
        float _patchsize = getpatchsize();
        float _edgedistance = Mathf.Max(0f, _distance - _patchsize * 0.5f);
        if (_children != null)
        {
            bool _collapse = _edgedistance > _patchsize * 1.35f;
            if (_collapse)
            {
                collapse();
                return;
            }
            _meshrenderer.enabled = false;
            _meshcollider.enabled = false;
            for (int _i = 0; _i < _children.Length; _i++)
            {
                if (_children[_i] != null)
                    _children[_i].updatelod(_playerposition);
            }
            return;
        }
        bool _split = _lod < _world.maxlod && _edgedistance < _patchsize * _world.lodfactor;
        if (_split)
        {
            subdivide();
            return;
        }
        _meshrenderer.enabled = true;
        updatecollider(_edgedistance);
    }
    private void updatecollider(float _distance)
    {
        if (!_built)
            return;
        bool _near = _distance < _world.colliderdistance;
        bool _detailed = _lod >= _world.colliderstartlod;
        bool _enable = _near && _detailed;
        if (!_enable)
        {
            _meshcollider.sharedMesh = null;
            _meshcollider.enabled = false;
            return;
        }
        _meshcollider.sharedMesh = null;
        _meshcollider.sharedMesh = _meshfilter.sharedMesh;
        _meshcollider.enabled = true;
    }
    private void subdivide()
    {
        if (_children != null)
            return;
        _meshcollider.sharedMesh = null;
        _meshcollider.enabled = false;
        _children = new planetpatch[4];
        for (int _i = 0; _i < 4; _i++)
        {
            int _childx = _x * 2 + (_i % 2);
            int _childy = _y * 2 + (_i / 2);
            GameObject _object = new GameObject($"PlanetPatch_{_face}_{_lod + 1}_{_childx}_{_childy}");
            _object.transform.SetParent(transform.parent);
            _object.transform.localPosition = Vector3.zero;
            _object.transform.localRotation = Quaternion.identity;
            planetpatch _child = _object.AddComponent<planetpatch>();
            _child.initialize(_world, _face, _lod + 1, _childx, _childy);
            _children[_i] = _child;
        }
        _meshrenderer.enabled = false;
    }
    private void collapse()
    {
        if (_children == null)
            return;
        for (int _i = 0; _i < _children.Length; _i++)
        {
            if (_children[_i] != null)
                Destroy(_children[_i].gameObject);
        }
        _children = null;
        _meshrenderer.enabled = true;
        _meshcollider.sharedMesh = null;
        _meshcollider.enabled = false;
    }
    private void build()
    {
        int _resolution = Mathf.Max(2, _world.patchresolution);
        int _count = _resolution + 1;
        float _size = 2f / Mathf.Pow(2f, _lod);
        float _u0 = -1f + _x * _size;
        float _v0 = -1f + _y * _size;
        float _u1 = _u0 + _size;
        float _v1 = _v0 + _size;
        List<Vector3> _vertices = new List<Vector3>();
        List<Vector2> _uvs = new List<Vector2>();
        List<int>[] _triangles = new List<int>[3];
        for (int _i = 0; _i < 3; _i++)
            _triangles[_i] = new List<int>();
        float _textureScale = _world.planetradius / Mathf.Max(_world.textureworldsize, 0.001f);
        for (int _py = 0; _py < _count; _py++)
        {
            float _ty = _py / (float)_resolution;
            float _v = Mathf.Lerp(_v0, _v1, _ty);
            for (int _px = 0; _px < _count; _px++)
            {
                float _tx = _px / (float)_resolution;
                float _u = Mathf.Lerp(_u0, _u1, _tx);
                Vector3 _direction = _world.cubedirection(_face, _u, _v);
                float _height = _world.getterrainheight(_direction);
                Vector3 _position = _direction * (_world.planetradius + _height);
                _vertices.Add(_position);
                _uvs.Add(new Vector2(_u * _textureScale, _v * _textureScale));
            }
        }
        for (int _py = 0; _py < _resolution; _py++)
        {
            for (int _px = 0; _px < _resolution; _px++)
            {
                int _a = _py * _count + _px;
                int _b = _a + 1;
                int _c = _a + _count;
                int _d = _c + 1;
                Vector3 _center = (_vertices[_a] + _vertices[_b] + _vertices[_c] + _vertices[_d]).normalized;
                voxelmaterial _material = _world.getsurfacematerial(_center);
                int _materialindex = _world.getmaterialindex(_material);
                addtriangle(_vertices, _triangles, _materialindex, _a, _c, _d);
                addtriangle(_vertices, _triangles, _materialindex, _a, _d, _b);
            }
        }
        Mesh _mesh = new Mesh();
        _mesh.name = $"PlanetMesh_{_face}_{_lod}_{_x}_{_y}";
        _mesh.indexFormat = IndexFormat.UInt32;
        _mesh.SetVertices(_vertices);
        _mesh.SetUVs(0, _uvs);
        _mesh.subMeshCount = 3;
        for (int _i = 0; _i < 3; _i++)
            _mesh.SetTriangles(_triangles[_i], _i);
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        _meshfilter.sharedMesh = _mesh;
        _built = true;
    }
    private void addtriangle(List<Vector3> _vertices, List<int>[] _triangles, int _materialindex, int _a, int _b, int _c)
    {
        Vector3 _p0 = _vertices[_a];
        Vector3 _p1 = _vertices[_b];
        Vector3 _p2 = _vertices[_c];
        Vector3 _normal = Vector3.Cross(_p1 - _p0, _p2 - _p0);
        Vector3 _center = (_p0 + _p1 + _p2).normalized;
        if (Vector3.Dot(_normal, _center) < 0f)
        {
            int _temp = _b;
            _b = _c;
            _c = _temp;
        }
        _triangles[_materialindex].Add(_a);
        _triangles[_materialindex].Add(_b);
        _triangles[_materialindex].Add(_c);
    }
    private Vector3 getcenterposition()
    {
        float _size = 2f / Mathf.Pow(2f, _lod);
        float _u = -1f + (_x + 0.5f) * _size;
        float _v = -1f + (_y + 0.5f) * _size;
        Vector3 _direction = _world.cubedirection(_face, _u, _v);
        return _world.getsurfaceposition(_direction);
    }
    private float getpatchsize()
    {
        return 2f * _world.planetradius / Mathf.Pow(2f, _lod);
    }
}