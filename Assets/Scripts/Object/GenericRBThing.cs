using System.Collections;
using TarodevController;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
// generic script for objects moved by code so that they can carry the player
public class GenericRBThing : MonoBehaviour
{
    public Transform pivot;
    public float inheritLVGrace = 0.5f;
    public float inheritLVMult = 0.75f;
    public float inheritLVThreshold = 0.1f;

    private Rigidbody2D rb;
    private Vector3 oldPos;

    private Vector2 highestLV;
    private Coroutine resetVelocityCoroutine;
    private bool playerOnThing = false;
    private bool playerJumped = false;
    private PlayerController player = null;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        oldPos = transform.position;
    }

    private void FixedUpdate()
    {
        var newPos = transform.position;
        var delta = newPos - oldPos;
        var newLV = delta / Time.fixedDeltaTime;
        rb.linearVelocity = newLV;
        oldPos = newPos;

        if (pivot != null)
        {
            rb.MovePosition(pivot.position);
        }

        if (newLV.sqrMagnitude > highestLV.sqrMagnitude)
        {
            highestLV = newLV;
        }
        else if (newLV.magnitude < inheritLVThreshold && resetVelocityCoroutine == null)
        {
            resetVelocityCoroutine = StartCoroutine(ResetHighestLV());
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && playerOnThing)
        {
            playerJumped = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        player = collision.gameObject.GetComponent<PlayerController>();
        playerOnThing = true;
        playerJumped = false;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (playerJumped && player != null)
        {
            var inherited = new Vector2(
                Mathf.Max(player.FrameVelocity.x, highestLV.x),
                Mathf.Max(player.FrameVelocity.y, highestLV.y));
            player.SetFrameVelocity(inherited);
        }
        playerOnThing = false;
        playerJumped = false;
        player = null;
    }

    private IEnumerator ResetHighestLV()
    {
        yield return new WaitForSeconds(inheritLVGrace);
        highestLV = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        resetVelocityCoroutine = null;
    }
}