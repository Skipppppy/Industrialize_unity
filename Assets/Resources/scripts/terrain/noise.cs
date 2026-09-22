using UnityEngine;
public static class planetnoise
{
    public static float hash01(int _x, int _y, int _z, int _seed)
    {
        unchecked
        {
            uint _h = (uint)_seed;
            _h ^= (uint)_x * 374761393u;
            _h ^= (uint)_y * 668265263u;
            _h ^= (uint)_z * 2147483647u;
            _h ^= _h >> 13;
            _h *= 1274126177u;
            _h ^= _h >> 16;
            return (_h & 0x00ffffff) / 16777215f;
        }
    }
    public static float value(Vector3 _position, int _seed)
    {
        int _x0 = Mathf.FloorToInt(_position.x);
        int _y0 = Mathf.FloorToInt(_position.y);
        int _z0 = Mathf.FloorToInt(_position.z);
        int _x1 = _x0 + 1;
        int _y1 = _y0 + 1;
        int _z1 = _z0 + 1;
        float _tx = _position.x - _x0;
        float _ty = _position.y - _y0;
        float _tz = _position.z - _z0;
        _tx = _tx * _tx * (3f - 2f * _tx);
        _ty = _ty * _ty * (3f - 2f * _ty);
        _tz = _tz * _tz * (3f - 2f * _tz);
        float _a = Mathf.Lerp(hash01(_x0, _y0, _z0, _seed), hash01(_x1, _y0, _z0, _seed), _tx);
        float _b = Mathf.Lerp(hash01(_x0, _y1, _z0, _seed), hash01(_x1, _y1, _z0, _seed), _tx);
        float _c = Mathf.Lerp(hash01(_x0, _y0, _z1, _seed), hash01(_x1, _y0, _z1, _seed), _tx);
        float _d = Mathf.Lerp(hash01(_x0, _y1, _z1, _seed), hash01(_x1, _y1, _z1, _seed), _tx);
        float _e = Mathf.Lerp(_a, _b, _ty);
        float _f = Mathf.Lerp(_c, _d, _ty);
        return Mathf.Lerp(_e, _f, _tz);
    }
    public static float fbm(Vector3 _position, float _frequency, int _octaves, int _seed)
    {
        float _value = 0f;
        float _amplitude = 1f;
        float _normalizer = 0f;
        for (int _i = 0; _i < _octaves; _i++)
        {
            float _sample = value(_position * _frequency, _seed + _i * 97);
            _value += (_sample - 0.5f) * 2f * _amplitude;
            _normalizer += _amplitude;
            _amplitude *= 0.5f;
            _frequency *= 2f;
        }
        return _value / Mathf.Max(_normalizer, 0.001f);
    }
}