using UnityEngine;

public class ExtraTags : MonoBehaviour
{
    //bro

    public enum ExtraTag 
    {
        none, heatSource, blizzardStart, blizzardEnd, blizzardContinue, shrine, contraption
    };

    public ExtraTag extraTag = ExtraTag.none;
}
