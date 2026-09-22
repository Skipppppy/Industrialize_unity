using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    public PlayerMovement player;

    public Vector3 standOffset;
    public Vector3 crawlOffset;
    public Vector3 fourLegOffset;
    public CameraHandheldSway sway;

    Vector3 currentOffset;
    void Start()
    {
        standOffset = new Vector3(0, 0.6f, 0.2f);
        crawlOffset = new Vector3(0, 0.2f, 0.2f);
        fourLegOffset = new Vector3(0, 0.3f, 0.2f);

        currentOffset = standOffset;
        transform.localPosition = currentOffset;
    }
    void Update()
    {
        UpdateOffset();
    }

    void UpdateOffset()
    {
        Vector3 targetOffset = standOffset;

        if (player.crawl)
            targetOffset = crawlOffset;
        else if (player.fourleg)
            targetOffset = fourLegOffset;

        currentOffset = Vector3.Lerp(
            currentOffset,
            targetOffset,
            10f * Time.deltaTime
        );

        transform.localPosition =
            currentOffset;
    }
}