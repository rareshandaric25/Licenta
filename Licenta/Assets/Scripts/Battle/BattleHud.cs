using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
   [SerializeField] Text nameText;
   [SerializeField] Text levelText;
   [SerializeField] Text statusText;
   [SerializeField] HPBar hpBar;

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
      levelText.text = "Lvl " + creature.Level;
      hpBar.SetHP((float) creature.HP / creature.MaxHp);

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

   public IEnumerator UpdateHP()
   {
      if(_creature.HpChanged)
      {
         yield return hpBar.SetHPSmooth((float) _creature.HP / _creature.MaxHp);
         _creature.HpChanged = false;
      }
   }
}
