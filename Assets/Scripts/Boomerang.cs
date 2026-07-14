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
    public float maxThrownSpeed = 25f;
    public float maxReturnSpeed = 22f;
    public float distanceMult = 10f;
    public float distMultDelayTime = 1f;

    [Header("Aiming")]
    public float slowDown = 0.25f;

    [Header("Teleport")]
    public float tpSpeed = 0.1f;
    public float deflectGraceTime = 0.15f;

    [Header("Visuals")]
    public GameObject visual;
    public GameObject torchure;
    public GameObject directionIndicator;
    public TeleportEffect tpeffect;
    public ParticleSystem rangPhaseParticle;

    public TrailRenderer rangTrail;
    public TrailRenderer burnTrail;
    public Gradient availableTpTrail;
    public Gradient normalTrail;
    public Gradient tpBurnTrail;
    public Gradient normalBurnTrail;

    private PlayerAudio _playerAudio;

    private Rigidbody2D rb;
    private Collider2D col;

    private bool isThrown = false;
    private bool isReturning = false;
    private bool hasDeflected = false;
    private bool deflectGrace = false;

    public bool IsThrown => isThrown;

    private bool isCharging = false;
    private Vector2 cachedDirection;

    private static float ogTime;

    private float distmulttimer;

    private PlayerController imLowkTrolling;
    private PlayerHealth judgement;
    private Slopburger slop;
    public bool hasTped;
    private bool isTping;

    private float ogROD;

    private bool burnerang;
    public bool isBurning;

    private bool shouldTrail = false;

    // --- Alternate throw mode (J + WASD) ---
    private bool isChargingAlt = false;
    private Vector2 altWASDDirection = Vector2.right; // default direction

    private float noCatchTimer = 0;

    private bool insideGeometry = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        imLowkTrolling = player.GetComponent<PlayerController>();
        judgement = player.GetComponent<PlayerHealth>();
        slop = player.GetComponent<Slopburger>();
        _playerAudio = player.GetComponent<PlayerAudio>();

        judgement.death += ResetBoomerang;
        judgement.tping = false;

        var ok = rangPhaseParticle.emission;
        ogROD = ok.rateOverDistance.Evaluate(0);

        ogTime = Time.timeScale;

        ResetBoomerang();
    }

    void Update()
    {
        if (judgement.IsDead) return;

        HandleEffects();

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

        if (slop != null)
        {
            if (slop.TheOrch == true) 
            {
                burnerang = true;
            }
            else
            {
                burnerang = false;
            }
        }

        if (isThrown) return;

        // Alternate throw mode
        HandleAltThrow();

        // Don't process mouse throw while alt mode is active
        if (isChargingAlt) return;

        // Mouse throw mode

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

            imLowkTrolling.doWeDeserveDestruction = false;

            Throw();
        }
    }

    public void ResetBoomerang()
    {
        isThrown = false;
        isCharging = false;
        isChargingAlt = false;
        hasDeflected = false;
        hasTped = false;
        isTping = false;
        noCatchTimer = 0;
        distmulttimer = 0;
        isBurning = false;

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        col.isTrigger = true;
        col.enabled = false;

        visual.SetActive(false);
        shouldTrail = false;

        torchure.SetActive(false);
        rangTrail.emitting = false;
        burnTrail.emitting = false;

        if (directionIndicator != null)
            directionIndicator.SetActive(false);

        transform.position = player.transform.position;
        transform.parent = player.transform;

        Time.timeScale = ogTime;

        imLowkTrolling.doWeDeserveDestruction = false;

        var ok = rangPhaseParticle.emission;
        ok.rateOverDistance = 0;
    }

    private void HandleEffects()
    {
        rangTrail.colorGradient = hasTped ? normalTrail : availableTpTrail;
        burnTrail.colorGradient = hasTped ? normalBurnTrail : tpBurnTrail;

        if (isBurning)
        {
            torchure.SetActive(true);
        }
        else
        {
            torchure.SetActive(false);
        }

        if (shouldTrail)
        {
            if (isBurning)
            {
                burnTrail.emitting = true;
                rangTrail.emitting = false;
            } 
            else
            {
                burnTrail.emitting = false;
                rangTrail.emitting = true;
            }
            
        }
        else
        {
            rangTrail.emitting = false;
            burnTrail.emitting = false;
        }

        var emission = rangPhaseParticle.emission;
        emission.rateOverDistance = insideGeometry && !deflectGrace ? ogROD : 0;
        var main = rangPhaseParticle.main;
        main.startRotation = visual.transform.rotation.z;
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

        if (noCatchTimer <= 0)
        {
            float catchRadius = col.bounds.extents.magnitude + 0.5f;
            if (Vector2.Distance(transform.position, player.transform.position) <= catchRadius)
            {
                Catch();
                return;
            }
        }

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

        bool dismultting = distmulttimer >= distMultDelayTime;

        if (dismultting)
        {
            isReturning = true;
            scaledSpeed = maxThrownSpeed * (1f + distanceFactor * distanceMult);
        }

        // Steer velocity toward player direction
        rb.linearVelocity = Vector2.Lerp(
            rb.linearVelocity,
            direction * scaledSpeed,
            returnLerpStrength * Time.fixedDeltaTime
        );

        var clampTo = dismultting ? maxReturnSpeed : maxThrownSpeed;

        // Clamp
        rb.linearVelocity = Vector2.ClampMagnitude(rb.linearVelocity, clampTo);
    }

    void Throw()
    {
        isThrown = true;
        hasDeflected = false;

        transform.parent = null;
        rb.bodyType = RigidbodyType2D.Dynamic;

        TheLopcity();

        visual.SetActive(true);
        shouldTrail = true;
        if (burnerang) isBurning = true;

        distmulttimer = 0;
        noCatchTimer = noCatchPeriod;

        col.enabled = true;

        _playerAudio?.PlayThrow();

        StartCoroutine(IgnorePlayerBriefly());
    }

    private void TheLopcity()
    {
        var plrb = player.GetComponent<Rigidbody2D>();

        // Base throw velocity
        Vector2 throwVelocity = cachedDirection * throwPower;

        // Player movement
        Vector2 playerVelocity = plrb.linearVelocity;

        // Check alignment between velocities
        float alignment = Vector2.Dot(
            throwVelocity.normalized,
            playerVelocity.normalized
        );

        // Remap to 0,1 to be used for Lerp
        alignment = Mathf.Clamp01((alignment + 1f) * 0.5f);

        // Larper
        Vector2 addedVelocity = Vector2.Lerp(Vector2.zero, playerVelocity, alignment);

        rb.linearVelocity = throwVelocity + addedVelocity;
    }

    private IEnumerator IgnorePlayerBriefly()
    {
        int boomerangLayer = gameObject.layer;
        int playerLayer = player.layer;

        Physics2D.IgnoreLayerCollision(boomerangLayer, playerLayer, true);
        yield return new WaitForSeconds(noCatchPeriod);
        Physics2D.IgnoreLayerCollision(boomerangLayer, playerLayer, false);
    }

    void ICameToGoon()
    {
        if (hasTped || (insideGeometry && !deflectGrace)) return;

        hasTped = true;
        isTping = true;
        judgement.tping = isTping;

        tpeffect.ToggleTrail(true);

        float distance = Vector2.Distance(player.transform.position, transform.position);
        float tweenDuration = distance * tpSpeed;
        player.gameObject.transform.DOMove(transform.position, tweenDuration, false)
            .OnComplete(() => {
                tpeffect.ToggleTrail(false);
                tpeffect.teleport();
                isTping = false;
                judgement.tping = isTping;

                Catch();
                if (!imLowkTrolling.IsGliding && !imLowkTrolling.IsClinging)
                {
                    imLowkTrolling.ApplyBounce(0);
                    imLowkTrolling.ForceJump();
                }

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

        var huh = other.GetComponent<ExtraTags>();
        if (huh != null && huh.extraTag == ExtraTags.ExtraTag.walterfall)
        {
            isBurning = false;
        }

        if (other.gameObject.CompareTag("Breakable Vines") ||
            other.gameObject.layer == LayerMask.NameToLayer("Debug") || 
            other.isTrigger)
        {
            return;
        }

        if (other.gameObject.CompareTag("One Way"))
        {
            if (!IsHittingSolidSide(other))
                return;
        }

        insideGeometry = true;

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

    bool IsHittingSolidSide(Collider2D platform)
    {
        Vector2 platformUp = platform.transform.up;

        Vector2 travelDir = rb.linearVelocity.normalized;

        return Vector2.Dot(travelDir, platformUp) < 0f;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!isThrown) return;

        if (collision.gameObject.CompareTag("Breakable Vines") || collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        insideGeometry = false;
    }

    void Deflect(Collider2D other)
    {
        if (!isReturning)
        {
            deflectGrace = true;
            Invoke(nameof(UnGrace), deflectGraceTime);
        }

        Vector2 toPlayer = (player.transform.position - transform.position).normalized;
        distmulttimer = distMultDelayTime;

        // Keep current speed but redirect toward player
        float currentSpeed = rb.linearVelocity.magnitude;
        rb.linearVelocity = toPlayer * currentSpeed;
    }

    void UnGrace()
    {
        deflectGrace = false;
    }

    void Catch()
    {
        insideGeometry = false;

        isThrown = false;
        isReturning = false;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        transform.position = player.transform.position;
        transform.parent = player.transform;

        visual.SetActive(false);
        shouldTrail = false;
        isBurning = false;
        col.enabled = false;
    }
}