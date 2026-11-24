using System.Collections.Generic;
using UnityEngine;
public sealed partial class V06Skills
{
 [SerializeField] int swordCharges=1;
 [SerializeField] float swordChargeTimer;
 [SerializeField,InspectorName("浪客已积攒距离")] float swordChargeDistance;
 public float SwordChargeDistanceRequired=>Mathf.Max(.01f,Card(V06CardKind.浪客));
 // Consumed charges recover in release order using the corresponding sword source's cooldown.
 readonly Queue<bool> swordRechargeOrder=new Queue<bool>();
 public int SwordPoolCapacity=>(Has(V06CardKind.月牙)||Has(V06CardKind.涟漪))?1+Mathf.Max(0,Mathf.RoundToInt(Card(V06CardKind.剑息))):0;
 public int SwordPoolReady=>Mathf.Clamp(swordCharges,0,SwordPoolCapacity);
 void ResetSwordPool(){swordCharges=1;swordChargeTimer=swordChargeDistance=0;swordRechargeOrder.Clear();}
 public void CardGranted(V06CardKind kind){if(kind==V06CardKind.浪客)swordChargeTimer=swordChargeDistance=0;if((kind==V06CardKind.月牙||kind==V06CardKind.涟漪)&&arena.v06Attack.Level(kind)==1&&!(Has(V06CardKind.月牙)&&Has(V06CardKind.涟漪)))ResetSwordPool();if(kind==V06CardKind.月牙||kind==V06CardKind.涟漪||kind==V06CardKind.剑息)EnsureSwordQueue();}
 void EnsureSwordQueue(){int capacity=SwordPoolCapacity;if(capacity==0){ResetSwordPool();return;}swordCharges=Mathf.Clamp(swordCharges,0,capacity);int missing=capacity-swordCharges;if(swordRechargeOrder.Count>missing){var keep=swordRechargeOrder.ToArray();swordRechargeOrder.Clear();for(int i=0;i<missing;i++)swordRechargeOrder.Enqueue(keep[i]);}while(swordRechargeOrder.Count<missing)swordRechargeOrder.Enqueue(!Has(V06CardKind.月牙));if(missing==0)swordChargeTimer=swordChargeDistance=0;}
 float NextSwordCooldown(){bool ring=swordRechargeOrder.Count>0&&swordRechargeOrder.Peek();return Mathf.Max(.01f,Configured(ring?V06CardKind.涟漪:V06CardKind.月牙,0,ring?rippleCooldown:moonCooldown));}
 bool ConsumeSwordCharge(bool ring){EnsureSwordQueue();if(SwordPoolReady<=0)return false;swordCharges--;swordRechargeOrder.Enqueue(ring);return true;}
 void TickSwordPool(float dt){EnsureSwordQueue();if(swordRechargeOrder.Count==0||Has(V06CardKind.浪客))return;swordChargeTimer+=dt*(1+Haste(1)/100);float cooldown=NextSwordCooldown();if(swordChargeTimer+.00001f>=cooldown){swordChargeTimer=Mathf.Max(0,swordChargeTimer-cooldown);swordRechargeOrder.Dequeue();swordCharges++;if(swordCharges>=SwordPoolCapacity)swordChargeTimer=0;}}
 public void RecoverByDistance(float amount){if(!Has(V06CardKind.浪客)||amount<=0)return;EnsureSwordQueue();if(swordRechargeOrder.Count==0)return;swordChargeDistance+=amount;float required=SwordChargeDistanceRequired;while(swordRechargeOrder.Count>0&&swordChargeDistance+.00001f>=required){swordChargeDistance=Mathf.Max(0,swordChargeDistance-required);swordRechargeOrder.Dequeue();swordCharges++;}if(swordCharges>=SwordPoolCapacity)swordChargeDistance=0;}
 public float SwordNextRecovery(){EnsureSwordQueue();return swordRechargeOrder.Count==0?-1:Mathf.Clamp01(Has(V06CardKind.浪客)?swordChargeDistance/SwordChargeDistanceRequired:swordChargeTimer/NextSwordCooldown());}
}
