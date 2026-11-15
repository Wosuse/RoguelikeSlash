using System.Collections.Generic;
using UnityEngine;

/// <summary>Only this component owns global pause. Bullet time remains a separate world rate.</summary>
public sealed class RunClock : MonoBehaviour
{
    readonly HashSet<string> reasons = new HashSet<string>();
    [SerializeField, InspectorName("暂停原因（运行时）")] string pauseReasons;
    public bool Paused => reasons.Count != 0;
    float previousScale = 1;
    void Awake()
    {
#if UNITY_EDITOR
        // Keep editor-side validation responsive when the Game view is not focused.
        Application.runInBackground = true;
#endif
    }
    public void SetPause(string reason, bool active)
    {
        bool before = Paused;
        if (active) reasons.Add(reason); else reasons.Remove(reason);
        if (!before && Paused) { previousScale = Time.timeScale; Time.timeScale = 0; }
        if (before && !Paused) Time.timeScale = previousScale;
        pauseReasons = string.Join(", ", reasons);
    }
    public void ResetRun() { if (Paused) Time.timeScale = previousScale; reasons.Clear(); pauseReasons = ""; }
    void OnDestroy() => ResetRun();
}
