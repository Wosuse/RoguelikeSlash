using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(200)]
public sealed class AfterimageDecoy:MonoBehaviour
{
 public ArenaPrototype arena;public float remaining=3,radius=3,damage,attractRadius=8;
 public bool IsTargetable=>!exploded&&!hitPending&&isActiveAndEnabled;
 bool hitPending,exploded;float explosionAge;LineRenderer burst;Material burstMaterial;
 readonly List<Material> ownedMaterials=new List<Material>();
 readonly List<ArenaPrototype.Target> targets=new List<ArenaPrototype.Target>();
 public void Initialize(ArenaPrototype owner,float seconds,float explosionRadius,float finalDamage,float attractionRadius,Color tint){arena=owner;remaining=seconds;radius=explosionRadius;damage=finalDamage;attractRadius=attractionRadius;arena.CreateAfterimageVisual(transform,tint,ownedMaterials);arena.RegisterAfterimage(this);}
 public void ReceiveAttack(){if(IsTargetable)hitPending=true;}
 void Update(){if(arena&&arena.v06Attack&&arena.v06Attack.clock.Paused)return;Advance(Time.deltaTime*(arena?arena.WorldRate:1));}
 public void Advance(float dt){if(exploded){explosionAge+=dt;if(burst){float r=radius*Mathf.Clamp01(explosionAge/.18f);burst.startColor=burst.endColor=new Color(1,.75f,.3f,1-Mathf.Clamp01(explosionAge/.3f));for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;burst.SetPosition(i,transform.position+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*r);}}if(explosionAge>=.3f)Destroy(gameObject);return;}remaining-=dt;if(hitPending||remaining<=0)Explode();}
 void Explode(){if(exploded)return;exploded=true;arena.UnregisterAfterimage(this);foreach(var renderer in GetComponentsInChildren<Renderer>())renderer.enabled=false;arena.ReadTargets(targets);Vector2 position=transform.position;foreach(var target in targets)if(Vector2.Distance(target.position,position)<=radius)arena.V09FinalHit(target.id,damage,DamageSource.Skill);
 burst=gameObject.AddComponent<LineRenderer>();burstMaterial=new Material(Shader.Find("Sprites/Default"));burst.sharedMaterial=burstMaterial;burst.loop=true;burst.positionCount=64;burst.widthMultiplier=.2f;burst.sortingOrder=20;for(int i=0;i<64;i++)burst.SetPosition(i,transform.position);}
 void OnDisable(){if(arena)arena.UnregisterAfterimage(this);}
 void OnDestroy(){foreach(var material in ownedMaterials)if(material)Destroy(material);if(burstMaterial)Destroy(burstMaterial);}
}
