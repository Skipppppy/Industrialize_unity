using UnityEngine;
using UnityEngine.UI;

public class inventoryhandsui : MonoBehaviour
{
    public Image rightHandImage;
    public Image leftHandImage;
    public Image rightHandCell;
    public Image leftHandCell;

    private playerinventoryhandsmanager hands;
    private handlogicanddrop logic;
    private void Start()
    {
        hands = FindFirstObjectByType<playerinventoryhandsmanager>();
    }

    public void UpdateUI()
    {
        if (hands == null)
            return;

        if (hands.righthandid != 0)
        {
            Sprite icon =
                Resources.Load<Sprite>("Items/" + hands.righthandid);

            if (icon != null)
            {
                rightHandImage.sprite = icon;
                rightHandImage.color = Color.white;
                rightHandCell.color = new Color(1f, 1f, 1f, 1f);
                Color c = rightHandImage.color;
                c.a = 1f;
                rightHandImage.color = c;
            }
        }

        if (hands.lefthandid != 0)
        {
            Sprite icon =
                Resources.Load<Sprite>("Items/" + hands.lefthandid);

            if (icon != null)
            {
                leftHandImage.sprite = icon;
                leftHandImage.color = Color.white;
                leftHandCell.color = new Color(1f, 1f, 1f, 1f);
                Color c = leftHandImage.color;
                c.a = 1f;
                leftHandImage.color = c;
            }
        }
    }
    public void ClearUIRight()
    {
        if (hands.righthandempty == true)
        rightHandImage.sprite = null;
        rightHandImage.color = new Color(1f, 1f, 1f, 0f);
        rightHandCell.color = new Color(1f, 1f, 1f, 0.5f);
    }
    public void ClearUILeft()
    {
        if (hands.lefthandempty == true)
        leftHandImage.sprite = null;
        leftHandImage.color = new Color(1f, 1f, 1f, 0f);
        leftHandCell.color = new Color(1f, 1f, 1f, 0.5f);
    }
}


        