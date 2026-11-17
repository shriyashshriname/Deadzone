---
name: juice-game-feel
description: "Game feel and juice specialist for satisfying feedback, animations, screen shake, hitstop, and combat polish in Unity."
---

# Juice & Game Feel

## Overview
Game feel (juice) brings combat and interaction to life through layered visual, audio, and timing feedback. Makes weapon firing, impacts, and abilities feel tactile, punchy, and responsive.

## When to Use
- Adding screen shake, recoil, and muzzle flash to weapon firing.
- Implementing hit-stop (micro-pause) on heavy enemy melee or explosive hits.
- Creating pop, squash-and-stretch, or damage flashes on sprites/meshes.
- Polishing UI buttons and pickup interactions.

## Core Pillars
1. **Visual**: Screen shake, muzzle flashes, color flash (white/red), floating damage text.
2. **Timing**: Hit-stop (freezing or slowing `Time.timeScale` for 2–5 frames on heavy impacts).
3. **Audio**: Layered firing/impact SFX with slight pitch randomization (0.95x–1.05x).
4. **Haptic**: Subtle rumble or mobile vibration pulse on firing/damage.

## Standard Zero-Dependency Implementation (Coroutines & Math)
For $0 budget projects without third-party tweening packages, use lightweight coroutines and `Time.unscaledDeltaTime`:

```csharp
// Screen Shake (zero garbage, horizontal top-down plane)
public IEnumerator ShakeRoutine(Transform target, float duration, float magnitude)
{
    Vector3 original = target.localPosition;
    float elapsed = 0f;
    while (elapsed < duration)
    {
        float x = Random.Range(-1f, 1f) * magnitude;
        float z = Random.Range(-1f, 1f) * magnitude;
        target.localPosition = original + new Vector3(x, 0f, z);
        elapsed += Time.unscaledDeltaTime;
        yield return null;
    }
    target.localPosition = original;
}

// Hit-Stop Micro-Pause (realtime wait)
public IEnumerator HitStopRoutine(float duration = 0.05f)
{
    Time.timeScale = 0f;
    yield return new WaitForSecondsRealtime(duration);
    Time.timeScale = 1f;
}
```

## Best Practices
- Keep feedback snappy: combat impacts under 0.15s, hitstops 0.03s–0.06s.
- Always provide settings toggles (e.g. Screen Shake On/Off for accessibility).
- Never allocate heap memory (avoid `new` inside hot update/firing loops).
- Never block player input during juice feedback.
