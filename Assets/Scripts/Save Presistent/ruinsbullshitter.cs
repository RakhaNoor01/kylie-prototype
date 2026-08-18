using UnityEngine;

public class RuinsMenuGate : MonoBehaviour
{
    [Header("Locked")]
    [SerializeField] private GameObject lockedObject;

    [Header("Unlocked")]
    [SerializeField] private GameObject unlockedObject;

    private void Start()
    {
        bool ruinsUnlocked = Playerpref.RuinsUnlocked;

        lockedObject.SetActive(!ruinsUnlocked);
        unlockedObject.SetActive(ruinsUnlocked);
    }

    private const string RuinsUnlockedKey = "RuinsUnlocked";

    public static bool RuinsUnlocked =>
        PlayerPrefs.GetInt(RuinsUnlockedKey, 0) == 1;

    public static void UnlockRuins()
    {
        PlayerPrefs.SetInt(RuinsUnlockedKey, 1);
        PlayerPrefs.Save();

        Debug.Log("[Playerpref] RUINS UNLOCKED!");
    }
}