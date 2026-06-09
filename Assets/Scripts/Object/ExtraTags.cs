using UnityEngine;

public class ExtraTags : MonoBehaviour
{
    //bro

    public enum ExtraTag 
    {
        none, heatSource, blizzardStart, blizzardEnd, blizzardContinue, shrine, contraption, walterfall,
        stopCollapse
    };

    public ExtraTag extraTag = ExtraTag.none;
    public bool getColInfo = false;
    public Collider2D colInfo;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (getColInfo) colInfo = collision;
    }


    private void OnTriggerExit2D(Collider2D collision)
    {
        if (colInfo == collision)
        {
            colInfo = null;
        }
    }
}
