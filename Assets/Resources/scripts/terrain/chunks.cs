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
    private int _startx;
    private int _startz;
    private int _cellsx;
    private int _cellsz;
    private int _worldwidth;
    private int _worldlength;
    private float _pointsize;
    private float _baseheight;
    private float _height;
    private float _noisescale;
    private float _flatness;
    private float _textureworldsize;
    private float _seedx;
    private float _seedz;
    private bool _showpoints;
    private bool _showchunk;
    private float _pointgizmosize;

    public void initialize(int _startx, int _startz, int _cellsx, int _cellsz, int _worldwidth, int _worldlength, float _pointsize, float _baseheight, float _height, float _noisescale, float _flatness, float _textureworldsize, float _seedx, float _seedz, Material _grassmaterial)
    {
        this._startx = _startx;
        this._startz = _startz;
        this._cellsx = _cellsx;
        this._cellsz = _cellsz;
        this._worldwidth = _worldwidth;
        this._worldlength = _worldlength;
        this._pointsize = _pointsize;
        this._baseheight = _baseheight;
        this._height = _height;
        this._noisescale = _noisescale;
        this._flatness = _flatness;
        this._textureworldsize = _textureworldsize;
        this._seedx = _seedx;
        this._seedz = _seedz;
        _meshfilter = GetComponent<MeshFilter>();
        _meshrenderer = GetComponent<MeshRenderer>();
        _meshcollider = GetComponent<MeshCollider>();
        _meshrenderer.sharedMaterial = _grassmaterial;
        transform.position = new Vector3(_startx * _pointsize, 0f, _startz * _pointsize);
        createmesh();
    }

    public void setdebug(bool _showpoints, bool _showchunk, float _pointgizmosize)
    {
        this._showpoints = _showpoints;
        this._showchunk = _showchunk;
        this._pointgizmosize = _pointgizmosize;
    }

    private void createmesh()
    {
        int _vertexwidth = _cellsx + 1;
        int _vertexlength = _cellsz + 1;
        _points = new terrainpoint[_vertexwidth, _vertexlength];

        Vector3[] _vertices = new Vector3[_vertexwidth * _vertexlength];
        Vector2[] _uv = new Vector2[_vertices.Length];
        Vector3[] _normals = new Vector3[_vertices.Length];
        int[] _triangles = new int[_cellsx * _cellsz * 6];

        int _vertexindex = 0;

        for (int _x = 0; _x < _vertexwidth; _x++)
        {
            for (int _z = 0; _z < _vertexlength; _z++)
            {
                int _worldx = _startx + _x;
                int _worldz = _startz + _z;

                float _heightvalue = getheight(_worldx, _worldz);

                Vector3 _position = new Vector3(
                _x * _pointsize,
                _heightvalue,
                _z * _pointsize
                );

                _points[_x, _z] = new terrainpoint(
                _position,
                _heightvalue,
                0
                );

                _vertices[_vertexindex] = _position;

                _uv[_vertexindex] = new Vector2(
                _worldx * _pointsize / _textureworldsize,
                _worldz * _pointsize / _textureworldsize
                );

                float _left = getheight(_worldx - 1, _worldz);
                float _right = getheight(_worldx + 1, _worldz);
                float _down = getheight(_worldx, _worldz - 1);
                float _up = getheight(_worldx, _worldz + 1);

                _normals[_vertexindex] = new Vector3(
                _left - _right,
                2f * _pointsize,
                _down - _up
                ).normalized;

                _vertexindex++;
            }
        }

        int _triangleindex = 0;

        for (int _x = 0; _x < _cellsx; _x++)
        {
            for (int _z = 0; _z < _cellsz; _z++)
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

        _mesh = new Mesh();
        _mesh.name = "pointterrainchunkmesh";
        _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;
        _mesh.uv = _uv;
        _mesh.normals = _normals;
        _mesh.RecalculateBounds();

        _meshfilter.sharedMesh = _mesh;

        _meshcollider.sharedMesh = null;
        _meshcollider.sharedMesh = _mesh;
    }

    private float getheight(int _x, int _z)
    {
        float _noise = Mathf.PerlinNoise(
        _seedx + _x * _noisescale,
        _seedz + _z * _noisescale
        );

        _noise = Mathf.SmoothStep(0f, 1f, _noise);
        _noise = Mathf.Lerp(_noise, 0.5f, _flatness);

        return _baseheight + (_noise - 0.5f) * _height;
    }

    private void OnDrawGizmos()
    {
        if (_showchunk)
        {
            Gizmos.color = Color.yellow;

            Vector3 _center = new Vector3(
            _cellsx * _pointsize * 0.5f,
            _baseheight,
            _cellsz * _pointsize * 0.5f
            );

            Vector3 _size = new Vector3(
            _cellsx * _pointsize,
            Mathf.Max(_height, 0.1f),
            _cellsz * _pointsize
            );

            Gizmos.DrawWireCube(_center, _size);
        }

        if (_showpoints && _points != null)
        {
            Gizmos.color = Color.red;

            for (int _x = 0; _x < _points.GetLength(0); _x++)
            {
                for (int _z = 0; _z < _points.GetLength(1); _z++)
                {
                    Gizmos.DrawSphere(
                    _points[_x, _z].position,
                    _pointgizmosize
                    );
                }
            }
        }
    }
}