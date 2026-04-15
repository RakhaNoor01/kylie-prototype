using System;
using System.Collections.Generic;
using UnityEngine;

public enum Speaker { NPC, Player }
public enum PortraitAnimType { None, Bounce, Spin, Flip }

[Serializable]
public class DialogueLine
{
    public Speaker speaker;
    public string text;
    public PortraitAnimType animType = PortraitAnimType.None;
    
    // Portrait ekspresif saat dia sedang berbicara
    public Sprite portrait;
}

[CreateAssetMenu(menuName = "Dialogue/Dwi Dialogue")]
public class DialogueData : ScriptableObject
{
    public List<DialogueLine> lines = new List<DialogueLine>();

    [Header("=== STAY PORTRAITS (Non-Speaking) ===")]
    [Tooltip("Portrait NPC saat Player yang sedang bicara")]
    public Sprite npcStayPortrait;
    
    [Tooltip("Portrait Player saat NPC yang sedang bicara")]
    public Sprite playerStayPortrait;
}