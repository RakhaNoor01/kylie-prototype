using UnityEngine;

public class ExtraTags : MonoBehaviour
{
    //bro

    public enum ExtraTag 
    {
        none, heatSource, blizzardStart, blizzardEnd, blizzardContinue, shrine, contraption, walterfall
    };

    public ExtraTag extraTag = ExtraTag.none;
}
