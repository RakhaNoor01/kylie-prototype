// SettingsSidebarNavigator.cs - Keyboard input dihapus
using UnityEngine;

/// <summary>
/// Pasang di GameObject sidebar Settings
/// HANYA mouse click, keyboard dihapus
/// </summary>
public class SettingsSidebarNavigator : MonoBehaviour
{
    [Header("Urutan HARUS: Audio, Controls, Miscellaneous, Back")]
    public MenuButton[] buttons;
    public SettingsMenuManager settingsMenu;

    private int _index = 0;

    private void Awake()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            buttons[i].onHover = () => Select(idx);
            buttons[i].onSubmit = () => { _index = idx; Confirm(); };
        }
    }

    public void ResetSelection(bool resetIndex)
    {
        if (resetIndex) _index = 0;
        Refresh();
    }

    public void Confirm()
    {
        if (_index == buttons.Length - 1) settingsMenu.Close();
        else settingsMenu.SwitchPanel(_index);
    }

    public void ClearHighlight()
    {
        foreach (var b in buttons) b.SetSelected(false);
    }

    private void Select(int idx)
    {
        _index = idx;
        Refresh();
        settingsMenu.OnSidebarFocused();
    }

    private void Refresh()
    {
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].SetSelected(i == _index);
    }
}