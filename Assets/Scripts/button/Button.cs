using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public enum ButtonType
{
    OneTime,
    Timed,
    Toggle
}

[RequireComponent(typeof(Collider2D))]
public class Button : MonoBehaviour
{
    public ButtonType type;
    [Tooltip("Set empty to not have persistent data")]
    public string buttonID;

    [Header("Timed Settings")]
    public float duration = 2f;

    public bool state;
    public bool boomerangOnly = true;
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

        ApplyStateToTargets();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!(collision.gameObject.CompareTag("Goonerang") || 
            (collision.gameObject.CompareTag("Player") && !boomerangOnly)))
        {
            return;
        }

        switch (type)
        {
            case ButtonType.OneTime:  HandleOneTime(); break;
            case ButtonType.Timed:    HandleTimed();   break;
            case ButtonType.Toggle:   HandleToggle();  break;
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

        if (!string.IsNullOrEmpty(buttonID))
        {
            TempData.SetValue(buttonID, state);
        }
    }
}