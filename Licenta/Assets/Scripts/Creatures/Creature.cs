using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class Creature
{
   [SerializeField] CreatureBase _creature;
   [SerializeField] int level;

   public Creature(CreatureBase pBase,int plevel)
   {
      _creature = pBase;
      level = plevel;
      
      Init();
   }
   public CreatureBase Base {
      get
      {
         return _creature;
      }
   }

   public int Level
   {
      get
      {
         return level;
      }
   }

   public int Exp { get; set; }
   public int HP { get; set; }
   public List<Move> Moves { get; set; }
   public Move CurrentMove { get; set; }
   public Dictionary<Stat, int> Stats { get; private set; }
   public Dictionary<Stat, int> StatBoosts { get; private set; }
   public Condition Status { get; private set; }
   public int StatusTime { get; set; }

   public Queue<string> StatusChanges { get; private set; }
   public bool HpChanged { get; set; }
   public event Action OnStatusChanged; 

   public void Init()
   {
      HP = MaxHp;
      
      //Generate moves
      Moves = new List<Move>();
      foreach (var move in Base.LearnableMoves)
      {
         if (move.Level <= Level)
            Moves.Add(new Move(move.Base));

         if (Moves.Count >= CreatureBase.MaxNumOfMoves)
            break;
      }

      Exp = Base.GetExpForLevel(Level);
      
      CalculateStats();
      HP = MaxHp;

      StatusChanges = new Queue<string>();
      ResetStatBoost();
      Status = null;
   }

   void CalculateStats()
   {
      Stats = new Dictionary<Stat, int>();
      Stats.Add(Stat.Attack, Mathf.FloorToInt((Base.Attack * Level) / 100f) + 5);
      Stats.Add(Stat.Defense, Mathf.FloorToInt((Base.Defense * Level) / 100f) + 5);
      Stats.Add(Stat.SpAttack, Mathf.FloorToInt((Base.SpAttack * Level) / 100f) + 5);
      Stats.Add(Stat.SpDefense, Mathf.FloorToInt((Base.SpDefense * Level) / 100f) + 5);
      Stats.Add(Stat.Speed, Mathf.FloorToInt((Base.Speed * Level) / 100f) + 5);

      MaxHp = Mathf.FloorToInt((Base.MaxHp * Level) / 100f) + 10 + Level;
   }

   void resetHp()
   {
      HP = MaxHp;
   }
   
   void ResetStatBoost()
   {
      StatBoosts = new Dictionary<Stat, int>()
      {
         {Stat.Attack, 0},
         {Stat.Defense, 0},
         {Stat.SpAttack, 0},
         {Stat.SpDefense, 0},
         {Stat.Speed, 0},
         {Stat.Accuracy, 0},
         {Stat.Evasion, 0}
      };
   }

   int GetStat(Stat stat)
   {
      int statVal = Stats[stat];
      
      // Aply stat boost
      int boost = StatBoosts[stat];
      var boostValues = new float[] {1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f};

      if (boost >= 0)
         statVal = Mathf.FloorToInt(statVal * boostValues[boost]);
      else
         statVal = Mathf.FloorToInt(statVal / boostValues[-boost]);

      return statVal;
   }

   public void ApplyBoosts(List<StatBoost> statBoosts)
   {
      foreach (var statBoost in statBoosts)
      {
         var stat = statBoost.stat;
         var boost = statBoost.boost;

         StatBoosts[stat] = Mathf.Clamp(StatBoosts[stat] + boost,-6,6);
         
         if(boost>0)
            StatusChanges.Enqueue($"{Base.Name}'s {stat} rose");
         else
            StatusChanges.Enqueue($"{Base.Name}'s {stat} fell");

         Debug.Log($"{stat} has been boosted to {StatBoosts[stat]}");
      }
   }

   public bool CheckForLevelUp()
   {
      if (Exp > Base.GetExpForLevel(level + 1))
      {
         ++level;
         return true;
      }
      return false;
   }

   public LearnableMove GetLearnableMoveAtCurrLevel()
   {
      return Base.LearnableMoves.Where(x => x.Level == level).FirstOrDefault();
   }

   public void LearnMove(LearnableMove moveToLearn)
   {
      if (Moves.Count > CreatureBase.MaxNumOfMoves)
         return;
      
      Moves.Add(new Move(moveToLearn.Base));
   }
   
   public int Attack {
      get { return GetStat(Stat.Attack); }
   }
   
   public int Defense
   {
      get { return GetStat(Stat.Defense); }
   }
   
   public int SpAttack {
      get { return GetStat(Stat.SpAttack); }
   }
   
   public int SpDefense {
      get { return GetStat(Stat.SpDefense); }
   }
   
   public int Speed {
      get { return GetStat(Stat.Speed); }
   }

   public int MaxHp { get; private set; }

   public DamageDetails TakeDamge(Move move, Creature attacker)
   {
      float critical = 1;
      if (Random.value * 100f <= 6.25f)
         critical = 2;
      float type = TypeChart.GetEfectiveness(move.Base.Type, this.Base.Type1) *
                   TypeChart.GetEfectiveness(move.Base.Type, this.Base.Type2);

      var damageDetails = new DamageDetails()
      {
         TypeEffectiveness = type,
         Critical = critical,
         Fainted = false
      };
      
      float attack = (move.Base.Category==MoveCategory.Special) ? attacker.SpAttack : attacker.Attack;
      float defense = (move.Base.Category==MoveCategory.Special) ? SpDefense : Defense;
      
      float modifiers = Random.Range(0.85f, 1f) * type * critical;
      float a = (2 * attacker.Level + 10) / 250f;
      float d = a * move.Base.Power * ((float) attack / defense) + 2;
      int damage = Mathf.FloorToInt(d * modifiers);

      UpdateHP(damage);

      return damageDetails;
   }

   public void UpdateHP(int damage)
   {
      HP = Mathf.Clamp(HP - damage, 0, MaxHp);
      HpChanged = true;
   }

   public void SetStatus(ConditionID conditionId)
   {
      if (Status != null) return;
      
      Status = ConditionDB.Conditions[conditionId];
      Status?.OnStart?.Invoke(this);
      StatusChanges.Enqueue($"{Base.Name} {Status.StartMessage}");
      OnStatusChanged?.Invoke();
   }

   public void CureStatus()
   {
      Status = null;
      OnStatusChanged?.Invoke();
   }

   public Move GetRandomMove()
   {
      var moveWithPP = Moves.Where(x => x.PP > 0).ToList();
      
      int r = Random.Range(0, moveWithPP.Count);
      return moveWithPP[r];
   }

   public bool OnBeforeMove()
   {
      if (Status?.OnBeforeMove != null)
      {
         return Status.OnBeforeMove(this);
      }

      return true;
   }
   
   public void OnAfterTurn()
   {
      Status?.OnAfterTurn?.Invoke(this);
   }

   public void OnBattleOver()
   {
      ResetStatBoost();
      resetHp();
      foreach (var move in Moves)
      {
         move.PP = move.Base.PP;
      }
   }
}
 public class DamageDetails
   {
      public bool Fainted { get; set; }

      public float Critical { get; set; }

      public float TypeEffectiveness { get; set; }
   }