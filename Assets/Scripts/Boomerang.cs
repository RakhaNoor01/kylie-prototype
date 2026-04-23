using DG.Tweening;
using TarodevController;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Boomerang : MonoBehaviour
{
    public GameObject player;

    [Header("Throw")]
    public float throwPower = 15f;
    public KeyCode throwKey;
    public float noCatchPeriod = 0.15f;

    [Header("Return")]
    public float returnLerpStrength = 1f;
    public float maxSpeed = 25f;
    public float distanceMult = 10f;
    public float distMultDelayTime = 1f;

    [Header("Aiming")]
    public float slowDown = 0.25f;

    [Header("Teleport")]
    public float tpSpeed = 0.1f;

    [Header("Visuals")]
    public GameObject directionIndicator;
    public TeleportEffect tpeffect;

    private PlayerAudio _playerAudio;

    private Rigidbody2D rb;
    private Collider2D col;

    private bool isThrown = false;
    private bool hasDeflected = false;

    private SpriteRenderer rangSprite;
    public TrailRenderer rangTrail;

    private bool isCharging = false;
    private Vector2 cachedDirection;

    private float ogTime;
    private float ogDelta;

    private float distmulttimer;

    private PlayerController imLowkTrolling;
    private PlayerHealth judgement;
    private bool hasTped;
    private bool isTping;

    // --- Alternate throw mode (J + WASD) ---
    private bool isChargingAlt = false;
    private Vector2 altWASDDirection = Vector2.right; // default direction

    private float noCatchTimer = 0;
    private bool theplayerisdead = false;
    

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rangSprite = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        col.isTrigger = true;
        col.enabled = false;

        if (directionIndicator != null)
            directionIndicator.SetActive(false);

        rangSprite.enabled = false;
        rangTrail.enabled = false;

        ogTime = Time.timeScale;
        ogDelta = Time.fixedDeltaTime;

        hasTped = false;

        theplayerisdead = false;

        imLowkTrolling = player.GetComponent<PlayerController>();
        judgement = player.GetComponent<PlayerHealth>();

        judgement.death += ThyEndIsNow;
        judgement.tping = false;

        _playerAudio = player.GetComponent<PlayerAudio>();
    }

    void Update()
    {
        if (theplayerisdead) return;

        if (noCatchTimer >= 0)
        {
            noCatchTimer -= Time.deltaTime;
        }

        // Teleport while thrown
        if (isThrown && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(throwKey)))
        {
            ICameToGoon();
            return;
        }

        if (isThrown) return;

        // -----------------------------------------------
        // Alternate throw mode: hold J, aim with WASD
        // -----------------------------------------------
        HandleAltThrow();

        // Don't process mouse throw while alt mode is active
        if (isChargingAlt) return;

        // -----------------------------------------------
        // Original mouse throw mode
        // -----------------------------------------------

        // Start aiming
        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;

            if (directionIndicator != null)
                directionIndicator.SetActive(true);
        }

        // While holding mouse
        if (isCharging)
        {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mouseWorld - (Vector2)transform.position).normalized;
            direction = SnapTo8Directions(direction);

            cachedDirection = direction;

            // Rotate indicator
            if (directionIndicator != null)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                directionIndicator.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            // Slow time down
            Time.timeScale = slowDown;
            Time.fixedDeltaTime = ogDelta * slowDown;

            imLowkTrolling.doWeDeserveDestruction = true;
        }
        else
        {
            imLowkTrolling.doWeDeserveDestruction = false;
        }

        // Release to throw
        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            isCharging = false;

            if (directionIndicator != null)
                directionIndicator.SetActive(false);

            Time.timeScale = ogTime;
            Time.fixedDeltaTime = ogDelta;

            imLowkTrolling.doWeDeserveDestruction = false;

            Throw();
        }
    }

    private void ThyEndIsNow()
    {
        isCharging = false;
        isChargingAlt = false;

        if (directionIndicator != null)
            directionIndicator.SetActive(false);

        Time.timeScale = ogTime;
        Time.fixedDeltaTime = ogDelta;

        imLowkTrolling.doWeDeserveDestruction = false;
        theplayerisdead = true;

        // Clean up if thrown mid-flight
        if (isThrown)
        {
            isThrown = false;
            hasDeflected = false;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            transform.position = player.transform.position;
            transform.parent = player.transform;
            rangSprite.enabled = false;
            rangTrail.enabled = false;
            col.enabled = false;
        }
    }

    public void ResetBoomerang()
    {
        theplayerisdead = false;
        isThrown = false;
        isCharging = false;
        isChargingAlt = false;
        hasDeflected = false;
        hasTped = false;
        isTping = false;
        noCatchTimer = 0;
        distmulttimer = 0;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;
        rangSprite.enabled = false;
        rangTrail.enabled = false;

        if (directionIndicator != null)
            directionIndicator.SetActive(false);

        // Re-attach to player
        transform.position = player.transform.position;
        transform.parent = player.transform;

        Time.timeScale = ogTime;
        Time.fixedDeltaTime = ogDelta;

        imLowkTrolling.doWeDeserveDestruction = false;
    }

    void HandleAltThrow()
    {
        bool jHeld = Input.GetKey(throwKey);

        // Start alt aiming when J is first pressed
        if (Input.GetKeyDown(throwKey) && !isCharging)
        {
            isChargingAlt = true;

            altWASDDirection = new Vector2(imLowkTrolling.FacingDirection, 0f);

            if (directionIndicator != null)
                directionIndicator.SetActive(true);

            // Slow time down
            Time.timeScale = slowDown;
            Time.fixedDeltaTime = ogDelta * slowDown;

            imLowkTrolling.doWeDeserveDestruction = true;
        }

        if (isChargingAlt && jHeld)
        {
            // Read WASD input
            float h = 0f;
            float v = 0f;

            if (Input.GetKey(KeyCode.D)) h += 1f;
            if (Input.GetKey(KeyCode.A)) h -= 1f;
            if (Input.GetKey(KeyCode.W)) v += 1f;
            if (Input.GetKey(KeyCode.S)) v -= 1f;

            Vector2 rawDir = new Vector2(h, v);

            // Only update direction if WASD is being pressed
            if (rawDir.sqrMagnitude > 0.01f)
            {
                altWASDDirection = SnapTo8Directions(rawDir.normalized);
            }

            cachedDirection = altWASDDirection;

            // Rotate indicator
            if (directionIndicator != null)
            {
                float angle = Mathf.Atan2(cachedDirection.y, cachedDirection.x) * Mathf.Rad2Deg;
                directionIndicator.transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        // Release J to throw
        if (Input.GetKeyUp(throwKey) && isChargingAlt)
        {
            isChargingAlt = false;

            if (directionIndicator != null)
                directionIndicator.SetActive(false);

            Time.timeScale = ogTime;
            Time.fixedDeltaTime = ogDelta;

            imLowkTrolling.doWeDeserveDestruction = false;

            Throw();
        }

        // Cancel alt mode if somehow J is no longer held (edge case)
        if (isChargingAlt && !jHeld)
        {
            isChargingAlt = false;

            if (directionIndicator != null)
                directionIndicator.SetActive(false);

            Time.timeScale = ogTime;
            Time.fixedDeltaTime = ogDelta;

            imLowkTrolling.doWeDeserveDestruction = false;
        }
    }

    void FixedUpdate()
    {
        if (imLowkTrolling.Grounded == true && !isTping)
        {
            hasTped = false;
        }

        if (!isThrown) return;

        // Timer until distmult begins affecting the boomerang
        if (distmulttimer < distMultDelayTime)
        {
            distmulttimer += Time.deltaTime;
        }

        Vector2 toPlayer = player.transform.position - transform.position;
        float distance = toPlayer.magnitude;
        Vector2 direction = toPlayer.normalized;

        // Stronger pull when closer and distmult timer is up
        float distanceFactor = Mathf.Clamp01(1f / (distance + 0.1f));
        float scaledSpeed = 1f;

        if (distmulttimer >= distMultDelayTime)
        {
            scaledSpeed = maxSpeed * (1f + distanceFactor * distanceMult);
        }

        // Steer velocity toward player direction
        rb.linearVelocity = Vector2.Lerp(
            rb.linearVelocity,
            direction * scaledSpeed,
            returnLerpStrength * Time.fixedDeltaTime
        );

        // Clamp
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, maxSpeed);
    }

    void Throw()
    {
        isThrown = true;
        hasDeflected = false;
        col.enabled = true;

        transform.parent = null;
        rb.bodyType = RigidbodyType2D.Dynamic;

        rb.linearVelocity = cachedDirection * throwPower;

        rangSprite.enabled = true;
        rangTrail.enabled = true;

        distmulttimer = 0;
        noCatchTimer = noCatchPeriod;

        _playerAudio?.PlayThrow();

        StartCoroutine(FUCK());
    }

    private IEnumerator FUCK(){
        yield return new WaitForSeconds(0.1f);
        col.enabled = true;
    }

    void ICameToGoon()
    {
        if (hasTped) return;

        hasTped = true;
        isTping = true;
        judgement.tping = isTping;

        tpeffect.ToggleTrail(true);

        float distance = Vector2.Distance(player.transform.position, transform.position);
        float tweenDuration = distance * tpSpeed;
        player.gameObject.transform.DOMove(transform.position, tweenDuration, false)
            .OnComplete(() => {
                imLowkTrolling.ApplyBounce(0);
                tpeffect.ToggleTrail(false);
                tpeffect.teleport();
                isTping = false;
                judgement.tping = isTping;

                Catch();
                imLowkTrolling.ForceJump();

                _playerAudio?.PlayTeleport();
            });
    }

    Vector2 SnapTo8Directions(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float snapped = Mathf.Round(angle / 45f) * 45f;
        float rad = snapped * Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isThrown) return;

        // Skip deflection for breakable objects - they break but don't stop boomerang
        if (other.gameObject.CompareTag("Breakable Vines"))
        {
            return;
        }

        // Deflect off first non-player collision
        if (!hasDeflected && !other.gameObject.CompareTag("Player"))
        {
            hasDeflected = true;
            noCatchTimer = -1;
            Deflect(other);
        }

        // Catch player
        if (other.gameObject == player && noCatchTimer <= 0)
        {
            Catch();
            return;
        }
    }

    void Deflect(Collider2D other)
    {
        Vector2 toPlayer = (player.transform.position - transform.position).normalized;
        distmulttimer = distMultDelayTime;

        // Keep current speed but redirect toward player
        float currentSpeed = rb.linearVelocity.magnitude;
        rb.linearVelocity = toPlayer * currentSpeed;
    }

    void Catch()
    {
        // Check pogo condition BEFORE resetting velocity
        bool caughtFromBelow = false;

        Vector2 boomerangVelocity = rb.linearVelocity;
        float playerY = player.transform.position.y;
        float boomerangY = transform.position.y;

        // Was moving upward AND player was above?
        if (boomerangVelocity.y > 0f && playerY > boomerangY)
        {
            caughtFromBelow = true;
        }

        isThrown = false;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        transform.position = player.transform.position;
        transform.parent = player.transform;

        // Trigger pogo
        if (caughtFromBelow)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.ActivatePogoWindow();
            }
        }

        rangSprite.enabled = false;
        rangTrail.enabled = false;
        col.enabled = false;
    }
}