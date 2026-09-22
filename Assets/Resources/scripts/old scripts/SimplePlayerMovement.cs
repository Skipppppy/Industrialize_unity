using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float runSpeed = 9f;
    public float crawlSpeed = 1.5f;
    public float runcrawlSpeed = 4f;
    public float fourlegs = 3f;
    public float runfourlegs = 15f;

    float currentspeed;
    public Vector3 moveDirection;
    private Vector3 moveVelocity;

    public float gravity = -9.81f;
    public float groundOffset = -2f;

    public bool crawl = false;
    public bool fourleg = false;

    public CharacterController controller;
    public CursorManager ui;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        PoseOn();
        SpeedOn();
        MoveOn();
    }

    public void PoseOn()
    {
        if (!ui.uiMode)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                crawl = !crawl;
                if (fourleg) crawl = false;
            }
            else if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                fourleg = !fourleg;
                if (crawl) fourleg = false;
            }
        }
    }

    public void SpeedOn()
    {
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        float normalSpeed = walkSpeed;
        float sprintSpeed = runSpeed;

        if (crawl)
        {
            normalSpeed = crawlSpeed;
            sprintSpeed = runcrawlSpeed;
        }
        else if (fourleg)
        {
            normalSpeed = fourlegs;
            sprintSpeed = runfourlegs;
        }

        currentspeed = isRunning ? sprintSpeed : normalSpeed;
    }

    public void MoveOn()
    {
        float x = ui.uiMode ? 0 : Input.GetAxisRaw("Horizontal");
        float z = ui.uiMode ? 0 : Input.GetAxisRaw("Vertical");

        Vector3 targetMove =
            transform.right * x +
            transform.forward * z;

        targetMove = targetMove.normalized;

        moveDirection = Vector3.SmoothDamp(
            moveDirection,
            targetMove,
            ref moveVelocity,
            0.15f
        );

        controller.Move(moveDirection * currentspeed * Time.deltaTime);

        if (controller.isGrounded && gravity < 0)
        {
            Vector3 vel = new Vector3(0, groundOffset, 0);
            controller.Move(vel * Time.deltaTime);
        }

        Vector3 gravityMove = new Vector3(0, gravity, 0);
        controller.Move(gravityMove * Time.deltaTime);
    }
}