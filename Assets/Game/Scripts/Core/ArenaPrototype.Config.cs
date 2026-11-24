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

    [SerializeField, InspectorName("移动与自动射击")] ArenaMovementTuning movement;
    [SerializeField, InspectorName("敌人生成")] ArenaEnemyTuning enemyTuning;
    [SerializeField, InspectorName("角色与子弹外观")] ArenaActorAppearance[] actorLooks;
    [SerializeField, InspectorName("音频管理")] ArenaAudioSettings audioSettings;
    static readonly Look DefaultLook = new Look();

    float moveSpeed => movement ? movement.WorldSpeed : 5f;
    float specialEnemyStart => enemyTuning ? enemyTuning.specialStart : 30f;
    float enemySpacing => enemyTuning ? enemyTuning.spacing : 1.2f;
    float meleeWarningTime => enemyTuning ? enemyTuning.warningTime : .3f;
    float meleeAttackRange => enemyTuning ? enemyTuning.attackRange : 1.15f;
    Look playerLook => ActorLook(0);
    Look chaserLook => ActorLook(1);
    Look chargerLook => ActorLook(2);
    Look shooterLook => ActorLook(3);
    Look playerBulletLook => ActorLook(4);
    Look enemyBulletLook => ActorLook(5);
    AudioClip shotSound => audioSettings ? audioSettings.playerShot : null;
    AudioClip enemyShotSound => audioSettings ? audioSettings.enemyShot : null;
    AudioClip hurtSound => audioSettings ? audioSettings.hurt : null;
    AudioSource soundSource;

    Look ActorLook(int index)
    {
        if (actorLooks == null || index >= actorLooks.Length || !actorLooks[index]) return DefaultLook;
        var look = actorLooks[index];
        return new Look { material = look.material, texture = look.texture, tint = look.tint };
    }

    void PlaySound(AudioClip clip)
    {
        if (!clip) return;
        if (!soundSource)
        {
            soundSource = (audioSettings ? audioSettings.gameObject : gameObject).AddComponent<AudioSource>();
            soundSource.playOnAwake = false;
            soundSource.spatialBlend = 0;
        }
        soundSource.PlayOneShot(clip, audioSettings ? audioSettings.volume : .75f);
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
