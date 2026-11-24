using UnityEngine;
public enum AbilityCardKind { Projectile, Direction, Mastery, Weapon }
[CreateAssetMenu(menuName="割草/能力卡")]
public sealed class AbilityCardDefinition : ScriptableObject
{
    [InspectorName("类型")] public AbilityCardKind kind;
    [InspectorName("武器")] public WeaponKind weapon;
    public V06Card v06;
    [InspectorName("名称")] public string title;
    [TextArea, InspectorName("说明")] public string description;
    [InspectorName("图标")] public Sprite icon;
    [InspectorName("选择音效")] public AudioClip sound;
    public bool Available(WeaponInventory inventory)
    {
        switch (kind)
        {
            case AbilityCardKind.Projectile: return inventory.ProjectileCount < 3;
            case AbilityCardKind.Direction: return inventory.DirectionCount < 3;
            case AbilityCardKind.Mastery: return false;
            default: return !inventory.Owns(weapon) || inventory.Mastery < 6;
        }
    }
    public void Grant(WeaponInventory inventory)
    {
        switch (kind)
        {
            case AbilityCardKind.Projectile: inventory.AddProjectile(); break;
            case AbilityCardKind.Direction: inventory.AddDirection(); break;
            case AbilityCardKind.Mastery: break;
            case AbilityCardKind.Weapon: inventory.Acquire(weapon); break;
        }
    }
}
