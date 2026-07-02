using UnityEngine;

public class LevelSelectManager : MonoBehaviour
{
    [Header("Level Cards")]
    public LevelCard cardForest;
    public LevelCard cardMountain;

    [Header("Selection Visual")]
    public float selectedScale   = 1.08f;
    public float unselectedScale = 0.93f;
    public float scaleDuration   = 0.2f;

    [Range(0f, 1f)]
    public float dimAlpha = 0.4f;

    private int _selectedIndex = 0;
    private bool _isActive     = false;
    private MainMenuManager _mainMenu;
    private LevelCard[] _cards;

    private void Awake()
    {
        _mainMenu = GetComponent<MainMenuManager>();
        _cards    = new LevelCard[] { cardForest, cardMountain };
        cardForest.Setup(0, this);
        cardMountain.Setup(1, this);
    }

    private void Update()
    {
        if (!_isActive) return;
        if (_mainMenu.CurrentState != MainMenuManager.MenuState.LevelSelect) return;

        bool moved = false;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            _selectedIndex = Mathf.Max(0, _selectedIndex - 1);
            moved = true;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            _selectedIndex = Mathf.Min(_cards.Length - 1, _selectedIndex + 1);
            moved = true;
        }

        if (moved) UpdateSelection(animate: true);

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            ConfirmSelection();
    }

    /// <summary>
    /// Menyiapkan kartu dalam keadaan kecil (unselected) tanpa animasi sebelum transisi masuk layar.
    /// </summary>
    public void PrepareTransition()
    {
        _selectedIndex = 0;
        foreach (var card in _cards)
        {
            // Set semua ke ukuran kecil (unselectedScale) dan beri efek menggelap awal tanpa animasi
            card.SetSelected(false, selectedScale, unselectedScale, dimAlpha, scaleDuration, false);
        }
    }

    public void OnEnter()
    {
        _isActive      = true;
        _selectedIndex = 0;
        // Transisi geser panel selesai, besarkan kartu pertama yang dipilih dengan animasi halus
        UpdateSelection(animate: true);
    }

    public void OnExit()
    {
        _isActive = false;
        // Reset visual card sebelum transisi panel keluar dimulai
        foreach (var card in _cards) card.ResetVisual();
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

    private void UpdateSelection(bool animate)
    {
        for (int i = 0; i < _cards.Length; i++)
            _cards[i].SetSelected(i == _selectedIndex, selectedScale, unselectedScale,
                                  dimAlpha, scaleDuration, animate);
    }

    private void ConfirmSelection()
    {
        if (_selectedIndex == 0) _mainMenu.LoadForest();
        else                     _mainMenu.LoadMountain();
    }
}