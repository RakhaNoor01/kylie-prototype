// MenuNavigator.cs - Keyboard input dihapus
using UnityEngine;

/// <summary>
/// Pasang di GameObject yang sama dengan MainMenuManager
/// HANYA untuk mouse hover/klik, keyboard sudah dihapus
/// </summary>
[RequireComponent(typeof(MainMenuManager))]
public class MenuNavigator : MonoBehaviour
{
    [Header("Tombol (urutan HARUS: Start Journey, Options, Credits, Quit Game)")]
    public MenuButton[] buttons;

    private int _index = 0;
    private MainMenuManager _mainMenu;

    private void Awake()
    {
        _mainMenu = GetComponent<MainMenuManager>();

        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogError("[MenuNavigator] Array 'buttons' kosong — isi di Inspector.", this);
            return;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            buttons[i].onHover = () => Select(idx);
            buttons[i].onSubmit = () => { _index = idx; Confirm(); };
        }
    }

    public void ResetSelection()
    {
        _index = 0;
        Refresh();
    }

    public void RestoreSelection()
    {
        Refresh();
    }

    public void Confirm()
    {
        switch (_index)
        {
            case 0: _mainMenu.OnClickPlay(); break;
            case 1: _mainMenu.OnClickOptions(); break;
            case 2: _mainMenu.OnClickCredits(); break;
            case 3: _mainMenu.OnClickExit(); break;
        }
    }

    private void Select(int idx)
    {
        _index = idx;
        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].SetSelected(i == _index);
    }
}