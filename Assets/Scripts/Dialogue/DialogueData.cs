using System;
using System.Collections.Generic;
using UnityEngine;

public enum Speaker { NPC, Player }

[Serializable]
public class DialogueLine
{
    public Speaker speaker;

    [TextArea(2, 5)]
    public string text;

    [Tooltip("Portrait saat karakter ini sedang bicara (optional)")]
    public Sprite portrait;
}

[CreateAssetMenu(menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("=== SPEAKER NAMES ===")]
    public string npcDisplayName;
    public string playerDisplayName;

    [Header("=== VOICE ===")]
    public AudioClip voiceClip;

    [Header("=== DIALOGUE LINES ===")]
    public List<DialogueLine> lines = new List<DialogueLine>();
}