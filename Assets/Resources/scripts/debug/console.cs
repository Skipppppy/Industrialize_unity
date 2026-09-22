using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class DevConsole : MonoBehaviour
{
    [Header("References")]
    //public voxelworld world;
    public Transform player;
    public Transform spawnPoint;

    [Header("UI")]
    public GameObject consolePanel;
    public TMP_InputField inputField;
    public TMP_Text outputText;

    [Header("Settings")]
    public Key toggleKey = Key.LeftCtrl;

    private readonly List<string> log =
        new List<string>();

    private bool opened;

    private void Start()
    {
        SetConsole(false);

        Print("Developer Console");
        Print("Type 'help' for commands.");
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            ToggleConsole();
        }

        if (!opened)
            return;

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            ExecuteInput();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetConsole(false);
        }
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    private void ToggleConsole()
    {
        SetConsole(!opened);
    }

    private void SetConsole(bool state)
    {
        opened = state;

        if (consolePanel != null)
            consolePanel.SetActive(state);

        if (state)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (inputField != null)
            {
                inputField.ActivateInputField();
                inputField.Select();
            }
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void ExecuteInput()
    {
        if (inputField == null)
            return;

        string command =
            inputField.text.Trim();

        if (string.IsNullOrEmpty(command))
            return;

        Print("> " + command);

        Execute(command);

        inputField.text = "";

        inputField.ActivateInputField();
    }

    // =========================================================
    // COMMANDS
    // =========================================================

    private void Execute(string commandLine)
    {
        string[] args =
            commandLine.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries
            );

        if (args.Length == 0)
            return;

        string command =
            args[0].ToLower();

        switch (command)
        {
            case "help":
                Help();
                break;

            //case "save":
            //    Save();
            //    break;

            //case "load":
            //    Load();
            //    break;

            //case "clear_save":
            //    ClearSave();
            //    break;

            case "spawn":
                Spawn(args);
                break;

            case "spawnpos":
                SpawnPos(args);
                break;

            case "clear":
                Clear();
                break;

            default:
                Print(
                    "Unknown command: " +
                    command
                );
                break;
        }
    }

    // =========================================================
    // HELP
    // =========================================================

    private void Help()
    {
        Print("Commands:");

        Print("help");
        Print("save");
        Print("load");
        Print("clear_save");

        Print("spawn <prefab> [amount]");
        Print("spawnpos <prefab> [amount]");

        Print("clear");
    }

    // =========================================================
    // SAVE
    // =========================================================

    //private void Save()
    //{
    //    if (world == null)
    //    {
    //        Print("ERROR: VoxelWorld missing.");
    //        return;
    //    }

    //    world.SaveWorld();

    //    Print("World saved.");
    //}

    // =========================================================
    // LOAD
    // =========================================================

    //private void Load()
    //{
    //    if (world == null)
    //    {
    //        Print("ERROR: VoxelWorld missing.");
    //        return;
    //    }

    //    world.LoadWorld();

    //    Print("World loaded.");
    //}

    // =========================================================
    // CLEAR SAVE
    // =========================================================

    //private void ClearSave()
    //{
    //    if (world == null)
    //    {
    //        Print("ERROR: VoxelWorld missing.");
    //        return;
    //    }

    //    world.DeleteSave();

    //    Print("Save deleted.");
    //}

    // =========================================================
    // SPAWN
    // =========================================================

    private void Spawn(string[] args)
    {
        if (args.Length < 2)
        {
            Print(
                "Usage: spawn <prefab> [amount]"
            );

            return;
        }

        string prefabName = args[1];

        int amount = 1;

        if (args.Length >= 3)
        {
            if (!int.TryParse(
                args[2],
                out amount))
            {
                Print("Invalid amount.");
                return;
            }
        }

        SpawnPrefab(
            prefabName,
            amount,
            GetSpawnPosition()
        );
    }

    private void SpawnPos(string[] args)
    {
        if (args.Length < 2)
        {
            Print(
                "Usage: spawnpos <prefab> [amount]"
            );

            return;
        }

        string prefabName = args[1];

        int amount = 1;

        if (args.Length >= 3)
        {
            if (!int.TryParse(
                args[2],
                out amount))
            {
                Print("Invalid amount.");
                return;
            }
        }

        if (spawnPoint == null)
        {
            Print(
                "ERROR: SpawnPoint missing."
            );

            return;
        }

        SpawnPrefab(
            prefabName,
            amount,
            spawnPoint.position
        );
    }

    private void SpawnPrefab(
        string prefabName,
        int amount,
        Vector3 position)
    {
        GameObject prefab =
            Resources.Load<GameObject>(
                "prefabs/" + prefabName
            );

        if (prefab == null)
        {
            Print(
                "Prefab not found: " +
                "prefabs/" +
                prefabName
            );

            return;
        }

        amount =
            Mathf.Clamp(
                amount,
                1,
                100
            );

        for (int i = 0; i < amount; i++)
        {
            Vector3 offset =
                UnityEngine.Random.insideUnitSphere *
                0.5f;

            offset.y =
                Mathf.Abs(offset.y);

            Instantiate(
                prefab,
                position + offset,
                Quaternion.identity
            );
        }

        Print(
            "Spawned " +
            amount +
            "x " +
            prefabName
        );
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoint != null)
            return spawnPoint.position;

        if (player != null)
        {
            return player.position +
                   player.forward * 2f;
        }

        return Vector3.zero;
    }

    // =========================================================
    // LOG
    // =========================================================

    private void Print(string message)
    {
        log.Add(message);

        if (log.Count > 50)
            log.RemoveAt(0);

        if (outputText != null)
        {
            outputText.text =
                string.Join(
                    "\n",
                    log
                );
        }

        Debug.Log(
            "[DEV] " +
            message
        );
    }

    private void Clear()
    {
        log.Clear();

        if (outputText != null)
            outputText.text = "";
    }
}