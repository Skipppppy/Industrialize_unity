using UnityEngine;
[DefaultExecutionOrder(-1000)]
public class scenegravity : MonoBehaviour
{
    private Vector3 _previousgravity;
    private void Awake()
    {
        _previousgravity = Physics.gravity;
        Physics.gravity = Vector3.zero;
    }
    private void OnDestroy()
    {
        Physics.gravity = _previousgravity;
    }
}