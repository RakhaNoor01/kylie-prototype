using System;
using UnityEngine;

/// <summary>
/// Data satu karakter untuk sistem dialogue ala Celeste.
/// Buat satu asset per karakter lewat: Assets > Create > Dialogue > Dialogue Character
/// </summary>
[CreateAssetMenu(fileName = "New Dialogue Character", menuName = "Dialogue/Dialogue Character")]
public class DialogueCharacter : ScriptableObject
{
    [Tooltip("Harus sama persis dengan nama sebelum ':' di file .yarn. Contoh: 'Kylie', 'Ludwig', 'Jake Cob', 'Dwi'")]
    public string characterName;

    [Tooltip("Warna nama karakter, contoh oranye untuk player, teal untuk NPC")]
    public Color nameColor = Color.white;

    [Tooltip("Centang kalau ini karakter player. Box akan pindah ke sisi kanan dan bingkai portrait disembunyikan.")]
    public bool isPlayer;

    [Header("Ekspresi Portrait")]
    [Tooltip("Sprite netral/default, dipakai kalau baris dialog tidak punya tag ekspresi (#happy, #sad, dst)")]
    public Sprite defaultPortrait;

    [Tooltip("Semua sprite ekspresi karakter ini. Kasih nama tiap SPRITE persis sama dengan tag ekspresinya " +
             "(sprite bernama 'happy' otomatis kepakai lewat tag #happy di baris yarn). " +
             "Select banyak sprite sekaligus di Project window lalu drag semuanya ke sini dalam satu kali drag.")]
    public Sprite[] expressions;

    [Header("Dialogue Box")]
    [Tooltip("Sprite background DialogueBox khusus buat karakter ini (drag & drop di sini). " +
             "Kalau dikosongin, DialogueBox bakal pakai sprite default (sprite awal yang " +
             "kepasang di Image DialogueBox sebelum Play).")]
    public Sprite dialogueBoxBackground;

    /// <summary>
    /// Cari sprite berdasarkan tag ekspresi dari baris yarn (line.Metadata).
    /// Kalau tidak ketemu atau tag kosong, balik ke defaultPortrait.
    /// </summary>
    public Sprite GetSprite(string expressionKey)
    {
        if (!string.IsNullOrEmpty(expressionKey) && expressions != null)
        {
            foreach (var sprite in expressions)
            {
                if (sprite != null && string.Equals(sprite.name, expressionKey, StringComparison.OrdinalIgnoreCase))
                {
                    return sprite;
                }
            }
        }

        return defaultPortrait;
    }
}