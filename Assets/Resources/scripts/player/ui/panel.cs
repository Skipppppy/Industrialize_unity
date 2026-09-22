using UnityEngine;
using UnityEngine.InputSystem;

public class uicontroller : MonoBehaviour
{
    [SerializeField] private CursorManager _cursorManager;
    [SerializeField] private GameObject[] _buttons;
    [SerializeField] private GameObject[] _panels;
    [SerializeField] private GameObject _tPanel;

    private void Start()
    {
        SetButtons(false);
        SetPanels(false);

        if (_tPanel != null)
        {
            _tPanel.SetActive(false);
        }
    }

    private void Update()
    {
        bool _uiMode = _cursorManager.uiMode;

        SetButtons(_uiMode);

        if (!_uiMode)
        {
            SetPanels(false);

            if (_tPanel != null)
            {
                _tPanel.SetActive(false);
            }

            return;
        }

        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame && _cursorManager.uiMode)
        {
            if (_tPanel != null)
            {
                _tPanel.SetActive(!_tPanel.activeSelf);
            }
        }
    }

    private void SetButtons(bool _active)
    {
        for (int _i = 0; _i < _buttons.Length; _i++)
        {
            if (_buttons[_i] != null)
            {
                _buttons[_i].SetActive(_active);
            }
        }
    }

    private void SetPanels(bool _active)
    {
        for (int _i = 0; _i < _panels.Length; _i++)
        {
            if (_panels[_i] != null)
            {
                _panels[_i].SetActive(_active);
            }
        }
    }

    public void OpenPanel(int _index)
    {
        if (!_cursorManager.uiMode)
        {
            return;
        }

        if (_index < 0 || _index >= _panels.Length)
        {
            return;
        }

        if (_panels[_index] != null)
        {
            _panels[_index].SetActive(true);
        }
    }

    public void ClosePanel(int _index)
    {
        if (_index < 0 || _index >= _panels.Length)
        {
            return;
        }

        if (_panels[_index] != null)
        {
            _panels[_index].SetActive(false);
        }
    }
}