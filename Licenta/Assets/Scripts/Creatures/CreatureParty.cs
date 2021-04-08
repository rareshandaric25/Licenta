using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreatureParty : MonoBehaviour
{
    [SerializeField] List<Creature> creatures;

    public List<Creature> Creatures
    {
        get { return creatures; }
    }

    void Start()
    {
        foreach (var creature in creatures)
        {
            creature.Init();
        }
    }

    public Creature GetHeatlyCreature()
    {
        return creatures.Where(x => x.HP > 0).FirstOrDefault();
    }

    public void AddCreature(Creature newCreature)
    {
        if (creatures.Count < 6)
        {
            creatures.Add(newCreature);
        }
        else
        {
            //Todo: pc
        }
    }
}
