using UnityEngine;

public class CameraFOV : MonoBehaviour
{
    public Camera playerCamera;
    public float normalFOV = 75f;
    public float sprintFOV = 90f;
    public float fovSmooth = 8f;
    public CursorManager ui;

    void Update()
    {
        UpdateFOV();
    }

    void UpdateFOV()
    {
        bool isRunning = !ui.uiMode &&
            Input.GetKey(KeyCode.LeftShift);

        bool isMoving = !ui.uiMode &&
            (
                Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0 ||
                Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0
            );

        float targetFOV = normalFOV;

        if (isRunning && isMoving)
        {
            targetFOV = sprintFOV;
        }

        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFOV,
            fovSmooth * Time.deltaTime
        );
    }
}