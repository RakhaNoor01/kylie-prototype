// ConfirmPopupController.cs - FULL CODE (dengan OnPointerExit)
using UnityEngine;
using TMPro;
using DG.Tweening;

public class ConfirmPopupController : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup group;
    public TMP_Text messageLabel;
    public MenuButton yesButton;
    public MenuButton noButton;

    [Header("Button Animation")]
    public float buttonHoverScale = 1.1f;
    public float buttonAnimDuration = 0.2f;

    private System.Action _onYes;
    private System.Action _onNo;
    private Tween _yesTween;
    private Tween _noTween;

    private void Awake()
    {
        Debug.Log($"[ConfirmPopupController] Awake dipanggil! GameObject: {gameObject.name}");
        
        if (yesButton != null)
        {
            // Hover masuk - membesar
            yesButton.onHover = () => 
            {
                Debug.Log("[ConfirmPopupController] Yes button hover IN");
                AnimateButton(yesButton.transform, true);
            };
            
            // Hover keluar - kembali normal
            yesButton.onExit = () => 
            {
                Debug.Log("[ConfirmPopupController] Yes button hover OUT");
                AnimateButton(yesButton.transform, false);
            };
            
            yesButton.onSubmit = () => 
            {
                Debug.Log("[ConfirmPopupController] Yes button diklik!");
                Confirm(true);
            };
        }
        else
        {
            Debug.LogError("[ConfirmPopupController] yesButton is NULL!");
        }

        if (noButton != null)
        {
            // Hover masuk - membesar
            noButton.onHover = () => 
            {
                Debug.Log("[ConfirmPopupController] No button hover IN");
                AnimateButton(noButton.transform, true);
            };
            
            // Hover keluar - kembali normal
            noButton.onExit = () => 
            {
                Debug.Log("[ConfirmPopupController] No button hover OUT");
                AnimateButton(noButton.transform, false);
            };
            
            noButton.onSubmit = () => 
            {
                Debug.Log("[ConfirmPopupController] No button diklik!");
                Confirm(false);
            };
        }
        else
        {
            Debug.LogError("[ConfirmPopupController] noButton is NULL!");
        }

        SetActive(false);
    }

    private void OnDestroy()
    {
        _yesTween?.Kill();
        _noTween?.Kill();
    }

    private void AnimateButton(Transform buttonTransform, bool hover)
    {
        float targetScale = hover ? buttonHoverScale : 1f;
        
        if (buttonTransform == yesButton.transform)
        {
            _yesTween?.Kill();
            _yesTween = buttonTransform.DOScale(targetScale, buttonAnimDuration)
                .SetEase(Ease.OutBack);
        }
        else if (buttonTransform == noButton.transform)
        {
            _noTween?.Kill();
            _noTween = buttonTransform.DOScale(targetScale, buttonAnimDuration)
                .SetEase(Ease.OutBack);
        }
    }

    public void Show(string message, System.Action onYes, System.Action onNo)
    {
        Debug.Log($"[ConfirmPopupController] Show dipanggil! Message: {message}");
        
        if (messageLabel != null) 
        {
            messageLabel.text = message;
        }
        else
        {
            Debug.LogError("[ConfirmPopupController] messageLabel is NULL!");
        }
        
        _onYes = onYes;
        _onNo = onNo;

        if (yesButton != null) 
        {
            yesButton.SetSelected(true);
            yesButton.transform.localScale = Vector3.one;
        }
        
        if (noButton != null) 
        {
            noButton.SetSelected(false);
            noButton.transform.localScale = Vector3.one;
        }

        StartCoroutine(ShowPopupAnimation());
    }

    private System.Collections.IEnumerator ShowPopupAnimation()
    {
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        
        RectTransform rect = group.GetComponent<RectTransform>();
        rect.localScale = Vector3.one * 0.8f;
        
        gameObject.SetActive(true);
        
        Tween fadeTween = group.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
        Tween scaleTween = rect.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
        
        yield return fadeTween.WaitForCompletion();
        yield return scaleTween.WaitForCompletion();
        
        group.interactable = true;
        group.blocksRaycasts = true;
        transform.SetAsLastSibling();
    }

    private void Confirm(bool yes)
    {
        Debug.Log($"[ConfirmPopupController] Confirm: {yes}");
        StartCoroutine(HidePopupAnimation(yes));
    }

    private System.Collections.IEnumerator HidePopupAnimation(bool yes)
    {
        group.interactable = false;
        group.blocksRaycasts = false;
        
        RectTransform rect = group.GetComponent<RectTransform>();
        
        Tween fadeTween = group.DOFade(0f, 0.2f).SetEase(Ease.InQuad);
        Tween scaleTween = rect.DOScale(0.8f, 0.2f).SetEase(Ease.InBack);
        
        yield return fadeTween.WaitForCompletion();
        yield return scaleTween.WaitForCompletion();
        
        SetActive(false);
        
        if (yes)
        {
            _onYes?.Invoke();
        }
        else
        {
            _onNo?.Invoke();
        }
    }

    private void SetActive(bool active)
    {
        if (group == null) 
        {
            Debug.LogError("[ConfirmPopupController] group is NULL!");
            return;
        }
        
        Debug.Log($"[ConfirmPopupController] SetActive: {active}");
        
        if (!active)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
}