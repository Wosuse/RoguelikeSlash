using System.Collections.Generic;
using UnityEngine;

public sealed class BleedStatusController : MonoBehaviour
{
    public ArenaPrototype arena;
    public RunClock clock;
    [SerializeField, InspectorName("流血目标数")] int targetCount;
    sealed class Bleed {public int id,stacks,level;public float remaining,tick,interval;}
    readonly List<Bleed> states=new List<Bleed>();
    public static float CycleCoefficient(int stacks,int level)
    {float sum=0;for(int i=0;i<stacks;i++)sum+=Mathf.Max(0,.75f-i*(level>=1?.125f:.25f));return sum;}
    public bool IsFull(int id,int level){var state=states.Find(s=>s.id==id);return state!=null&&state.stacks>=(level>=1?5:3);}
    public void Apply(int id,int level,float interval)
    {
        if(!arena.TargetPosition(id,out _))return;
        var state=states.Find(s=>s.id==id);
        if(state==null){state=new Bleed{id=id,interval=Mathf.Max(.05f,interval)};states.Add(state);}
        state.level=level;state.stacks=Mathf.Min(level>=1?5:3,state.stacks+1);state.remaining=4;
        // Refresh lifetime only. Stats are evaluated at each tick; weapon level stays on the status.
    }
    void Update()
    {
        if(!arena.Alive||clock.Paused)return;Tick(Time.deltaTime*arena.WorldRate);
    }
    void Tick(float dt)
    {
        for(int i=states.Count-1;i>=0;i--)
        {
            var state=states[i];if(!arena.TargetPosition(state.id,out _)){states.RemoveAt(i);continue;}
            float advance=Mathf.Min(dt,state.remaining);state.remaining-=advance;state.tick+=advance;
            while(state.tick+0.00001f>=state.interval){state.tick-=state.interval;arena.WeaponHit(state.id,100*CycleCoefficient(state.stacks,state.level)*state.interval/4,DamageSource.Bleed,state.level>=3);}
            if(state.remaining<=.00001f)states.RemoveAt(i);
        }
        targetCount=states.Count;
    }
    public void ResetRun(){states.Clear();targetCount=0;}
}
