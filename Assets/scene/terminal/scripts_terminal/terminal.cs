using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.Burst.Intrinsics.X86.Avx;
public class terminalConsole : MonoBehaviour
{
    [System.Serializable]
    public class BootLine
    {
        [TextArea(1, 5)]
        public string text;
        [Min(0)]
        public float delay = 0.5f;
    }

    private Dictionary<string, Action<string[]>> commands;

    [Header("UI")]
    public TMP_Text terminalText;

    [Header("Boot")]
    [TextArea(10, 40)]
    public string logo;
    public BootLine[] bootSequence;
    public bool printLogo = true;
    public float typingSpeed = 0.015f;

    [Header("Terminal")]
    public string prompt = "> ";
    public string cursor = "_";
    public float cursorBlinkSpeed = 0.5f;
    public int maxVisibleLines = 28;
    private int scrollOffset = 0;
    public int maxInputLength = 116;

    [Header("Settings")]
    public bool uppercaseCommands = true;
    private readonly List<string> console = new();
    private readonly List<string> history = new();
    private int historyIndex;
    private string currentInput = "";
    private bool cursorVisible = true;
    private float cursorTimer;
    private bool bootFinished = false;

    [Header("Audio")]
    public AudioSource Ambient;
    public AudioSource Audio;
    public AudioClip[] typingSounds;
    public AudioClip floppy;
    public AudioClip bootSounds;
    public AudioClip idle;
    public AudioClip error;
    public int bootSoundInterval = 3;
    public int bootCounter;
    [Range(0, 1)]
    public float typingVolume = 0.3f;
    [Range(0, 1)]
    public float bootVolume = 0.35f;
    [Range(0, 1)]
    public float backgroundVolume = 0.2f;
    [Range(0, 1)]
    public float errorVolume = 0.2f;

    [Header("Command Output")]
    public float commandLineDelay = 0.05f;
    private Coroutine bootRoutine;
    private bool isPrintingCommand = false;

    void Update()
    {
        if (!bootFinished)
            return;
        UpdateCursor();
        ReadKeyboard();
        RefreshConsole();
        Scroll();

    }
    void Start()
    {
        InitializeCommands();
        terminalText.text = "";
        bootRoutine = StartCoroutine(BootSequence());
        Ambient.clip = idle;
        Ambient.loop = true;
        Ambient.volume = backgroundVolume;
        Ambient.Play();
    }
    private void InitializeCommands()
    {
        commands = new Dictionary<string, Action<string[]>>(
            StringComparer.OrdinalIgnoreCase
        );

        commands["help"] = args => CommandHelp();
        commands["clear"] = args => CommandClear();
        commands["cls"] = args => CommandClear();
        commands["version"] = args => CommandVersion();
        commands["exit"] = args => CommandExit();
        commands["settings"] = args => CommandSettings();
        commands["load"] = args => CommandLoad(args);
        commands["scene"] = args => CommandScene(args);
        commands["time"] = args => CommandTime();
        //commands["tutorial"] = args => CommandLoad(new string[] { "load", "Tutorial" });
        commands["info"] = args => CommandInfo();
    }
    void Scroll()
    {
        float wheel = Input.mouseScrollDelta.y;

        if (wheel > 0)
        {
            scrollOffset = Mathf.Clamp(
                scrollOffset + 1,
                0,
                Mathf.Max(0, console.Count - (maxVisibleLines - 1))
            );
        }

        if (wheel < 0)
        {
            scrollOffset = Mathf.Clamp(
                scrollOffset - 1,
                0,
                console.Count
            );
        }
    }
    IEnumerator BootSequence()
    {
        console.Clear();
        if (printLogo && !string.IsNullOrWhiteSpace(logo))
        {
            yield return StartCoroutine(TypeLine(logo));
            console.Add("");
            RefreshConsole();
        }
        foreach (BootLine line in bootSequence)
        {
            yield return new WaitForSeconds(line.delay);
            yield return StartCoroutine(TypeLine(line.text));
        }
        bootFinished = true;
        RefreshConsole();
    }
    IEnumerator TypeLine(string text)
    {
        string[] lines = text
            .Replace("\r", "")
            .Split('\n');


        foreach (string line in lines)
        {
            console.Add("");

            for (int i = 0; i < line.Length; i++)
            {
                console[console.Count - 1] += line[i];
                bootCounter++;
                if (bootCounter >= bootSoundInterval)
                {
                    Audio.PlayOneShot(bootSounds, bootVolume);
                    bootCounter = 0;
                }
                RefreshConsole();
                yield return new WaitForSeconds(typingSpeed);
            }
        }
    }
    void RefreshConsole()
    {
        System.Text.StringBuilder builder = new();
        int historyLines = maxVisibleLines - 1;
        int start = Mathf.Max(
            0,
            console.Count - historyLines - scrollOffset
        );
        int end = Mathf.Min(
            console.Count,
            start + historyLines
        );
        for (int i = start; i < end; i++)
        {
            builder.AppendLine(console[i]);
        }
        int emptyLines = historyLines - (end - start);

        for (int i = 0; i < emptyLines; i++)
        {
            builder.AppendLine("");
        }
        if (bootFinished)
        {
            builder.Append(prompt);
            builder.Append(currentInput);

            if (cursorVisible)
                builder.Append(cursor);
        }
        terminalText.text = builder.ToString();
    }
    void UpdateCursor()
    {
        cursorTimer += Time.deltaTime;
        if (cursorTimer >= cursorBlinkSpeed)
        {
            cursorTimer = 0;
            cursorVisible = !cursorVisible;
        }
    }

    void ReadKeyboard()
    {

        if (!Input.anyKeyDown)
            return;

        foreach (char c in Input.inputString)
        {
            switch (c)
            {
                case '\b':
                    if (currentInput.Length > 0)
                    {
                        currentInput = currentInput.Substring(0, currentInput.Length - 1);
                        PlayTypingSound();
                    }
                    else
                    {
                        playerrorsound();
                    }
                    break;

                case '\n':
                case '\r':
                    PlayTypingSound();
                    SubmitCommand();
                    break;

                default:
                    if (!char.IsControl(c))
                    {
                        if ((c >= 'А' && c <= 'я') || c == 'ё' || c == 'Ё')
                            continue;
                        if (currentInput.Length < maxInputLength)
                        {
                            currentInput += c;
                            PlayTypingSound();
                        }
                        else
                        {
                            playerrorsound();
                        }

                    }
                    break;
            }
        }
        HandleHistory();
    }
    void HandleHistory()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (history.Count == 0)
                return;
            historyIndex--;
            if (historyIndex < 0)
                historyIndex = 0;
            currentInput = history[historyIndex];
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (history.Count == 0)
                return;

            historyIndex++;

            if (historyIndex >= history.Count)
            {
                historyIndex = history.Count;
                currentInput = "";
            }
            else
            {
                currentInput = history[historyIndex];
            }
        }
    }
    void SubmitCommand()
    {
        if (!isPrintingCommand)
        {
        scrollOffset = 0;
        string cmd = currentInput.Trim();
        console.Add(prompt + cmd);
        if (!string.IsNullOrWhiteSpace(cmd))
        {
            history.Add(cmd);
            historyIndex = history.Count;
            ExecuteCommand(cmd);
        }
        currentInput = "";
        }

    }
    void ExecuteCommand(string command)
    {
        string[] args = command.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries
        );

        if (args.Length == 0)
            return;

        string cmd = args[0];

        if (commands.TryGetValue(cmd, out Action<string[]> action))
        {
            action(args);
        }
        else
        {
            StartCoroutine(PrintCommandOutput(
            "Unknown command: " + cmd,
            "Type HELP"));
            playerrorsound();
        }

        RefreshConsole();
    }
    void CommandHelp()
    {
        StartCoroutine(PrintCommandOutput(
        "",
        "AVAILABLE COMMANDS",
        "------------------------------",
        "help",
        "clear",
        "cls",
        "version",
        "scene list",
        "load <scene>",
        "settings",
        "exit",
        "time",
        "info",
        ""));
    }
    void CommandVersion()
    {
        StartCoroutine(PrintCommandOutput(
        "",
        "Industrialize",
        "Version" + Application.version,
        "Unity " + Application.unityVersion,
        ""));
    }
    void CommandClear()
    {
        console.Clear();
    }
    void CommandExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
    void CommandTime()
    {
        StartCoroutine(PrintCommandOutput(
        DateTime.Now.ToString("HH:mm:ss")));
    }
    void CommandSettings()
    {
        StartCoroutine(PrintCommandOutput(
        "",
        "Settings are not implemented",
        ""));
        playerrorsound();
    }
    void CommandLoad(string[] args)
    {
        if (args.Length < 2)
        {
            StartCoroutine(PrintCommandOutput(
            "Usage:",
            "LOAD <Scene>"));
            return;
        }
        string sceneName = args[1];
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path =
                SceneUtility.GetScenePathByBuildIndex(i);
            string name =
                Path.GetFileNameWithoutExtension(path);

            if (name.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
            {
                StartCoroutine(LoadsSceneRoutine(i));
                return;
            }
        }
        StartCoroutine(PrintCommandOutput(
        "Scene not found"));
    }
    void CommandScene(string[] args)
    {
        if (args.Length < 2)
        {
            StartCoroutine(PrintCommandOutput(
            "Usage:",
            "SCENE LIST"));
            return;
        }
        if (!args[1].Equals("LIST", StringComparison.OrdinalIgnoreCase))
        {
            StartCoroutine(PrintCommandOutput(
            "Unknown parameter"));
            playerrorsound();
            return;
        }
        StartCoroutine(PrintCommandOutput(
        "",
        "Scenes:",
        "----------------"));

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path =
                SceneUtility.GetScenePathByBuildIndex(i);
            string name =
                Path.GetFileNameWithoutExtension(path);
            console.Add(name);
        }
        console.Add("");
    }
    void CommandInfo()
    {
        StartCoroutine(PrintCommandOutput(
        "",
        "this game has its own discord channel and telegram.",
        "the game is currently in prototype form, and only a tutorial is available(no)",
        "stay tuned for more information",
        "discord > https://discord.gg/D2j6fjhcNv",
        "telegram > t.me/KauchukSintetichiskiy",
        ""));
    }
    public void PlayTypingSound()
    {
        if (typingSounds == null || typingSounds.Length == 0)
            return;
        Audio.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
        Audio.PlayOneShot(typingSounds[UnityEngine.Random.Range(0, typingSounds.Length)], typingVolume);
    }
    IEnumerator LoadsSceneRoutine(int index)
    {
        if (floppy != null)
        {
        Audio.PlayOneShot(floppy);
        yield return new WaitForSeconds(floppy.length);
        }
        SceneManager.LoadScene(index);
    }
    IEnumerator PrintCommandOutput(params string[] lines)
    {
        isPrintingCommand = true;

        foreach (string line in lines)
        {
            console.Add(line);

            RefreshConsole();

            yield return new WaitForSeconds(commandLineDelay);
        }

        isPrintingCommand = false;
    }
    void playerrorsound()
    {
        if (error == null)
            return;
        Audio.pitch = 1f;
        Audio.PlayOneShot(error, errorVolume);
    }
}
