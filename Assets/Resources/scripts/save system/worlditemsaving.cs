using UnityEngine;

public class WorldItem : MonoBehaviour
{
    [Tooltip("Путь относительно Resources")]
    public string prefabPath;

    private void Awake()
    {
        if (string.IsNullOrEmpty(prefabPath))
        {
            string objectName =
                gameObject.name.Replace("(Clone)", "");

            prefabPath =
                "prefabs/" + objectName;
        }
    }
}