//using UnityEngine;
//using UnityEngine.InputSystem;

//public class playerreferences : MonoBehaviour
//{
//    public static playerreferences instance;

//    [Header("References")]
//    public Transform player;
//    public Camera playercamera;
//    public PlayerInput playerinput;
//    public CharacterController charactercontroller;
//    public PlayerController playercontroller;
//    public CursorManager cursormanager;
//    public PlayerDragging playerdragging;
//    public EasyInv easyinv;

//    private void Awake()
//    {
//        instance = this;

//        player = transform;

//        if (playercamera == null)
//            playercamera = GetComponentInChildren<Camera>(true);

//        if (playerinput == null)
//            playerinput = GetComponent<PlayerInput>();

//        if (charactercontroller == null)
//            charactercontroller = GetComponent<CharacterController>();

//        if (playercontroller == null)
//            playercontroller = GetComponent<PlayerController>();

//        if (cursormanager == null)
//            cursormanager = GetComponentInChildren<CursorManager>(true);

//        if (playerdragging == null)
//            playerdragging = GetComponent<PlayerDragging>();

//        if (easyinv == null)
//            easyinv = GetComponent<EasyInv>();

//        DontDestroyOnLoad(gameObject);
//    }

//    private void OnDestroy()
//    {
//        if (instance == this)
//            instance = null;
//    }
//}