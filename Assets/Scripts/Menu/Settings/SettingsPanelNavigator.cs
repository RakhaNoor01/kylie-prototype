// SettingsPanelNavigator.cs - FULL CODE
using UnityEngine;

public class SettingsPanelNavigator : MonoBehaviour
{
    public SettingsRow[] rows;
    public SettingsMenuManager settingsMenu;

    private int _index = 0;

    private void Awake()
    {
        Debug.Log($"[SettingsPanelNavigator] Awake dipanggil untuk {gameObject.name}");

        for (int i = 0; i < rows.Length; i++)
        {
            int idx = i;
            if (rows[i] != null)
            {
                Debug.Log($"[SettingsPanelNavigator] Setup row {i}: {rows[i].name}");
                rows[i].onHover = () => Select(idx);
                rows[i].onSubmit = () => 
                { 
                    Debug.Log($"[SettingsPanelNavigator] onSubmit row {idx} dipanggil!");
                    _index = idx; 
                    Confirm(); 
                };
            }
        }
    }

    public void ResetSelection()
    {
        _index = 0;
        Refresh();
        Debug.Log($"[SettingsPanelNavigator] ResetSelection: {gameObject.name}, index 0");
    }

    public void ClearHighlight()
    {
        Debug.Log($"[SettingsPanelNavigator] ClearHighlight: {gameObject.name}");
        foreach (var r in rows)
        {
            if (r != null)
            {
                r.SetHighlighted(false);
                r.SetHovered(false);
            }
        }
    }

    public void Confirm()
    {
        if (rows.Length > 0 && _index < rows.Length && rows[_index] != null)
        {
            Debug.Log($"[SettingsPanelNavigator] Confirm row {_index} ({rows[_index].name})");
            rows[_index].Confirm();
        }
    }

    public void Cancel() => settingsMenu.ReturnToSidebar();

    private void Select(int idx)
    {
        Debug.Log($"[SettingsPanelNavigator] Select row {idx}");
        _index = idx;
        Refresh();
        settingsMenu.OnPanelFocused(this);
    }

    private void Refresh()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] != null)
            {
                bool isCurrent = (i == _index);
                rows[i].SetHighlighted(isCurrent);
                rows[i].SetHovered(isCurrent);
            }
        }
    }
}