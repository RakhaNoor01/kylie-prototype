using UnityEngine;

public class MenuNavigator : MonoBehaviour
{
    [Header("Tombol (urutan: Play, Controls, Exit)")]
    public MenuButton[] buttons;

    private int _index   = 0;
    private bool _active = false;
    private MainMenuManager _mainMenu;

    private void Awake()
    {
        _mainMenu = GetComponent<MainMenuManager>();

        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            buttons[i].onHover = () => Select(idx);
            buttons[i].onClick = () => Confirm();
            buttons[i].SetSelected(false);
        }
    }

    public void Activate()
    {
        _active = true;
        _index  = 0;
        Refresh();
    }

    public void Deactivate()
    {
        _active = false;
        // #4: Reset semua tombol ke home, termasuk geseran kanan
        foreach (var b in buttons) b.ResetPosition();
    }

    private void Update()
    {
        if (!_active) return;
        if (_mainMenu.CurrentState != MainMenuManager.MenuState.MainMenu) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            _index = (_index - 1 + buttons.Length) % buttons.Length;
            Refresh();
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            _index = (_index + 1) % buttons.Length;
            Refresh();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            Confirm();
    }

    private void Select(int idx) { _index = idx; Refresh(); }

    private void Refresh()
    {
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].SetSelected(i == _index);
    }

    private void Confirm()
    {
        switch (_index)
        {
            case 0: _mainMenu.OnClickPlay();     break;
            case 1: _mainMenu.OnClickControls(); break;
            case 2: _mainMenu.OnClickExit();     break;
        }
    }
}