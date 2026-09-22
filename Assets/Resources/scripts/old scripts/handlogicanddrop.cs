using UnityEngine;

public class handlogicanddrop : MonoBehaviour
{
    public playerinventoryhandsmanager hands;
    public bool mainhand = false;
    public itemsprefabs_ database;
    public Transform throwPoint;

    private void Start()
    {
       database = FindFirstObjectByType<itemsprefabs_>();
       hands = FindFirstObjectByType<playerinventoryhandsmanager>();
    }

    void Update()
    {
        itemdrop();
    }
    public void itemdrop()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            mainhand = !mainhand;
            Debug.Log("mainhand: " + mainhand);
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (mainhand == false && hands.righthandempty == false)
            {
                foreach (var item in database.items)
                {
                    if (item.id == hands.righthandid)
                    {
                        Debug.Log(item.prefab.name);

                        GameObject obj = Instantiate(item.prefab, throwPoint.position, throwPoint.rotation);

                        Rigidbody rb = obj.GetComponent<Rigidbody>();

                        if (rb != null)
                        {
                            rb.isKinematic = false;
                            rb.useGravity = true;

                            rb.linearVelocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;

                            rb.collisionDetectionMode =
                                CollisionDetectionMode.Continuous;

                            Camera cam = Camera.main;

                            Ray ray = cam.ViewportPointToRay(
                                new Vector3(0.5f, 0.5f, 0f)
                            );

                            Vector3 throwDirection = ray.direction.normalized;

                            float throwForce = 8f;

                            rb.linearVelocity = throwDirection * throwForce;
                        }
                    }
                }
                hands.righthandempty = true;
                hands.righthandid = 0;

                inventoryhandsui ui = FindFirstObjectByType<inventoryhandsui>();
                if (ui != null)
                {
                    ui.ClearUIRight();
                }
            }
            else if (mainhand == true && hands.lefthandempty == false)
            {
                foreach (var item in database.items)
                {
                    if (item.id == hands.lefthandid)
                    {
                        Debug.Log(item.prefab.name);
                        GameObject obj = Instantiate(item.prefab, throwPoint.position, throwPoint.rotation);

                        Rigidbody rb = obj.GetComponent<Rigidbody>();

                        if (rb != null)
                        {
                            rb.isKinematic = false;
                            rb.useGravity = true;

                            rb.linearVelocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;

                            rb.collisionDetectionMode =
                                CollisionDetectionMode.Continuous;

                            Camera cam = Camera.main;

                            Ray ray = cam.ViewportPointToRay(
                                new Vector3(0.5f, 0.5f, 0f)
                            );

                            Vector3 throwDirection = ray.direction.normalized;

                            float throwForce = 8f;

                            rb.linearVelocity = throwDirection * throwForce;
                        }
                        hands.lefthandempty = true;
                        hands.lefthandid = 0;
                        inventoryhandsui ui = FindFirstObjectByType<inventoryhandsui>();
                        if (ui != null)
                        {
                            ui.ClearUILeft();
                        }
                    }
                }
            }
        }
    }
}
