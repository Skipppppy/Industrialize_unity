using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;

    [Header("References")]
    //public voxelworld world;
    public Transform player;

    [Header("Save")]
    public string saveFileName = "save.json";

    private string SavePath
    {
        get
        {
            return Path.Combine(
                Application.persistentDataPath,
                saveFileName
            );
        }
    }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // =========================================================
    // SAVE
    // =========================================================

    //public void Save()
    //{
    //    if (world == null)
    //    {
    //        Debug.LogError(
    //            "SaveSystem: VoxelWorld is missing."
    //        );

    //        return;
    //    }

    //    WorldSaveData data =
    //        new WorldSaveData();

    //    // -----------------------------------------------------
    //    // VOXELS
    //    // -----------------------------------------------------

    //    //foreach (KeyValuePair<Vector3Int, VoxelMaterial> pair
    //    //         in world.GetVoxelModifications())
    //    //{
    //    //    VoxelSaveData voxel =
    //    //        new VoxelSaveData();

    //    //    voxel.x = pair.Key.x;
    //    //    voxel.y = pair.Key.y;
    //    //    voxel.z = pair.Key.z;

    //    //    voxel.material =
    //    //        (byte)pair.Value;

    //    //    data.voxels.Add(voxel);
    //    //}

    //    // -----------------------------------------------------
    //    // ITEMS
    //    // -----------------------------------------------------

    //    WorldItem[] items =
    //        FindObjectsByType<WorldItem>(
    //            FindObjectsInactive.Exclude,
    //            FindObjectsSortMode.None
    //        );

    //    foreach (WorldItem item in items)
    //    {
    //        if (item == null)
    //            continue;

    //        if (string.IsNullOrEmpty(
    //            item.prefabPath))
    //            continue;

    //        ItemSaveData itemData =
    //            new ItemSaveData();

    //        itemData.prefabPath =
    //            item.prefabPath;

    //        Vector3 position =
    //            item.transform.position;

    //        Vector3 rotation =
    //            item.transform.eulerAngles;

    //        itemData.px = position.x;
    //        itemData.py = position.y;
    //        itemData.pz = position.z;

    //        itemData.rx = rotation.x;
    //        itemData.ry = rotation.y;
    //        itemData.rz = rotation.z;

    //        data.items.Add(itemData);
    //    }

    //    // -----------------------------------------------------
    //    // PLAYER
    //    // -----------------------------------------------------

    //    if (player != null)
    //    {
    //        PlayerSaveData playerData =
    //            new PlayerSaveData();

    //        Vector3 position =
    //            player.position;

    //        Vector3 rotation =
    //            player.eulerAngles;

    //        playerData.px = position.x;
    //        playerData.py = position.y;
    //        playerData.pz = position.z;

    //        playerData.rx = rotation.x;
    //        playerData.ry = rotation.y;
    //        playerData.rz = rotation.z;

    //        data.player =
    //            playerData;
    //    }

    //    // -----------------------------------------------------
    //    // WRITE FILE
    //    // -----------------------------------------------------

    //    string json =
    //        JsonUtility.ToJson(
    //            data,
    //            true
    //        );

    //    try
    //    {
    //        File.WriteAllText(
    //            SavePath,
    //            json
    //        );

    //        Debug.Log(
    //            "World saved to:\n" +
    //            SavePath
    //        );

    //        Debug.Log(
    //            "Saved voxels: " +
    //            data.voxels.Count
    //        );

    //        Debug.Log(
    //            "Saved items: " +
    //            data.items.Count
    //        );
    //    }
    //    catch (System.Exception e)
    //    {
    //        Debug.LogError(
    //            "Save failed:\n" +
    //            e
    //        );
    //    }
    //}

    // =========================================================
    // LOAD
    // =========================================================

    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log(
                "No save file found."
            );

            return;
        }

        string json;

        try
        {
            json =
                File.ReadAllText(
                    SavePath
                );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Could not read save:\n" +
                e
            );

            return;
        }

        WorldSaveData data;

        try
        {
            data =
                JsonUtility.FromJson<WorldSaveData>(
                    json
                );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Save file is corrupted:\n" +
                e
            );

            return;
        }

        if (data == null)
            return;

        // -----------------------------------------------------
        // VOXELS
        // -----------------------------------------------------

        //world.ClearVoxelModifications();

        //foreach (VoxelSaveData voxel
        //         in data.voxels)
        //{
        //    Vector3Int position =
        //        new Vector3Int(
        //            voxel.x,
        //            voxel.y,
        //            voxel.z
        //        );

        //    world.SetVoxelModification(
        //        position,
        //        (VoxelMaterial)voxel.material
        //    );
        //}

        //world.ApplyVoxelModifications();

        // -----------------------------------------------------
        // ITEMS
        // -----------------------------------------------------

        ClearWorldItems();

        foreach (ItemSaveData item
                 in data.items)
        {
            GameObject prefab =
                Resources.Load<GameObject>(
                    item.prefabPath
                );

            if (prefab == null)
            {
                Debug.LogWarning(
                    "Could not load saved prefab: " +
                    item.prefabPath
                );

                continue;
            }

            Vector3 position =
                new Vector3(
                    item.px,
                    item.py,
                    item.pz
                );

            Quaternion rotation =
                Quaternion.Euler(
                    item.rx,
                    item.ry,
                    item.rz
                );

            Instantiate(
                prefab,
                position,
                rotation
            );
        }

        // -----------------------------------------------------
        // PLAYER
        // -----------------------------------------------------

        if (player != null &&
            data.player != null)
        {
            Vector3 position =
                new Vector3(
                    data.player.px,
                    data.player.py,
                    data.player.pz
                );

            Quaternion rotation =
                Quaternion.Euler(
                    data.player.rx,
                    data.player.ry,
                    data.player.rz
                );

            CharacterController controller =
                player.GetComponent<CharacterController>();

            if (controller != null)
                controller.enabled = false;

            player.position =
                position;

            player.rotation =
                rotation;

            if (controller != null)
                controller.enabled = true;
        }

        Debug.Log(
            "World loaded."
        );
    }

    // =========================================================
    // ITEMS
    // =========================================================

    private void ClearWorldItems()
    {
        WorldItem[] items =
            FindObjectsByType<WorldItem>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (WorldItem item in items)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
    }

    // =========================================================
    // DELETE
    // =========================================================

    public void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);

            Debug.Log(
                "Save deleted."
            );
        }
        else
        {
            Debug.Log(
                "Save file does not exist."
            );
        }
    }

    public string GetSavePath()
    {
        return SavePath;
    }
}