using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Creature", menuName = "Creature/Create New Creture")]
public class CreatureBase: ScriptableObject
{
   [SerializeField] string name;
   
   [TextArea]
   [SerializeField] string description;

   [SerializeField] Sprite frontsprite;
   [SerializeField] Sprite backsprite;

   //Type
   [SerializeField] CreatureType type1;
   [SerializeField] CreatureType type2;
   
   //Base Stats
   [SerializeField] int maxHp;
   [SerializeField] int attack;
   [SerializeField] int defense;
   [SerializeField] int spAttack;
   [SerializeField] int spDefense;
   [SerializeField] int speed;

   [SerializeField] int catchRate = 255;

   [SerializeField] List<LearnableMove> learnableMoves;
   
   public string Name {
      get { return name; }
   }
   
   public string Description {
      get { return description; }
   }
   
   public Sprite Frontsprite {
      get { return frontsprite; }
   }
   
   public Sprite Backsprite {
      get { return backsprite; }
   }
   
   public CreatureType Type1 {
      get { return type1; }
   }
   
   public CreatureType Type2 {
      get { return type2; }
   }

   public int MaxHp {
      get { return maxHp; }
   }
   
   public int Attack {
      get { return attack; }
   }
   
   public int Defense {
      get { return defense; }
   }
   
   public int SpAttack {
      get { return spAttack; }
   }
   
   public int SpDefense {
      get { return spDefense; }
   }
   
   public int Speed {
      get { return speed; }
   }

   public List<LearnableMove> LearnableMoves {
      get { return learnableMoves; }
   }
   
   public int CatchRate => catchRate;
}

[System.Serializable]
public class LearnableMove
{
   [SerializeField] MoveBase moveBase;
   [SerializeField] int level;

   public MoveBase Base {
      get { return moveBase; }
   }
   
   public int Level {
      get { return level; }
   }
}

public enum CreatureType
{
   None,
   Normal,
   Fire,
   Water,
   Electric,
   Grass,
   Ice,
   Fighting,
   Poison,
   Ground,
   Flying,
   Psychic,
   Bug,
   Rock,
   Ghost,
   Dark,
   Dragon,
   Steel
}

public enum Stat
{
   Attack,
   Defense,
   SpAttack,
   SpDefense,
   Speed,
   
   //Used for move 
   Accuracy,
   Evasion
}
public class TypeChart
{
   static float[][] chart =
   {  //                       Nor  Fir   Wat  Ele  Gra  Ice  Fig  Poi  Gro  Fly  Psy  Bugg  Roc  Gho  Dra  Dar  Ste
      /* Normal */new float[] { 1f,  1f,   1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f, 0.5f, 0f,  1f,  1f, 0.5f },
      /* Fire   */new float[] { 1f, 0.5f, 0.5f, 1f,  2f,  2f,  1f,  1f,  1f,  1f,  1f,  2f, 0.5f, 1f, 0.5f, 1f,  2f },
      /* Water  */new float[] { 1f,  2f,  0.5f, 2f, 0.5f, 1f,  1f,  1f,  2f,  1f,  1f,  1f,  2f,  1f, 0.5f, 1f,  1f },
      /* Electr */new float[] { 1f,  1f,   2f, 0.5f,0.5f, 2f,  1f,  1f,  0f,  2f,  1f,  1f,  1f,  1f, 0.5f, 1f,  1f },
      /* Grass  */new float[] { 1f, 0.5f,  2f,  1f, 0.5f, 1f,  1f, 0.5f, 2f, 0.5f, 1f, 0.5f, 2f,  1f, 0.5f, 1f, 0.5f },
      /* Ice    */new float[] { 1f, 0.5f, 0.5f, 1f,  2f, 0.5f, 1f,  1f,  2f,  2f,  1f,  1f,  1f,  1f,  2f,  1f, 0.5f },
      /* Fighti */new float[] { 2f,  1f,   1f,  1f,  1f,  2f,  1f, 0.5f, 1f, 0.5f, 0.5f, 0.5f, 2f, 0f, 1f,  2f,  2f },      
      /* Poison */new float[] { 1f,  1f,   1f,  1f,  2f,  1f,  1f, 0.5f, 0.5f, 1f, 1f,  1f, 0.5f, 0.5f, 1f, 1f,  0f },  
      /* Ground */new float[] { 1f,  2f,   1f,  2f, 0.5f, 1f,  1f,  2f,  1f,  0f,  1f, 0.5f, 2f,  1f,  1f,  1f,  2f },
      /* Flying */new float[] { 1f,  1f,   1f, 0.5f, 2f,  1f,  2f,  1f,  1f,  1f,  1f,  2f, 0.5f, 1f,  1f,  1f, 0.5f },
      /* Psychic*/new float[] { 1f,  1f,   1f,  1f,  1f,  1f,  2f,  2f,  1f,  1f, 0.5f, 1f,  1f,  1f,  1f,  0f, 0.5f },
      /* Bugg   */new float[] { 1f, 0.5f,  1f,  1f,  2f, 1f, 0.5f, 0.5f, 1f, 0.5f, 2f,  1f,  1f, 0.5f, 1f,  2f, 0.5f },
      /* Rock   */new float[] { 1f,  2f,   1f,  1f,  1f,  2f, 0.5f, 1f, 0.5f, 2f,  1f,  2f,  1f,  1f,  1f,  1f, 0.5f },
      /* Ghost  */new float[] { 0f,  1f,   1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  2f,  1f,  1f,  2f, 1f, 0.5f, 0.5f },
      /* Dragon */new float[] { 1f,  1f,   1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  1f,  2f,  1f,  0.5f},
      /* Dark   */new float[] { 1f,  1f,   1f,  1f,  1f,  1f, 0.5f, 1f,  1f,  1f,  2f,  1f,  1f,  2f, 1f, 0.5f, 0.5f },
      /* Steel  */new float[] { 1f, 0.5f, 0.5f, 0.5f, 1f, 2f,  1f,  1f,  1f,  1f,  1f,  1f,  2f,  1f,  1f,  1f,  0.5f }
   };
   
   public static float GetEfectiveness(CreatureType attackType, CreatureType defenseType)
   {
      if (attackType == CreatureType.None || defenseType == CreatureType.None)
         return 1;

      int raw = (int) attackType - 1;
      int col = (int) defenseType - 1;

      return chart[raw][col];

   }
}
