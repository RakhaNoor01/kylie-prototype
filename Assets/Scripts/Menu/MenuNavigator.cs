using UnityEngine;

public class MenuNavigator : MonoBehaviour
{
    [Header("Tombol (urutan: Play, Controls, Credit, Exit)")]
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
        // Visual tombol SENGAJA tidak direset di sini — dibiarkan apa adanya
        // (mis. tombol Play yang lagi hover/scaled) supaya ikut ke-slide keluar
        // bareng panel Main Menu. Kalau direset instan di sini, tombol yang lagi
        // membesar (hover) bakal "snap" balik ke ukuran normal dalam 1 frame
        // sebelum panel sempat bergerak — kelihatan seperti blink/bug.
        // Panggil ResetButtonsVisual() setelah Main Menu benar-benar invisible.
    }

    /// <summary>
    /// Reset semua tombol ke kondisi normal (scale, sprite icon, dsb).
    /// Panggil ini SETELAH groupMainMenu di-GroupOff (sudah invisible),
    /// supaya tidak ada snap instan yang kelihatan pemain.
    /// </summary>
    public void ResetButtonsVisual()
    {
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
            case 2: _mainMenu.OnClickCredits();  break;
            case 3: _mainMenu.OnClickExit();     break;
        }
    }
}