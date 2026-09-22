using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class TerminalConsole : MonoBehaviour
{
    [System.Serializable]
    public class BootLine
    {
        [TextArea(2, 5)]
        public string text;

        public float delay = 0.5f;
    }

    public float version = 0.1f;


    [Header("UI")]
    public TMP_Text outputText;
    public TMP_InputField inputField;


    [Header("Boot screen")]
    [TextArea(5, 20)]
    public string logo;


    public BootLine[] bootLines;



    [Header("Typing")]
    public float typingSpeed = 0.03f;


    [Header("Cursor")]
    public string cursor = "";
    public float cursorSpeed = 0.5f;



    private bool ready;

    private bool cursorVisible;

    private Coroutine cursorRoutine;



    private List<string> history = new();

    private int historyIndex;



    void Start()
    {
        inputField.gameObject.SetActive(false);

        StartCoroutine(Boot());
    }



    IEnumerator Boot()
    {

        outputText.text = "";


        if (!string.IsNullOrEmpty(logo))
        {
            yield return TypeLine(logo);
            outputText.text += "\n\n";
        }



        foreach (var line in bootLines)
        {
            yield return new WaitForSeconds(line.delay);

            yield return TypeLine(line.text);

            outputText.text += "\n";
        }



        outputText.text += "\nPLEASE WRITE help TO SEE LIST OF COMMANDS\n\n";


        ready = true;


        inputField.gameObject.SetActive(true);

        inputField.ActivateInputField();


        AddPrompt();


        cursorRoutine = StartCoroutine(CursorBlink());
    }



    IEnumerator TypeLine(string text)
    {
        foreach (char c in text)
        {
            outputText.text += c;

            yield return new WaitForSeconds(typingSpeed);
        }
    }



    IEnumerator CursorBlink()
    {
        while (true)
        {
            yield return new WaitForSeconds(cursorSpeed);


            if (cursorVisible)
            {
                RemoveCursor();
            }
            else
            {
                AddCursor();
            }


            cursorVisible = !cursorVisible;
        }
    }



    void AddPrompt()
    {
        outputText.text += ">";

        AddCursor();
    }



    void AddCursor()
    {
        outputText.text += cursor;
    }



    void RemoveCursor()
    {
        if (outputText.text.EndsWith(cursor))
        {
            outputText.text =
            outputText.text.Substring(
            0,
            outputText.text.Length - cursor.Length);
        }
    }




    public void EnterCommand()
    {
        RemoveCursor();


        string command =
        inputField.text.Trim();



        if (command == "")
            return;



        outputText.text += command + "\n";


        history.Add(command);

        historyIndex = history.Count;



        Execute(command);



        inputField.text = "";

        inputField.ActivateInputField();


        AddPrompt();
    }




    void Execute(string command)
    {

        string[] args =
        command.Split(' ');


        string cmd =
        args[0].ToLower();



        switch (cmd)
        {

            case "help":

                Print(
@"HELP

SCENE LIST
LOAD <SCENE>
CLEAR
VERSION
EXIT

");

                break;



            case "scene":

                if (args.Length > 1 &&
                args[1].ToLower() == "list")
                {
                    SceneList();
                }

                break;



            case "load":

                if (args.Length < 2)
                {
                    Print(
                    "LOAD NEEDS SCENE NAME\n\n");
                }
                else
                {
                    LoadScene(args[1]);
                }

                break;



            case "clear":

                outputText.text = "";

                break;



            case "version":

                Print(
                "INDUSTRIALIZE\n\n" + version);

                break;



            case "exit":

                Application.Quit();

                break;



            default:

                Print(
                "UNKNOWN COMMAND\n\n");

                break;

        }

    }




    void SceneList()
    {
        Print("AVAILABLE SCENES:\n");


        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {

            string path =
            SceneUtility.GetScenePathByBuildIndex(i);


            string name =
            System.IO.Path.GetFileNameWithoutExtension(path);


            Print(
            "- " + name + "\n");
        }


        Print("\n");
    }




    void LoadScene(string name)
    {

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {

            string path =
            SceneUtility.GetScenePathByBuildIndex(i);


            string sceneName =
            System.IO.Path.GetFileNameWithoutExtension(path);



            if (sceneName.ToLower() == name.ToLower())
            {
                SceneManager.LoadScene(i);
                return;
            }

        }


        Print(
        "SCENE NOT FOUND\n\n");
    }
    void Print(string text)
    {
        outputText.text += text;
    }
}