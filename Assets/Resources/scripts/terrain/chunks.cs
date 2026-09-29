using UnityEngine;
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class pointterrainchunk : MonoBehaviour
{
    private terrainpoint[,] _points;
    private Mesh _mesh;
    private MeshFilter _meshfilter;
    private MeshRenderer _meshrenderer;
    private MeshCollider _meshcollider;
    private int _face;
    private int _chunkx;
    private int _chunkz;
    private int _chunksize;
    private int _chunksperface;
    private float _planetradius;
    private float _baseheight;
    private float _height;
    private float _noisescale;
    private float _flatness;
    private float _textureworldsize;
    private float _seedx;
    private float _seedz;
    private bool _showpoints;
    private bool _showchunk;
    private bool _colliderenabled;
    private float _pointgizmosize;
    private int _pointgizmestep;
    private int _maxgizmopoints;
    public void initialize(int _face, int _chunkx, int _chunkz, int _chunksize, int _chunksperface, float _planetradius, float _baseheight, float _height, float _noisescale, float _flatness, float _textureworldsize, int _seed, Material _grassmaterial)
    {
        this._face = _face;
        this._chunkx = _chunkx;
        this._chunkz = _chunkz;
        this._chunksize = _chunksize;
        this._chunksperface = _chunksperface;
        this._planetradius = _planetradius;
        this._baseheight = _baseheight;
        this._height = _height;
        this._noisescale = _noisescale;
        this._flatness = _flatness;
        this._textureworldsize = _textureworldsize;
        this._seedx = _seed * 0.12345f;
        this._seedz = _seed * 0.67891f;
        _meshfilter = GetComponent<MeshFilter>();
        _meshrenderer = GetComponent<MeshRenderer>();
        _meshcollider = GetComponent<MeshCollider>();
        _meshrenderer.sharedMaterial = _grassmaterial;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        createmesh();
    }
    public void setdebug(bool _showpoints, bool _showchunk, float _pointgizmosize, int _pointgizmestep, int _maxgizmopoints)
    {
        this._showpoints = _showpoints;
        this._showchunk = _showchunk;
        this._pointgizmosize = _pointgizmosize;
        this._pointgizmestep = Mathf.Max(1, _pointgizmestep);
        this._maxgizmopoints = Mathf.Max(1, _maxgizmopoints);
    }
    public void setcolliderenabled(bool _enabled)
    {
        if (_meshcollider == null) return;
        if (_colliderenabled == _enabled) return;
        _colliderenabled = _enabled;
        _meshcollider.enabled = _enabled;
        if (_enabled && _mesh != null)
        {
            _meshcollider.sharedMesh = _mesh;
        }
        else
        {
            _meshcollider.sharedMesh = null;
        }
    }
    private void createmesh()
    {
        int _vertexwidth = _chunksize + 1;
        int _vertexlength = _chunksize + 1;
        int _totalcells = _chunksperface * _chunksize;
        _points = new terrainpoint[_vertexwidth, _vertexlength];
        Vector3[] _vertices = new Vector3[_vertexwidth * _vertexlength];
        Vector2[] _uv = new Vector2[_vertices.Length];
        int[] _triangles = new int[_chunksize * _chunksize * 6];
        int _vertexindex = 0;
        for (int _x = 0; _x < _vertexwidth; _x++)
        {
            for (int _z = 0; _z < _vertexlength; _z++)
            {
                float _u = -1f + ((_chunkx * _chunksize + _x) / (float)_totalcells) * 2f;
                float _v = -1f + ((_chunkz * _chunksize + _z) / (float)_totalcells) * 2f;
                Vector3 _direction = getdirection(_face, _u, _v);
                float _heightvalue = getheight(_direction);
                Vector3 _position = _direction * (_planetradius + _heightvalue);
                _points[_x, _z] = new terrainpoint(_position, _heightvalue, 0);
                _vertices[_vertexindex] = _position;
                float _texscale = 2f * _planetradius / Mathf.Max(_textureworldsize, 0.001f);
                _uv[_vertexindex] = new Vector2((_u + 1f) * 0.5f * _texscale, (_v + 1f) * 0.5f * _texscale);
                _vertexindex++;
            }
        }
        int _triangleindex = 0;
        for (int _x = 0; _x < _chunksize; _x++)
        {
            for (int _z = 0; _z < _chunksize; _z++)
            {
                int _a = _x * _vertexlength + _z;
                int _b = (_x + 1) * _vertexlength + _z;
                int _c = _x * _vertexlength + _z + 1;
                int _d = (_x + 1) * _vertexlength + _z + 1;
                _triangles[_triangleindex++] = _a;
                _triangles[_triangleindex++] = _c;
                _triangles[_triangleindex++] = _d;
                _triangles[_triangleindex++] = _a;
                _triangles[_triangleindex++] = _d;
                _triangles[_triangleindex++] = _b;
            }
        }
        for (int _i = 0; _i < _triangles.Length; _i += 3)
        {
            Vector3 _a = _vertices[_triangles[_i]];
            Vector3 _b = _vertices[_triangles[_i + 1]];
            Vector3 _c = _vertices[_triangles[_i + 2]];
            Vector3 _normal = Vector3.Cross(_b - _a, _c - _a);
            Vector3 _center = (_a + _b + _c).normalized;
            if (Vector3.Dot(_normal, _center) < 0f)
            {
                int _temp = _triangles[_i + 1];
                _triangles[_i + 1] = _triangles[_i + 2];
                _triangles[_i + 2] = _temp;
            }
        }
        _mesh = new Mesh();
        _mesh.name = "pointterrainchunkmesh";
        _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;
        _mesh.uv = _uv;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        _meshfilter.sharedMesh = _mesh;
        _meshcollider.sharedMesh = null;
        if (_colliderenabled)
        {
            _meshcollider.sharedMesh = _mesh;
        }
    }
    private Vector3 getdirection(int _face, float _u, float _v)
    {
        Vector3 _cube;
        switch (_face)
        {
            case 0: _cube = new Vector3(1f, _v, -_u); break;
            case 1: _cube = new Vector3(-1f, _v, _u); break;
            case 2: _cube = new Vector3(_u, 1f, -_v); break;
            case 3: _cube = new Vector3(_u, -1f, _v); break;
            case 4: _cube = new Vector3(_u, _v, 1f); break;
            default: _cube = new Vector3(-_u, _v, -1f); break;
        }
        return _cube.normalized;
    }
    private float getheight(Vector3 _direction)
    {
        float _noise1 = Mathf.PerlinNoise((_direction.x + 2f) * _noisescale + _seedx, (_direction.y + 2f) * _noisescale + _seedz);
        float _noise2 = Mathf.PerlinNoise((_direction.y + 2f) * _noisescale + _seedz * 0.71f, (_direction.z + 2f) * _noisescale + _seedx * 0.53f);
        float _noise3 = Mathf.PerlinNoise((_direction.z + 2f) * _noisescale + _seedx * 0.37f, (_direction.x + 2f) * _noisescale + _seedz * 0.83f);
        float _noise = (_noise1 + _noise2 + _noise3) / 3f;
        _noise = Mathf.SmoothStep(0f, 1f, _noise);
        _noise = Mathf.Lerp(_noise, 0.5f, _flatness);
        return _baseheight + (_noise - 0.5f) * _height;
    }
    private void OnDrawGizmos()
    {
        if (!_showchunk || _mesh == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_mesh.bounds.center, _mesh.bounds.size);
    }
    private void OnDrawGizmosSelected()
    {
        if (!_showpoints || _points == null) return;
        Gizmos.color = Color.red;
        int _width = _points.GetLength(0);
        int _length = _points.GetLength(1);
        int _step = Mathf.Max(1, _pointgizmestep);
        int _countx = Mathf.CeilToInt(_width / (float)_step);
        int _countz = Mathf.CeilToInt(_length / (float)_step);
        while (_countx * _countz > _maxgizmopoints)
        {
            _step++;
            _countx = Mathf.CeilToInt(_width / (float)_step);
            _countz = Mathf.CeilToInt(_length / (float)_step);
        }
        int _drawn = 0;
        for (int _x = 0; _x < _width; _x += _step)
        {
            for (int _z = 0; _z < _length; _z += _step)
            {
                if (_drawn >= _maxgizmopoints) break;
                Gizmos.DrawSphere(_points[_x, _z].position, _pointgizmosize);
                _drawn++;
            }
            if (_drawn >= _maxgizmopoints) break;
        }
    }
}