using UnityEngine;

public class playerinventoryhandsmanager : MonoBehaviour
{
    public int IDitem = 0;
    public int amountitem = 0;
    public bool durabilityitem;
    public int durabilityitem_ = 0;
    //right hand
    public int righthandid = 0;
    public int amountright = 0;
    public bool durabilityright;
    public int durabilityitemright = 0;
    public bool righthandempty = true;
    //left hand
    public int lefthandid = 0;
    public int amountleft = 0;
    public bool durabilityleft;
    public int durabilityitemleft = 0;
    public bool lefthandempty = true;

    public void SetItem(itemdata data)
    {
        if (righthandid == 0)
        {
            righthandid = data.ItemType;
            amountright = data.Amount;
            durabilityright = data.Durability;
            durabilityitemright = (int)data.DurabilityItem;
            righthandempty = false;
            Debug.Log("исход1");
        }
        else if (righthandempty == false)
        {
            lefthandid = data.ItemType;
            amountleft = data.Amount;
            durabilityleft = data.Durability;
            durabilityitemleft = (int)data.DurabilityItem;
            lefthandempty = false;
            Debug.Log("исход2");
        }
        else if (righthandempty == false && lefthandempty == false)
        {
            return;
        }
    }
}