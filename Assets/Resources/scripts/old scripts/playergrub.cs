using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    public Camera playerCamera;
    public float rayDistance = 5f;
    public CursorManager ui;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !ui.uiMode)
        {
            Ray ray = new Ray(
                playerCamera.transform.position,
                playerCamera.transform.forward
            );

            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                PickupItem item = hit.collider.GetComponent<PickupItem>();

                if (item != null)
                {
                    item.TryPickup(transform);
                }
            }
        }
    }
}