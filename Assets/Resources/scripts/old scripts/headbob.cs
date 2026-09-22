using UnityEngine;
public class headbob : MonoBehaviour
{
    void Start()
    {
        playermovement = GetComponent<PlayerMovement>();
    }
    public PlayerMovement playermovement;
    // headbob
    // walk
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.03f;
    // run
    public float runBobSpeed = 20f;
    public float runBobAmount = 0.06f;
    // crawl
    public float crawlBobSpeed = 8f;
    public float crawlBobAmount = 0.015f;
    // smooth
    public float bobSmooth = 10f;
    private float bobTimer;
    public Vector3 bobOffset;
    public CursorManager ui;
    public Vector3 GetBobOffset()
    {
        return bobOffset;
    }
    public void HeadBob()
    {
        if (!ui.uiMode)
        {
            bool isMoving =
                Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0 ||
                Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0;

            bool isRunning = Input.GetKey(KeyCode.LeftShift);

            if (isMoving && playermovement.controller.isGrounded)
            {
                float currentBobSpeed = walkBobSpeed;
                float currentBobAmount = walkBobAmount;

                // crawl
                if (playermovement.crawl)
                {
                    currentBobSpeed = crawlBobSpeed;
                    currentBobAmount = crawlBobAmount;
                }

                // run
                else if (isRunning)
                {
                    currentBobSpeed = runBobSpeed;
                    currentBobAmount = runBobAmount;
                }

                bobTimer += Time.deltaTime * currentBobSpeed;

                Vector3 targetBob = new Vector3(
                    Mathf.Cos(bobTimer * 0.5f) *
                    currentBobAmount * 0.5f,

                    Mathf.Sin(bobTimer) *
                    currentBobAmount,

                    0
                );

                bobOffset = Vector3.Lerp(
                    bobOffset,
                    targetBob,
                    bobSmooth * Time.deltaTime
                );
            }
            else
            {
                bobTimer = 0;

                bobOffset = Vector3.Lerp(
                    bobOffset,
                    Vector3.zero,
                    bobSmooth * Time.deltaTime
                );
            }
        }
    }
    void Update()
    {
        HeadBob();
    }
}
