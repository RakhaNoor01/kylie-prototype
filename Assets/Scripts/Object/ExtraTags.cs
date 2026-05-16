using UnityEngine;

public class ExtraTags : MonoBehaviour
{
    //bro

    public enum ExtraTag 
    {
        none, heatSource, blizzardStart, blizzardEnd, blizzardContinue, shrine
    };

    public ExtraTag extraTag = ExtraTag.none;
}
