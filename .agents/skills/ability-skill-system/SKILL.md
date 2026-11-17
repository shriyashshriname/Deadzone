---
name: ability-skill-system
description: "Data-driven ability system with ScriptableObject definitions, cooldowns, resource costs, targeting, and cast times."
---

# Ability Skill System

## Overview
Data-driven ability architecture using Unity ScriptableObjects. Supports cooldown tracking, resource consumption, targeting, and execution decoupling.

## When to Use
- Adding active hero abilities (e.g. Dash/Roll, Turret deploy, EMP Blast, Grenade throw).
- Tracking ability cooldowns with event hooks for UI gauges.
- Managing resource costs (mana, energy, stamina, ammo).
- Standardizing ability definitions across player and elite enemies.

## Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                    AbilityDataSO                            │
│              (ScriptableObject Asset)                       │
├─────────────────────────────────────────────────────────────┤
│  Identity: ID, Name, Description, Icon                      │
│  Cooldown: Duration, Shared Group                           │
│  Costs: [{Type: Energy, Amount: 25}, ...]                   │
│  Targeting: Self, Direction, GroundPoint                    │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    AbilityManager                           │
│              (MonoBehaviour on Actor)                       │
├─────────────────────────────────────────────────────────────┤
│  TryUseAbility(slotIndex, context)                          │
│  Cooldown Tracking, Resource Checking                       │
│  Events: OnCooldownStarted, OnAbilityFired                  │
└─────────────────────────────────────────────────────────────┘
```

## Best Practices
- Keep AbilityDataSO immutable data containers; never store runtime state on ScriptableObjects.
- Track runtime cooldowns inside MonoBehaviour actor components (or dictionaries indexed by Ability ID).
- Use C# events (`System.Action<float>`) to notify UI without coupling gameplay logic directly to UI widgets.
- Keep each ability script under 250 LOC adhering to clean architecture.
