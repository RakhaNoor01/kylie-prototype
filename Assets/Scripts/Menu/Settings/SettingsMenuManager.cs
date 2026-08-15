// SettingsMenuManager.cs - FULL CODE (dengan debug tambahan)
using UnityEngine;

public class SettingsMenuManager : MonoBehaviour
{
    [Header("Sidebar")]
    public SettingsSidebarNavigator sidebar;

    [Header("Panels")]
    public GameObject panelAudio;
    public GameObject panelControls;
    public GameObject panelMisc;
    public SettingsPanelNavigator navAudio;
    public SettingsPanelNavigator navControls;
    public SettingsPanelNavigator navMisc;

    [Header("Popup")]
    public ConfirmPopupController confirmPopup;

    [Header("Integrasi")]
    public MainMenuManager mainMenu;

    private GameObject[] _panelObjs;
    private SettingsPanelNavigator[] _panelNavs;
    private SettingsPanelNavigator _activePanelNav;

    private void Awake()
    {
        Debug.Log("[SettingsMenuManager] Awake dipanggil!");
        
        if (sidebar == null) sidebar = GetComponentInChildren<SettingsSidebarNavigator>();
        if (confirmPopup == null) confirmPopup = GetComponentInChildren<ConfirmPopupController>();
        if (mainMenu == null) mainMenu = GetComponentInParent<MainMenuManager>();

        if (panelAudio == null) panelAudio = FindPanelByName("Panel_Audio");
        if (panelControls == null) panelControls = FindPanelByName("Panel_Controls");
        if (panelMisc == null) panelMisc = FindPanelByName("Panel_Misc");

        if (navAudio == null) navAudio = FindNavByName("Panel_Audio");
        if (navControls == null) navControls = FindNavByName("Panel_Controls");
        if (navMisc == null) navMisc = FindNavByName("Panel_Misc");

        _panelObjs = new[] { panelAudio, panelControls, panelMisc };
        _panelNavs = new[] { navAudio, navControls, navMisc };

        bool hasError = false;
        for (int i = 0; i < _panelObjs.Length; i++)
        {
            if (_panelObjs[i] == null)
            {
                Debug.LogError($"[SettingsMenuManager] Panel {i} tidak ditemukan!", this);
                hasError = true;
            }
        }

        if (hasError)
        {
            for (int i = 0; i < _panelObjs.Length; i++)
            {
                if (_panelObjs[i] == null)
                {
                    _panelObjs[i] = new GameObject($"Panel_{i}_Dummy");
                    _panelObjs[i].transform.SetParent(transform);
                    _panelObjs[i].SetActive(false);
                }
            }
        }
    }

    private GameObject FindPanelByName(string panelName)
    {
        Transform child = transform.Find(panelName);
        if (child != null) return child.gameObject;

        foreach (Transform t in transform.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == panelName && t != transform)
            {
                if (t.parent == transform || t.parent.parent == transform)
                    return t.gameObject;
            }
        }
        return null;
    }

    private SettingsPanelNavigator FindNavByName(string panelName)
    {
        GameObject panel = FindPanelByName(panelName);
        if (panel != null)
        {
            SettingsPanelNavigator nav = panel.GetComponent<SettingsPanelNavigator>();
            if (nav == null) nav = panel.GetComponentInChildren<SettingsPanelNavigator>();
            return nav;
        }
        return null;
    }

    public void Open()
    {
        Debug.Log("[SettingsMenuManager] Open dipanggil!");
        
        if (_panelObjs == null) return;

        for (int i = 0; i < _panelObjs.Length; i++)
        {
            if (_panelObjs[i] != null)
                _panelObjs[i].SetActive(i == 0);
        }

        _activePanelNav = null;
        if (_panelNavs != null)
        {
            foreach (var nav in _panelNavs)
            {
                if (nav != null) nav.ClearHighlight();
            }
        }

        if (sidebar != null)
        {
            sidebar.ResetSelection(resetIndex: true);
        }
    }

    public void SwitchPanel(int index)
    {
        Debug.Log($"[SettingsMenuManager] SwitchPanel: {index}");
        
        if (_panelObjs == null || index < 0 || index >= _panelObjs.Length) return;

        for (int i = 0; i < _panelObjs.Length; i++)
        {
            if (_panelObjs[i] != null)
                _panelObjs[i].SetActive(i == index);
        }

        if (sidebar != null) sidebar.ClearHighlight();

        if (_panelNavs != null && index < _panelNavs.Length)
        {
            _activePanelNav = _panelNavs[index];
            if (_activePanelNav != null)
            {
                _activePanelNav.ResetSelection();
            }
        }
    }

    public void ReturnToSidebar()
    {
        Debug.Log("[SettingsMenuManager] ReturnToSidebar dipanggil!");
        
        if (_activePanelNav != null) _activePanelNav.ClearHighlight();
        _activePanelNav = null;

        if (sidebar != null)
        {
            sidebar.ResetSelection(resetIndex: false);
        }
    }

    public void OnSidebarFocused()
    {
        Debug.Log("[SettingsMenuManager] OnSidebarFocused dipanggil!");
        
        if (_activePanelNav != null) _activePanelNav.ClearHighlight();
        _activePanelNav = null;
    }

    public void OnPanelFocused(SettingsPanelNavigator nav)
    {
        Debug.Log($"[SettingsMenuManager] OnPanelFocused: {nav.name}");
        
        if (sidebar != null) sidebar.ClearHighlight();
        _activePanelNav = nav;
    }

    public void Close()
    {
        Debug.Log("[SettingsMenuManager] Close dipanggil!");
        
        ApplyAllChanges();
        FinishClose();
    }

    public void RequestResetAllPopup()
    {
        Debug.Log("[SettingsMenuManager] RequestResetAllPopup dipanggil!");
        
        if (confirmPopup == null)
        {
            Debug.LogError("[SettingsMenuManager] confirmPopup is NULL!");
            return;
        }

        Debug.Log("[SettingsMenuManager] confirmPopup ditemukan, menampilkan popup...");
        
        SettingsPanelNavigator returnNav = _activePanelNav;
        
        confirmPopup.Show(
            "Reset all Setting changes?",
            onYes: () => 
            { 
                Debug.Log("[SettingsMenuManager] Reset ALL - YES!");
                ResetAllToDefault();
                if (returnNav != null)
                {
                    returnNav.ResetSelection();
                }
            },
            onNo: () => 
            { 
                Debug.Log("[SettingsMenuManager] Reset ALL - NO (dibatalkan)");
                if (returnNav != null)
                {
                    returnNav.ResetSelection();
                }
            }
        );
    }

    private void ApplyAllChanges()
    {
        SettingsRow[] allRows = GetComponentsInChildren<SettingsRow>(true);
        foreach (var row in allRows)
        {
            row?.ApplyChanges();
        }
        Debug.Log("[SettingsMenuManager] Auto-save: semua perubahan di-apply");
    }

    private void ResetAllToDefault()
    {
        SettingsRow[] allRows = GetComponentsInChildren<SettingsRow>(true);
        foreach (var row in allRows)
        {
            row?.ResetToDefault();
        }
        Debug.Log("[SettingsMenuManager] Semua setting di-reset ke default");
    }

    private void FinishClose()
    {
        if (_activePanelNav != null) _activePanelNav.ClearHighlight();
        _activePanelNav = null;
        if (mainMenu != null) mainMenu.CloseSettings();
    }
}