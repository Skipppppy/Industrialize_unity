using UnityEngine;
using System.Collections;

public class PickupItem : MonoBehaviour
{
    public float pickupDistance = 3f;
    public float flySpeed = 8f;
    public bool isPickingUp = false;
    public bool CanPickup => !isPickingUp;

    public void TryPickup(Transform player)
    {
        playerinventoryhandsmanager hands = player.GetComponent<playerinventoryhandsmanager>();

        if (hands != null &&
            hands.righthandid != 0 &&
            hands.lefthandid != 0)
        {
            Debug.Log("Обе руки заняты");
            return;
        }

        if (isPickingUp)
            return;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance <= pickupDistance)
        {
            StartCoroutine(FlyToPlayer(player));
        }
    }

    IEnumerator FlyToPlayer(Transform player)
    {
        playerinventoryhandsmanager hands = player.GetComponent<playerinventoryhandsmanager>();
        isPickingUp = true;

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        while (Vector3.Distance(transform.position, player.position) > 0.2f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                flySpeed * Time.deltaTime
            );
        if (hands.righthandempty == false && hands.lefthandempty == false)
        {
                RestoreItem();
            yield break;
        }
            yield return null;
        }

        itemdata data = GetComponent<itemdata>();

        if (hands != null && data != null)
        {
            hands.SetItem(data);
        }

        inventoryhandsui ui =
    FindFirstObjectByType<inventoryhandsui>();

        if (ui != null)
        {
            ui.UpdateUI();
        }

        {
            Destroy(gameObject);
        }
    }
    public void RestoreItem()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
            col.enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        isPickingUp = false;
    }
}