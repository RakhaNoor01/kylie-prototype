using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TarodevController;
using Unity.VisualScripting;

public enum ButtonType
{
    OneTime,
    Timed,
    Toggle
}

public class Button : MonoBehaviour
{
    public ButtonType type;
    public float duration = 2f;
    public bool state;

    [Header("Persistence")]
    [Tooltip("Set empty to not have persistent data")]
    public string buttonID;
    /* if true, button will not save persistent data automatically, 
    instead only doing so if the player collides with manualSaveState */
    public bool autoSaveState = true;
    public ExtraTags manualSaveState;

    [Header("Activation")]
    public bool onlyIfPlayerMoved;
    public bool boomerActivated = true;
    public bool playerActivated = false;
    [Tooltip("If not null, button will activated when colliding with this ExtraTag object")]
    public ExtraTags objectActivated;

    public List<ButtonTarget> buttonTargets = new List<ButtonTarget>();

    private bool _hasTriggered = false;
    private bool _timerRunning = false;

    public void RegisterTarget(ButtonTarget target)
    {
        if (!buttonTargets.Contains(target))
            buttonTargets.Add(target);
    }

    private void Start()
    {
        if (TempData.HasKey(buttonID))
        {
            state = (bool)TempData.GetValue(buttonID);
        }

        var buddy = (bool?)TempData.GetValue($"{buttonID}_trig") ?? false;

        if (buddy)
        {
            if (!string.IsNullOrEmpty(buttonID))
                _hasTriggered = buddy;
        }

        ApplyStateToTargets();
    }

    private void Update()
    {
        if (autoSaveState || manualSaveState == null) return;

        var col = manualSaveState.colInfo;
        if (col == null) return;

        if (col.gameObject.CompareTag("Player"))
        {
            SaveState();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (onlyIfPlayerMoved) return;

        Buttoner(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!PlayerController.Instance.FirstInput && onlyIfPlayerMoved) return;

        Buttoner(collision);
    }

    private void Buttoner(Collider2D collision)
    {
        if ((collision.gameObject.CompareTag("Goonerang") && boomerActivated) ||
            (collision.gameObject.CompareTag("Player") && playerActivated))
        {
            TriggerButton();
        }

        if (objectActivated == null) return;
        var gat = collision.gameObject.GetComponent<ExtraTags>();
        if (gat == objectActivated)
        {
            TriggerButton();
        }
    }

    public void TriggerButton()
    {
        switch (type)
        {
            case ButtonType.OneTime: HandleOneTime(); break;
            case ButtonType.Timed: HandleTimed(); break;
            case ButtonType.Toggle: HandleToggle(); break;
        }
    }

    private void HandleOneTime()
    {
        if (_hasTriggered) return;
        _hasTriggered = true;
        state = !state;
        ApplyStateToTargets();
    }

    private void HandleTimed()
    {
        if (_timerRunning) return;
        state = !state;
        ApplyStateToTargets();
        StartCoroutine(TimedRevert());
    }

    private IEnumerator TimedRevert()
    {
        _timerRunning = true;
        yield return new WaitForSeconds(duration);
        state = !state;
        ApplyStateToTargets();
        _timerRunning = false;
    }

    private void HandleToggle()
    {
        state = !state;
        ApplyStateToTargets();
    }

    private void ApplyStateToTargets()
    {
        foreach (var target in buttonTargets)
            target.SetState(state);

        if (autoSaveState)
            SaveState();
    }

    private void SaveState()
    {
        if (!string.IsNullOrEmpty(buttonID))
        {
            TempData.SetValue(buttonID, state);
        }

        if (type == ButtonType.OneTime && !string.IsNullOrEmpty(buttonID))
        {
            TempData.SetValue($"{buttonID}_trig", _hasTriggered);
        }
    }
}