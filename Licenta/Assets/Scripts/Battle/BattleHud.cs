using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
   [SerializeField] Text nameText;
   [SerializeField] Text levelText;
   [SerializeField] Text statusText;
   [SerializeField] HPBar hpBar;
   [SerializeField] GameObject expBar;

   [SerializeField] Color psnColor;
   [SerializeField] Color slpColor;
   [SerializeField] Color parColor;
   [SerializeField] Color frzColor;


   Creature _creature;
   Dictionary<ConditionID, Color> statusColors;
   
   public void SetData(Creature creature)
   {
      _creature = creature;
      
      nameText.text = creature.Base.Name;
      SetLevel();
      hpBar.SetHP((float) creature.HP / creature.MaxHp);
      SetExp();

      statusColors = new Dictionary<ConditionID, Color>()
      {
         {ConditionID.psn, psnColor},
         {ConditionID.slp, slpColor},
         {ConditionID.par, parColor},
         {ConditionID.frz, frzColor}
      };
      
      SetStatusText();
      _creature.OnStatusChanged += SetStatusText;
   }
   
   void SetStatusText()
   {
      if (_creature.Status == null)
      {
         statusText.text = "";
      }
      else
      {
         statusText.text = _creature.Status.Id.ToString().ToUpper();
         statusText.color = statusColors[_creature.Status.Id];
      }
   }

   public void SetLevel()
   {
      levelText.text = "Lvl " + _creature.Level;
   }
   
   public void SetExp()
   {
      //Only the player has exp bar
      if (expBar == null) return;
    
      float normalizedExp = getNormalizedExp();
      expBar.transform.localScale = new Vector3(normalizedExp, 1, 1);
     
   }
   
   public IEnumerator SetExpSmooth(bool reset=false)
   {
       //Only the player has exp bar
       if (expBar == null) yield break;

       if (reset)
          expBar.transform.localScale = new Vector3(0, 1, 1);
       
       float normalizedExp = getNormalizedExp();
       yield return expBar.transform.DOScaleX(normalizedExp, 1.5f).WaitForCompletion();
   }

   float getNormalizedExp()
   {
      int currLevelExp = _creature.Base.GetExpForLevel(_creature.Level);
      int nextLevelExp = _creature.Base.GetExpForLevel(_creature.Level + 1);

      float normalizedExp = (float)(_creature.Exp - currLevelExp) / (nextLevelExp - currLevelExp);
      return Mathf.Clamp01(normalizedExp);
   }
   
   public IEnumerator UpdateHP()
   {
      if(_creature.HpChanged)
      {
         yield return hpBar.SetHPSmooth((float) _creature.HP / _creature.MaxHp);
         _creature.HpChanged = false;
      }
   }
}
