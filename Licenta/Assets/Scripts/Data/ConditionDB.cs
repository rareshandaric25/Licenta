using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionDB
{
    public static void Init()
    {
        foreach (var kvp in Conditions)
        {
            var conditionId = kvp.Key;
            var condition = kvp.Value;

            condition.Id = conditionId;
        }
    }
    public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>()
    {
        {
            ConditionID.psn,
            new Condition()
            {
                Name ="Poison",
                StartMessage = "has been poisoned",
                OnAfterTurn = (Creature creature) =>
                {
                    creature.UpdateHP(creature.MaxHp/8);
                    creature.StatusChanges.Enqueue($"{creature.Base.Name} took damage from poison");
                }
            }
        },
        {
            ConditionID.par,
            new Condition()
            {
                Name ="Paralyzed",
                StartMessage = "has been paralyzed",
                OnBeforeMove = (Creature creture) =>
                {
                    if (Random.Range(1, 5) == 1)
                    {
                        creture.StatusChanges.Enqueue($"{creture.Base.Name}'s paralyzed and can't move");
                        return false;
                    }

                    return true;
                }
            }
        },
        {
            ConditionID.frz,
            new Condition()
            {
                Name ="Freeze",
                StartMessage = "has been frozen",
                OnBeforeMove = (Creature creture) =>
                {
                    if (Random.Range(1, 5) == 1)
                    {
                        creture.CureStatus();
                        creture.StatusChanges.Enqueue($"{creture.Base.Name}'s paralyzed and can't move");
                        return true;
                    }

                    return false;
                }
            }
        },
        {
            ConditionID.slp,
            new Condition()
            {
                Name ="Sleep",
                StartMessage = "has fallen asleep",
                OnStart = (Creature creature) =>
                {
                    //Sleep for 1-3 turn
                    creature.StatusTime = Random.Range(1, 4);
                    Debug.Log($"Will be asleep for {creature.StatusTime} moves");
                },
                OnBeforeMove = (Creature creture) =>
                {
                    if (creture.StatusTime <= 0)
                    {
                        creture.CureStatus();
                        creture.StatusChanges.Enqueue($"{creture.Base.Name} woke up");
                        return true;
                    }
                    creture.StatusTime--;
                    creture.StatusChanges.Enqueue($"{creture.Base.Name} is sleeping");
                    return false;
                }
            }
        }
    };
}

public enum ConditionID
{
    none, psn, slp, par, frz 
}
