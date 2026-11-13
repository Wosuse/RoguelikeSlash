using UnityEngine;

public sealed partial class ArenaPrototype
{
    [System.Serializable]
    sealed class Look
    {
        public Material material;
        public Texture2D texture;
        public Color tint = Color.white;
    }

    [Header("Movement and skill timing")]
    [SerializeField, Min(0.1f)] float moveSpeed = 5f;
    [SerializeField, Min(0.05f)] float holdThreshold = 0.25f;
    [SerializeField, Min(0)] float skillCooldown = 8f;
    [SerializeField, Min(0.01f)] float parryWindow = 0.2f;
    [SerializeField, Min(0.02f)] float dashDuration = 0.2f;
    [SerializeField, Min(0)] float dashDistance = 3f;
    [SerializeField, Min(0.01f)] float slashRadius = 2.2f;
    [SerializeField, Min(0.02f)] float spinInterval = 0.35f;
    [SerializeField, Min(0)] int slashDamage = 34, spinDamage = 14, aimedDamage = 45;
    [SerializeField, Range(0, 1)] float guardDamageMultiplier = 0.5f;
    [Header("Combat timing")]
    [SerializeField, Min(0.02f)] float autoShotInterval = 0.8f;
    [SerializeField, Min(0)] float specialEnemyStart = 30f;
    [Header("Materials, textures and tints")]
    [SerializeField] Look playerLook = new Look(), chaserLook = new Look(), chargerLook = new Look(),
        shooterLook = new Look(), playerBulletLook = new Look(), enemyBulletLook = new Look();
    [SerializeField] Material skillEffectMaterial;
    [SerializeField] Color skillEffectColor = new Color(0.38f, 1f, 0.79f);
    [Header("Optional sound effects")]
    [SerializeField] AudioClip guardSound, parrySound, dashSound, slashSound, shotSound, enemyShotSound, hurtSound;
    [SerializeField, Range(0, 1)] float soundVolume = 0.75f;
    AudioSource soundSource;

    void PlaySound(AudioClip clip)
    {
        if (!clip) return;
        if (!soundSource)
        {
            soundSource = gameObject.AddComponent<AudioSource>();
            soundSource.playOnAwake = false;
            soundSource.spatialBlend = 0;
        }
        soundSource.PlayOneShot(clip, soundVolume);
    }

    void ApplyLook(Material material, Look look, Color fallback)
    {
        Material source = look.material ? look.material : spriteMaterial;
        material.shader = source.shader;
        material.CopyPropertiesFromMaterial(source);
        if (material.HasProperty("_MainTex") && look.texture) material.mainTexture = look.texture;
        if (material.HasProperty("_Color")) material.color = fallback * look.tint;
    }
}
