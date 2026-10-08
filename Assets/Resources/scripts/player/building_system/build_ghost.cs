using UnityEngine;
public class buildingghost : MonoBehaviour
{
    private buildingdefinition _building;
    private buildingmanager _manager;
    private float _progress;
    public void initialize(buildingdefinition _definition, buildingmanager _buildingmanager)
    {
        _building = _definition;
        _manager = _buildingmanager;
        _progress = 0f;
    }
    public buildingdefinition getbuilding()
    {
        return _building;
    }
    public buildingmanager getmanager()
    {
        return _manager;
    }
    public float getprogress()
    {
        return _progress;
    }
    public void addprogress(float _value)
    {
        _progress += _value;
    }
    public void resetprogress()
    {
        _progress = 0f;
    }
}
