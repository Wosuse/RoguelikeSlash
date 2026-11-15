using UnityEngine;

public sealed class WeaponInventory : MonoBehaviour
{
    [InspectorName("武器定义")] public WeaponDefinition[] definitions;
    [Min(1), InspectorName("重复武器增加熟练点")] public int duplicatePoints = 1;
    [SerializeField, InspectorName("已拥有（运行时）")] bool[] owned = new bool[3];
    [SerializeField, InspectorName("当前装备")] WeaponKind equipped;
    [SerializeField, InspectorName("共享熟练点")] int mastery;
    [SerializeField, InspectorName("每方向额外弹丸")] int extraProjectiles;
    [SerializeField, InspectorName("额外方向")] int extraDirections;
    public int Mastery => mastery;
    public int Level => mastery >= 6 ? 3 : mastery >= 3 ? 2 : mastery >= 1 ? 1 : 0;
    public int ProjectileCount => 1 + extraProjectiles;
    public int DirectionCount => 1 + extraDirections;
    public WeaponKind Equipped => equipped;
    public WeaponDefinition Current => definitions[(int)equipped];
    public bool Owns(WeaponKind kind) => owned[(int)kind];
    public void ResetRun() { System.Array.Clear(owned,0,owned.Length); owned[0] = true; equipped = WeaponKind.Dart; mastery = extraProjectiles = extraDirections = 0; }
    public bool Acquire(WeaponKind kind)
    {
        if (Owns(kind)) { if (mastery >= 6) return false; AddMastery(duplicatePoints); }
        else owned[(int)kind] = true;
        return true;
    }
    public void Equip(WeaponKind kind) { if (Owns(kind)) equipped = kind; }
    public void AddMastery(int count = 1) => mastery = Mathf.Clamp(mastery + count, 0, 6);
    public void AddProjectile() => extraProjectiles = Mathf.Min(2, extraProjectiles + 1);
    public void AddDirection() => extraDirections = Mathf.Min(2, extraDirections + 1);
}
