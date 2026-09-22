using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Texture2D cursorTexture;
    public bool uiMode = false;
    bool cursorUnlocked = false;
    public uivisibility ui;
    void Start()
    {
        LockCursor();
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            cursorUnlocked = !cursorUnlocked;

            if (cursorUnlocked)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }
    }

    void UnlockCursor()
    {
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;
        uiMode = true; 
    }

    void LockCursor()
    {
        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;
        uiMode = false;
    }
}