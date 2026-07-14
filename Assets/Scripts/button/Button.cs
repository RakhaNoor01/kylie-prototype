using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TarodevController;

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
    public bool onlyIfPlayerMoved = false;
    public bool blockableByTargets = true;
    public bool boomerActivated = true;
    public bool playerActivated = false;
    [Tooltip("If not null, button will activated when colliding with this ExtraTag object")]
    public ExtraTags objectActivated;

    public List<ButtonTarget> buttonTargets = new List<ButtonTarget>();

    private bool hasTriggered = false;
    public bool timerRunning = false;

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
                hasTriggered = buddy;
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

        Buttoner(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!PlayerController.Instance.FirstInput && onlyIfPlayerMoved) return;
        if (type == ButtonType.Toggle) return;

        Buttoner(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (onlyIfPlayerMoved) return;

        Buttoner(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!PlayerController.Instance.FirstInput && onlyIfPlayerMoved) return;
        if (type == ButtonType.Toggle) return;

        Buttoner(collision.gameObject);
    }

    private void Buttoner(GameObject collision)
    {
        if ((collision.CompareTag("Goonerang") && boomerActivated) ||
            (collision.CompareTag("Player") && playerActivated))
        {
            TriggerButton();
        }

        if (objectActivated == null) return;
        var gat = collision.GetComponent<ExtraTags>();
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
        if (hasTriggered) return;
        hasTriggered = true;
        state = !state;
        ApplyStateToTargets();
    }

    private void HandleTimed()
    {
        if (timerRunning || IsBlocked()) return;
        state = !state;
        ApplyStateToTargets();
        StartCoroutine(TimedRevert());
    }

    private IEnumerator TimedRevert()
    {
        timerRunning = true;
        yield return new WaitForSeconds(duration);
        state = !state;
        ApplyStateToTargets();
        timerRunning = false;
    }

    private void HandleToggle()
    {
        if (IsBlocked()) return;
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

    private bool IsBlocked()
    {
        if (!blockableByTargets) return false;

        var blocked = false;
        foreach (var target in buttonTargets)
        {
            if (target.blocked)
            {
                blocked = true;
                Debug.Log($"button {gameObject.name} blocked by target {target.gameObject.name}");
            }
        }

        return blocked;
    }

    private void SaveState()
    {
        if (!string.IsNullOrEmpty(buttonID))
        {
            TempData.SetValue(buttonID, state);
        }

        if (type == ButtonType.OneTime && !string.IsNullOrEmpty(buttonID))
        {
            TempData.SetValue($"{buttonID}_trig", hasTriggered);
        }
    }
}