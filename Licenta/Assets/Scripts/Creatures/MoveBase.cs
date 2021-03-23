﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "Creature/Create New Move")]
public class MoveBase : ScriptableObject
{
    [SerializeField] string name;

    [TextArea] 
    [SerializeField] string description;

    [SerializeField] CreatureType type;
    [SerializeField] int power;
    [SerializeField] int accuracy;
    [SerializeField] bool alwaysHits;
    [SerializeField] int pp;
    [SerializeField] int priority;
    [SerializeField] MoveCategory category;
    [SerializeField] MoveEffects effects;
    [SerializeField] List<SecondaryEffects> secondaryEffects;
    [SerializeField] MoveTarget target;

    public string Name {
        get { return name; }
    }
    
    public string Description {
        get { return description; }
    }

    public CreatureType Type {
        get { return type; }
    }

    public int Power {
        get { return power; }
    }
    
    public int Accuracy {
        get { return accuracy; }
    }

    public bool AlwaysHits
    {
        get { return alwaysHits; }
    }

    public int PP {
        get { return pp; }
    }

    public int Priority
    {
        get { return priority; }
    }

    public MoveCategory Category
    {
        get { return category; }
    }

    public MoveEffects Effects
    {
        get { return effects; }
    }

    public List<SecondaryEffects> SecondaryEffects
    {
        get { return secondaryEffects; }
    }
    public MoveTarget Target
    {
        get { return target; }
    }
}

[System.Serializable]
public class MoveEffects
{
    [SerializeField] List<StatBoost> boosts;
    [SerializeField] ConditionID status;

    public List<StatBoost> Boosts
    {
        get { return boosts; }
    }

    public ConditionID Status
    {
        get { return status; }
    }
}

[System.Serializable]
public class SecondaryEffects : MoveEffects
{
    [SerializeField] int chance;
    [SerializeField] MoveTarget target;

    public int Chance
    {
        get { return chance; }
    }

    public MoveTarget Target
    {
        get { return target; }
    }
}

[System.Serializable]
public class StatBoost
{
    public Stat stat;
    public int boost;
}

public enum MoveCategory
{
    Physical, Special, Status
}

public enum MoveTarget
{
    Foe, Self
}
