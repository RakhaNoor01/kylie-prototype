using System;
using System.Collections.Generic;
using UnityEngine;

public enum Speaker { NPC, Player }
public enum PortraitAnimType { None, Bounce, Spin, Flip }

[Serializable]
public class DialogueLine
{
    public Speaker speaker;

    [TextArea(2, 5)]
    public string text;

    public PortraitAnimType animType = PortraitAnimType.None;

    [Tooltip("Portrait ekspresif saat dia sedang bicara. Kosongkan untuk pakai portrait default.")]
    public Sprite portrait;
}

[CreateAssetMenu(menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    // ─────────────────────────────────────────────
    //  SPEAKER NAMES
    // ─────────────────────────────────────────────
    [Header("=== SPEAKER NAMES ===")]
    [Tooltip("Nama NPC yang tampil di kotak dialog. Kosongkan untuk pakai nama dari script NPC (fallback).")]
    public string npcDisplayName;

    [Tooltip("Nama Player yang tampil di kotak dialog. Kosongkan untuk pakai nama default dari DialogueManager (fallback).")]
    public string playerDisplayName;

    // ─────────────────────────────────────────────
    //  VOICE
    // ─────────────────────────────────────────────
    [Header("=== VOICE ===")]
    [Tooltip("Satu AudioClip yang diplay tiap line baru muncul. Kosongkan jika tidak perlu.")]
    public AudioClip voiceClip;

    // ─────────────────────────────────────────────
    //  LINES
    // ─────────────────────────────────────────────
    [Header("=== DIALOGUE LINES ===")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    // ─────────────────────────────────────────────
    //  STAY PORTRAITS
    // ─────────────────────────────────────────────
    [Header("=== STAY PORTRAITS (Non-Speaking) ===")]
    [Tooltip("Portrait NPC saat Player yang sedang bicara.")]
    public Sprite npcStayPortrait;

    [Tooltip("Portrait Player saat NPC yang sedang bicara.")]
    public Sprite playerStayPortrait;
}