using UnityEngine;

// Attach to EACH player's physics root (same object as AstronautMovement2D).
// Emits fart puffs while the jetpack fires - bigger clouds on fart boost,
// with a toot sound when the boost ignites.
// Builds its own ParticleSystem in code - no editor setup needed.
[DisallowMultipleComponent]
[RequireComponent(typeof(AstronautMovement2D))]
public sealed class FartParticles2D : MonoBehaviour
{
    [Header("Emission")]
    [SerializeField, Min(0f)] private float puffsPerSecond = 28f;

    [Header("Puff")]
    [SerializeField, Min(0.1f)] private float puffLifetime = 1.1f;
    [SerializeField, Min(0f)] private float puffSpeed = 2.2f;
    [SerializeField, Min(0f)] private float puffSpread = 1.4f;
    [SerializeField, Min(0.01f)] private float puffStartSize = 0.25f;
    [SerializeField, Min(0.01f)] private float puffEndSize = 1.1f;

    [Header("Color - gloriously immature")]
    [SerializeField] private Color puffColorA = new Color(0.55f, 0.85f, 0.35f, 0.85f);
    [SerializeField] private Color puffColorB = new Color(0.65f, 0.60f, 0.30f, 0.85f);

    [Header("Audio (optional)")]
    [Tooltip("Plays with random pitch when the fart boost ignites.")]
    [SerializeField] private AudioClip tootSound;

    private AstronautMovement2D movement;
    private ParticleSystem puffs;
    private AudioSource audioSource;
    private float emitAccumulator;
    private bool wasBoosting;
    private ParticleSystem.EmitParams emitParams;

    private void Awake()
    {
        ValidateSettings();
        movement = GetComponent<AstronautMovement2D>();
        BuildParticleSystem();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void BuildParticleSystem()
    {
        puffs = gameObject.AddComponent<ParticleSystem>();

        var main = puffs.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 220;
        main.startLifetime = puffLifetime;
        main.startSpeed = 0f; // velocity supplied per-particle in EmitParams
        main.startSize = 1f;  // scaled by the size-over-lifetime curve below

        // We emit manually, so the automatic emitter stays off.
        var emission = puffs.emission;
        emission.enabled = false;

        var shape = puffs.shape;
        shape.enabled = false;

        // Puffs grow as they age.
        var sizeOverLifetime = puffs.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve growth = new AnimationCurve();
        growth.AddKey(0f, puffStartSize);
        growth.AddKey(1f, puffEndSize);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, growth);

        // Puffs fade out.
        var colorOverLifetime = puffs.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        // Soft round texture, generated at runtime - no asset needed.
        Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 32f)) / 32f;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }
        tex.Apply();

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        Material mat = new Material(shader);
        mat.mainTexture = tex;

        var renderer = puffs.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.sortingOrder = -2; // behind the player, above the background

        puffs.Play();
    }

    private void Update()
    {
        if (movement == null || puffs == null)
            return;

        // Rip on boost ignition.
        if (movement.FartBoostHeld && !wasBoosting)
            PlayTootSound();
        wasBoosting = movement.FartBoostHeld;

        // Continuous puffs while thrusting - bigger while fart boosting.
        float boostSize = movement.FartBoostHeld ? 1.8f : 1f;
        emitAccumulator += movement.ThrustAmount * puffsPerSecond * Time.deltaTime;
        while (emitAccumulator >= 1f)
        {
            emitAccumulator -= 1f;
            EmitPuff(boostSize);
        }
    }

    private void PlayTootSound()
    {
        if (tootSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.85f, 1.2f);
            audioSource.PlayOneShot(tootSound);
        }
    }

    private void EmitPuff(float sizeMultiplier)
    {
        Vector2 forward = transform.up; // root's +Y is forward
        Vector2 pos = (Vector2)transform.position - forward * 0.55f;
        Vector2 vel = -forward * (puffSpeed * Random.Range(0.7f, 1.3f))
            + Random.insideUnitCircle * puffSpread
            + movement.Velocity * 0.4f
            + new Vector2(0f, 0.5f); // farts rise, even in space. Don't question it.

        emitParams.position = pos;
        emitParams.velocity = vel;
        emitParams.startSize = sizeMultiplier;
        emitParams.startLifetime = puffLifetime * Random.Range(0.8f, 1.25f);
        emitParams.startColor = Color.Lerp(puffColorA, puffColorB, Random.value);
        emitParams.rotation = Random.Range(0f, 360f);
        puffs.Emit(emitParams, 1);
    }

    private void OnValidate()
    {
        ValidateSettings();
    }

    private void ValidateSettings()
    {
        puffsPerSecond = Mathf.Max(0f, puffsPerSecond);
        puffLifetime = Mathf.Max(0.1f, puffLifetime);
        puffSpeed = Mathf.Max(0f, puffSpeed);
        puffSpread = Mathf.Max(0f, puffSpread);
        puffStartSize = Mathf.Max(0.01f, puffStartSize);
        puffEndSize = Mathf.Max(0.01f, puffEndSize);
    }
}
