using UnityEngine;

public sealed class ArenaSkillPreview : MonoBehaviour
{
    [SerializeField, InspectorName("显示判定范围")] bool showRange = true;
    [SerializeField, InspectorName("玩法组件")] MonoBehaviour tuning;
    [SerializeField, InspectorName("玩家位置")] Transform player;
    [SerializeField, InspectorName("技能编号")] int skillIndex;
    [SerializeField, InspectorName("战斗控制器")] ArenaPrototype arena;

    public string Status => arena ? arena.SkillStatus(skillIndex) : "未连接";

    void OnDrawGizmosSelected()
    {
        if (!showRange || !player) return;
        Gizmos.color = new Color(.38f, 1, .79f);
        if (tuning is GuardTuning guard) DrawCircle(guard.guardRadius);
        if (tuning is SlashTuning slash) DrawCircle(slash.slashRadius);
        if (tuning is DashTuning dash) Gizmos.DrawLine(player.position, player.position + Vector3.right * dash.dashDistance);
        if (tuning is GuardDashTuning counter)
            Gizmos.DrawWireCube(player.position + Vector3.right * counter.counterReach / 2,
                new Vector3(counter.counterReach, counter.counterHalfWidth * 2, 0));
        if (tuning is GuardSlashTuning || tuning is DashSlashTuning)
        {
            var sharedDash = transform.root.GetComponentInChildren<DashTuning>();
            if (sharedDash) Gizmos.DrawLine(player.position, player.position + Vector3.right * sharedDash.dashDistance);
        }
    }

    void DrawCircle(float radius)
    {
        const int steps = 48;
        Vector3 previous = player.position + Vector3.right * radius;
        for (int i = 1; i <= steps; i++)
        {
            float angle = Mathf.PI * 2 * i / steps;
            Vector3 next = player.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
