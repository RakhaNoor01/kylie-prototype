using UnityEngine;
using Yarn.Unity;

/// <summary>
/// SCRIPT TEST doang. Tempel ke GameObject kosong yang punya BoxCollider2D
/// (centang Is Trigger). Tujuannya isolasi masalah: kalau trigger sesimpel
/// ini AJA masih gagal, berarti masalahnya di DialogueRunner/Presenter,
/// BUKAN di DwiNPC.
///
/// Cara pakai:
/// 1. GameObject baru -> Add Component -> Box Collider 2D -> centang "Is Trigger"
/// 2. Tempel script ini di GameObject yang sama
/// 3. Isi "Start Node" dengan node yang PASTI ada di file .yarn kamu
/// 4. Play, jalanin Player nembus box ini
/// </summary>
public class DialogueTriggerTest : MonoBehaviour
{
    public string startNode = "Dwi_Start";

    DialogueRunner dialogueRunner;

    void Update()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("[TEST] OnTriggerEnter2D kena: " + other.gameObject.name + " | tag: " + other.tag);

        if (!other.CompareTag("Player")) return;

        if (dialogueRunner == null)
        {
            Debug.LogError("[TEST] DialogueRunner masih null, gak bisa mulai dialogue.");
            return;
        }

        Debug.Log("[TEST] IsDialogueRunning sebelum start: " + dialogueRunner.IsDialogueRunning);
        Debug.Log("[TEST] Coba StartDialogue node: " + startNode);

        dialogueRunner.StartDialogue(startNode);

        Debug.Log("[TEST] IsDialogueRunning setelah start: " + dialogueRunner.IsDialogueRunning);
    }
}