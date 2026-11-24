using UnityEngine;
public sealed partial class ArenaPrototype
{
 [SerializeField, InspectorName("玩家刚体")] Rigidbody2D scenePlayer;
 [SerializeField, InspectorName("敌人实例")] Transform enemyInstances;
 [SerializeField, InspectorName("玩家子弹")] Transform playerShots;
 [SerializeField, InspectorName("敌人子弹")] Transform enemyShots;
 [SerializeField, InspectorName("技能特效实例")] Transform effectInstances;
 float ClockRate => v06Skills ? v06Skills.Rate : 1;
 float EnemyTimeMultiplier => ClockRate;
}
