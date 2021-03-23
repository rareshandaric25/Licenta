using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapArea : MonoBehaviour
{
    [SerializeField] List<Creature> wildCretures;

    public Creature GetRandomWildCreature()
    {
        var wildCreture = wildCretures[Random.Range(0, wildCretures.Count)];
        wildCreture.Init();
        return wildCreture;
    }
}
