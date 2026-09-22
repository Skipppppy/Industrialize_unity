using UnityEngine;

public class CameraHandheldSway : MonoBehaviour
{
    public float rotationAmount = 0.15f;
    public float rotationSpeed = 0.5f;

    private Vector3 targetRotation;
    private Vector3 currentRotation;

    public Vector3 GetRotation()
    {
        return currentRotation;
    }

    void Update()
    {
        if (Vector3.Distance(currentRotation, targetRotation) < 0.02f)
        {
            targetRotation = new Vector3(
                Random.Range(-rotationAmount * 0.3f, rotationAmount * 0.3f),
                Random.Range(-rotationAmount * 0.3f, rotationAmount * 0.3f), 
                Random.Range(-rotationAmount, rotationAmount)                 
            );
        }

        currentRotation = Vector3.Lerp(
            currentRotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}