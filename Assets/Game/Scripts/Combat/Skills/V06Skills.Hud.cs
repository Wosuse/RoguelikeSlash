using UnityEngine;
public sealed partial class V06Skills
{
 public bool ButtonDisabled(int index)=>index<2&&arena&&arena.v06Attack&&arena.v06Attack.build&&!(index==0?arena.v06Attack.build.CanDodge:arena.v06Attack.build.CanSlash);
 public float ButtonCooldown(int index)=>index==1&&Writing?0:index<2?cd[index]:0;
 public float ButtonCooldownTotal(int index)=>index<2?uiCooldownTotal[index]:1;
 public float ButtonDuration(int index){if(index==2)return Ultimate?Mathf.Clamp01(ultimateLeft/ultimateDuration):-1;if(index==0){if(Writing)return Mathf.Clamp01(writingLeft/Mathf.Max(.01f,Card(V06CardKind.写意)));if(sprintLeft>0)return Mathf.Clamp01(sprintLeft/sprintDuration);if(dodgeLeft>0)return Mathf.Clamp01(dodgeLeft/dodgeDuration);}if(index==1&&slashLeft>0)return Mathf.Clamp01(slashLeft/slashDuration);return -1;}
 public float ButtonEnergy=>Mathf.Clamp01(energy/Mathf.Max(.01f,energyRequired));
 public int SwordCapacity(bool ring)=>Has(ring?V06CardKind.涟漪:V06CardKind.月牙)?SwordPoolCapacity:0;
 public int SwordReady(bool ring)=>Has(ring?V06CardKind.涟漪:V06CardKind.月牙)?SwordPoolReady:0;
 public float SwordRecovery(bool ring)=>Has(ring?V06CardKind.涟漪:V06CardKind.月牙)?SwordNextRecovery():-1;
}
