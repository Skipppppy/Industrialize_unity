using UnityEngine;

public class uivisibility : MonoBehaviour
{
    public GameObject[] uiObjects;

    public CursorManager ui;

    public GameObject[] uiObjects_;
    void Start()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        foreach (GameObject obj in uiObjects)
        {
            obj.SetActive(ui.uiMode);
        }

        foreach (GameObject obj in uiObjects_)
        {
            obj.SetActive(!ui.uiMode);
        }
    }

    void Update()
    {
        UpdateUI();
    }
}