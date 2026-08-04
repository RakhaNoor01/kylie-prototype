// LevelSelectManager.cs - Keyboard input dihapus
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class LevelSelectManager : MonoBehaviour
{
    [Header("Level Cards")]
    public LevelCard cardForest;
    public LevelCard cardMountain;
    public LevelCard cardRuins;

    [Header("Selection Visual")]
    public float selectedScale = 1.15f;
    public float unselectedScale = 0.85f;
    public float scaleDuration = 0.2f;

    [Header("Reveal Sequence")]
    public CanvasGroup titleGroup;
    public CanvasGroup controlHintGroup;

    [Header("Reveal Timing")]
    public float titleFadeDuration = 0.3f;
    public float cardPopDuration = 0.35f;
    public float cardPopStagger = 0.12f;
    public float controlFadeDuration = 0.25f;

    [Header("Level Scenes")]
    public string forestSceneName = "Level_Forest";
    public string mountainSceneName = "Level_Mountain";
    public string ruinsSceneName = "Level_Ruins";

    [Header("Level Loader")]
    public SoloLeveling soloLeveling;

    private int _selectedIndex = 0;
    private bool _isActive = false;
    private LevelCard[] _cards;
    private Tween _currentTween;

    private void Awake()
    {
        _cards = new LevelCard[] { cardForest, cardMountain, cardRuins };
        for (int i = 0; i < _cards.Length; i++)
            _cards[i].Setup(i, this);

        if (soloLeveling == null)
        {
            Debug.LogWarning("LevelSelectManager: SoloLeveling reference belum di-set di inspector.", this);
        }
        else
        {
            DontDestroyOnLoad(soloLeveling.gameObject);
        }
    }

    public void PrepareTransition()
    {
        _selectedIndex = 0;
        foreach (var card in _cards) card.PrepareHidden();
        if (titleGroup != null) titleGroup.alpha = 0f;
        if (controlHintGroup != null) controlHintGroup.alpha = 0f;
    }

    public void OnEnter()
    {
        _selectedIndex = 0;
        StartCoroutine(Co_RevealSequence());
    }

    public void OnExit()
    {
        _isActive = false;
        _currentTween?.Kill();
    }

    public void OnCardHover(int index)
    {
        if (!_isActive) return;
        _selectedIndex = index;
        UpdateSelection(animate: true);
    }

    public void OnCardClick(int index)
    {
        if (!_isActive) return;
        _isActive = false;
        _selectedIndex = index;
        ConfirmSelection();
    }

    public void SelectForest() => OnCardClick(0);
    public void SelectMountain() => OnCardClick(1);
    public void SelectRuins() => OnCardClick(2);
    public void LoadLevelByName(string sceneName) => soloLeveling?.LoadLevel(sceneName);

    private IEnumerator Co_RevealSequence()
    {
        if (titleGroup != null)
        {
            _currentTween?.Kill();
            _currentTween = titleGroup.DOFade(1f, titleFadeDuration)
                .SetEase(Ease.InOutQuad);
            yield return _currentTween.WaitForCompletion();
        }

        for (int i = 0; i < _cards.Length; i++)
        {
            bool selected = i == _selectedIndex;
            _cards[i].PopIn(selected, selectedScale, unselectedScale, cardPopDuration);
            yield return new WaitForSeconds(cardPopStagger);
        }
        yield return new WaitForSeconds(cardPopDuration);

        if (controlHintGroup != null)
        {
            _currentTween?.Kill();
            _currentTween = controlHintGroup.DOFade(1f, controlFadeDuration)
                .SetEase(Ease.InOutQuad);
            yield return _currentTween.WaitForCompletion();
        }

        _isActive = true;
    }

    private void UpdateSelection(bool animate)
    {
        for (int i = 0; i < _cards.Length; i++)
            _cards[i].SetSelected(i == _selectedIndex, selectedScale, unselectedScale, scaleDuration, animate);
    }

    private void ConfirmSelection()
    {
        if (soloLeveling == null)
        {
            Debug.LogWarning("SoloLeveling reference is missing on LevelSelectManager.");
            return;
        }

        switch (_selectedIndex)
        {
            case 0: soloLeveling.LoadLevel(forestSceneName); break;
            case 1: soloLeveling.LoadLevel(mountainSceneName); break;
            case 2: soloLeveling.LoadLevel(ruinsSceneName); break;
        }
    }
}