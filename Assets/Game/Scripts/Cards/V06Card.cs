using UnityEngine;
public enum V06CardKind { 连击弹, 穿透之箭, 分裂, 锈刃, 隼之眼, 射手天赋, 狙击天赋, 灵巧射手, 力劈 = 9, 战士天赋, 剑客天赋, 剑客之道, 亮剑, 写意, 月牙, 涟漪, 剑息, 快攻势, 浪, 炎, 火羽, 水涌, 引雷, 雷劈, 自信走位, 纯粹射手, 烈射手, 狂射手, 踢踏舞, 嗡嗡攻势, 会心, 冰寒, 剑气, 剑心, 浪客, 血袭, 血狂, 临危亢奋, 不屈, 背水, 淬毒之刃, 蓄势, 炽刃, 珠帘, 离别赠礼 }
public sealed class V06Card : MonoBehaviour
{
 public V06CardKind kind;
 [Min(1)] public int maxPicks=4;
 [Min(0)] public float weight=1;
 public Sprite icon; public AudioClip sound; public GameObject hitEffect;
 [Tooltip("按获得次数读取，百分比用百分数填写")] public float[] values={1,2,3,4};
 public float[] secondary={0,0,0,0};
 [Tooltip("0表示使用技能系统默认值")] public float cooldown,duration,radius;
 public float[] chargeBaseTimes={1.5f,2.5f,3.5f},chargeUpgradeTimes={.5f,1,2},chargeDamage={125,175,275};
 [TextArea] public string summary;
 [TextArea(3,10)] public string details;
 public string ShortDescription(int stage){string simple;if(TrySimpleGrowth(out simple))return simple;float v=Value(stage),s=Secondary(stage);switch(kind){
 case V06CardKind.连击弹:return $"每轮共 {1+v} 发";
 case V06CardKind.穿透之箭:return $"穿透伤害最低保留 {v}%";
 case V06CardKind.分裂:return stage==1?"首发分裂追击":stage==2?"解锁第二代分裂":$"前 {stage-1} 发可分裂";
 case V06CardKind.锈刃:return $"{v}% 概率中毒";
 case V06CardKind.隼之眼:return $"射程累计 +{v}%";
 case V06CardKind.射手天赋:return $"普攻伤害累计 +{v}%";
 case V06CardKind.狙击天赋:return $"暴击 +{v}%，暴伤 +{s}%";
 case V06CardKind.踢踏舞:return "普攻叠加移速与攻速，受伤或未命中超时清空";
 case V06CardKind.蓄势:return stage>1?"缩短蓄力门槛，蓄满自动释放；蓄力期间不可移动":"长按斩蓄力，松手或蓄满释放；蓄力期间不可移动";
 case V06CardKind.离别赠礼:return $"按下闪留下诱敌残影，受击或{(duration>0?duration:3):0.##}秒后爆炸";
 case V06CardKind.会心:return $"每攻击{v:0}轮，下一轮首发必暴";
 case V06CardKind.浪客:return $"每移动{v:0.##}单位恢复1点剑气";
 case V06CardKind.灵巧射手:return $"攻速 +{v}%，移速 +{s}%";
 default:return summary+"（"+v+"）";}}
 public bool GeneratedDescription=>kind==V06CardKind.会心||kind==V06CardKind.剑气||kind==V06CardKind.浪客||kind==V06CardKind.蓄势||kind==V06CardKind.踢踏舞||kind==V06CardKind.写意||kind==V06CardKind.月牙||kind==V06CardKind.涟漪||kind==V06CardKind.离别赠礼;
 public string Description(){if(kind==V06CardKind.会心)return $"仅1级。首次获得立即就绪，下一轮首发弹丸必定暴击。\n普通攻击每攻击{Value(1):0}轮，下一轮首发再次必暴；一轮中的连发弹丸和分裂子攻击不额外计轮。\n不随时间恢复，不受技能急速或冷却影响。必暴首发使用红色拖尾弓箭，伤害为普通首发的{Secondary(1):0.##}%，子攻击继承其外观与必暴。";if(kind==V06CardKind.剑气)return $"月牙与涟漪命中敌人时，敌人随剑气持续平移后退，累计最多击退{(radius>0?radius:6):0.##}个世界单位；剑气消失时立即停止推动。涟漪沿远离释放中心的方向推动。\n剑气经过的范围内，敌方子弹直接无效化；不清除己方弹丸。测试木桩同样可以被击退。";if(kind==V06CardKind.浪客)return $"剑气改为按实际移动距离恢复，不再随时间恢复，也不受技能急速影响。\n每移动{Value(1):0.##}个世界单位，恢复1点剑气；多个空槽逐个恢复。\n剑气CD黄条显示当前距离积攒进度，静止时不增长；充能已满时不额外储存距离。";if(kind==V06CardKind.蓄势)return ChargeDescription();if(kind==V06CardKind.踢踏舞){var arena=GetComponentInParent<ArenaPrototype>();var build=arena&&arena.v06Attack?arena.v06Attack.build:null;return $"每轮普通攻击首次命中敌人时，移动速度 +{(build?build.danceMovePercentPerStack:1):0.##}%，攻击速度 +{(build?build.danceAttackPercentPerStack:1):0.##}%，最多{(build?build.danceStackLimit:100)}层。\n加成按基础值加算；同一轮的连发、穿透和分裂只叠一次。\n连续{(build?build.danceMissTimeout:5):0.##}秒没有普通攻击命中敌人，清空全部叠层；同轮后续命中也会刷新计时。\n斩、剑气、持续伤害等不能叠层或刷新计时。\n受到实际生命伤害后清空全部叠层，护盾完全吸收或免疫伤害不清空。";}if(kind==V06CardKind.离别赠礼)return $"按下闪，在原地留下暗色玩家残影，吸引附近敌人攻击。\n受到一次攻击或存续{(duration>0?duration:3):0.##}秒后爆炸，半径{(radius>0?radius:3):0.##}，造成{Value(1):0.##}%攻击力伤害。";if(kind==V06CardKind.月牙||kind==V06CardKind.涟漪)return SwordSourceDescription();if(kind==V06CardKind.写意)return WritingDescription();string simple;if(TrySimpleGrowth(out simple))return simple+"（每次获取）\n最多获取 "+maxPicks+" 次。";string text=details+"\n\n当前配置：";for(int i=1;i<=maxPicks;i++)text+=$"\n第 {i} 次：{ShortDescription(i)}";if(kind==V06CardKind.蓄势)text+="\n蓄力期间不可移动；松开或达到最高档蓄力时间后释放环形斩。";return text;}
 string ChargeDescription(){var arena=GetComponentInParent<ArenaPrototype>();var skills=arena?arena.v06Skills:null;string text="长按斩进入蓄力，期间不可移动，也不释放普通攻击。\n松手时按已达到的最高档释放环形斩；达到最高档时间后自动释放，随后松手不会重复释放。";text+=$"\n未达到第一档时，使用基础环形斩伤害{(skills?skills.ringDamage:150):0.##}%攻击力。";text+="\n首次获取：";for(int i=0;i<Mathf.Min(chargeBaseTimes.Length,chargeDamage.Length);i++)text+=$"\n第{i+1}档：蓄力{chargeBaseTimes[i]:0.##}秒，伤害{chargeDamage[i]:0.##}%攻击力。";if(maxPicks>1){text+="\n再次获取后：";for(int i=0;i<Mathf.Min(chargeUpgradeTimes.Length,chargeDamage.Length);i++)text+=$"\n第{i+1}档：蓄力{chargeUpgradeTimes[i]:0.##}秒，伤害{chargeDamage[i]:0.##}%攻击力。";}return text;}
 string SwordSourceDescription(){bool ring=kind==V06CardKind.涟漪;var arena=GetComponentInParent<ArenaPrototype>();var skills=arena?arena.v06Skills:null;float cd=cooldown>0?cooldown:skills?(ring?skills.rippleCooldown:skills.moonCooldown):(ring?15:10);return (ring?"长按斩释放扩散环形剑气":"短按斩释放飞行剑气")+$"，基础伤害{Value(1):0.##}%攻击力。\n月牙和涟漪共用剑气充能，默认上限1；只有剑息增加上限。\n{(skills&&arena.v06Attack.Level(V06CardKind.浪客)>0?$"浪客生效：每移动{skills.SwordChargeDistanceRequired:0.##}单位恢复1点剑气，不随时间恢复，不受技能急速影响。":$"本次消耗的基础恢复时间{cd:0.##}秒，受技能急速影响。")}多个空槽按顺序逐个恢复。"+(!ring?$"\n命中总宽度{(skills?skills.EffectiveMoonHitWidth:1):0.##}，沿飞行路径两侧各一半。":"")+$"\n基础飞行距离{(skills?skills.swordRange:10):0.##}，速度{(skills?skills.swordSpeed:8):0.##}单位/秒。";}
 string WritingDescription(){var arena=GetComponentInParent<ArenaPrototype>();var skills=arena?arena.v06Skills:null;float attackSpeed=skills?skills.writingAttackSpeed:70,haste=skills?skills.writingHaste:60,moveSpeed=skills?skills.writingMoveSpeed:20;return $"长按闪开启写意，持续{Value(1):0.##}秒，松开后仍持续。\n期间攻速 +{attackSpeed:0.##}%，技能急速 +{haste:0.##}点，移动速度 +{moveSpeed:0.##}%。\n自动普攻和斩分别计时，伤害为50%；自动斩可移动，可触发50%伤害月牙并遵守其独立冷却。\n自动斩不占用斩按钮冷却。点击斩立即退出并释放一次正常伤害手动斩；点击闪直接退出。\n退出后移除临时加成，开始长按闪冷却，基础冷却×1.5。";}
 bool Linear(System.Func<int,float> read,float baseline,out float step){step=read(1)-baseline;if(step<=0)return false;for(int i=1;i<=maxPicks;i++)if(!Mathf.Approximately(read(i),baseline+step*i))return false;return true;}
 bool Constant(System.Func<int,float> read,out float value){value=read(1);if(value<=0)return false;for(int i=2;i<=maxPicks;i++)if(!Mathf.Approximately(read(i),value))return false;return true;}
 static string Number(float value)=>value.ToString("0.##");
 public bool TrySimpleGrowth(out string text){text=null;if(maxPicks<=1)return false;float a,b;
 switch(kind){
 case V06CardKind.隼之眼:if(Linear(Value,0,out a))text="射程 +"+Number(a)+"%";break;
 case V06CardKind.射手天赋:if(Linear(Value,0,out a))text="普攻伤害 +"+Number(a)+"%";break;
 case V06CardKind.力劈:if(Linear(Value,0,out a))text="斩伤害 +"+Number(a)+"%";break;
 case V06CardKind.战士天赋:if(Linear(Value,0,out a))text="斩技能急速 +"+Number(a)+" 点";break;
 case V06CardKind.剑客天赋:if(Linear(Value,0,out a)&&Linear(Secondary,0,out b))text="斩与剑气暴击率 +"+Number(a)+" 个百分点，暴击伤害 +"+Number(b)+" 个百分点";break;
 case V06CardKind.狙击天赋:if(Constant(Value,out a)&&Constant(Secondary,out b))text="普攻暴击率 +"+Number(a)+" 个百分点，暴击伤害 +"+Number(b)+" 个百分点";break;
 case V06CardKind.剑息:if(Linear(Value,0,out a))text="剑气储存次数 +"+Number(a);break;
 case V06CardKind.炎:if(Linear(Value,0,out a))text="火焰伤害 +"+Number(a)+"%";break;
 case V06CardKind.火羽:if(Linear(Value,0,out a))text="火焰弹触发概率 +"+Number(a)+" 个百分点";break;
 case V06CardKind.水涌:if(Linear(Value,0,out a))text="水弹触发概率 +"+Number(a)+" 个百分点；命中范围潮湿3秒";break;
 case V06CardKind.浪:if(Linear(Value,10,out a))text="潮湿易伤 +"+Number(a)+" 个百分点（基础10%）";break;
 case V06CardKind.连击弹:if(Linear(Value,0,out a))text="每轮弹丸 +"+Number(a)+"；仅首次获取使攻击频率降低25%";break;
 }return !string.IsNullOrEmpty(text);}

 public float Value(int stage)=>values!=null&&values.Length>0?values[Mathf.Clamp(stage-1,0,values.Length-1)]:0;
 public float Secondary(int stage)=>secondary!=null&&secondary.Length>0?secondary[Mathf.Clamp(stage-1,0,secondary.Length-1)]:0;
}
