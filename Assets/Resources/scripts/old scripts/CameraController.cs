using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float sensitivity = 2.0f;
    public float maxYAngle = 80.0f;
    private float rotationX = 0.0f;
    public CursorManager cursorlogic;
    public PlayerMovement player;
    public CameraHandheldSway sway;

    void Start()
    {

    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        transform.parent.Rotate(Vector3.up * mouseX * sensitivity);
        
        rotationX -= mouseY * sensitivity;
        rotationX = Mathf.Clamp(rotationX, -maxYAngle, maxYAngle);
        Vector3 rot = sway.GetRotation();

        transform.localRotation =
            Quaternion.Euler(
                rotationX + rot.x,
                rot.y,
                rot.z
            );

        if (cursorlogic.uiMode)
        {
            sensitivity = 0.3f; ;
        }
        else
        {
            sensitivity = 2f;
        }
    }
}
