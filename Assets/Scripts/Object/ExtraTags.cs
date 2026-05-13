using UnityEngine;

public class ExtraTags : MonoBehaviour
{
    //bro

    public enum ExtraTag 
    {
        none, heatSource, blizzardStart, blizzardEnd 
    };

    public ExtraTag extraTag = ExtraTag.none;
}
