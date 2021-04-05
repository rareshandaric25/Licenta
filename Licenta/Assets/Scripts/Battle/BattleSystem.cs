using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, PartyScreen, BattleOver}

public enum BattleAction{ Move, SwitchCreature, UseItem, Run}
public class BattleSystem : MonoBehaviour
{
   [SerializeField] BattleUnit playerUnit;
   [SerializeField] BattleUnit enemyUnit;
   [SerializeField] BattleDialogBox dialogBox;
   [SerializeField] PartyScreen partyScreen;
   [SerializeField] Image playerImage;
   [SerializeField] Image trainerImage;

   public event Action<bool> OnBattleOver;

   BattleState state;
   BattleState? prevState;
   int currentAction;
   int currentMove;
   int currentMember;

   CreatureParty playerParty;
   CreatureParty trainerParty;
   Creature wildCreature;

   bool isTrainerBattle = false;
   PlayerController player;
   TrainerController trainer;
   
   public void StartBattle(CreatureParty playerParty, Creature wildCreature)
   {
      this.playerParty = playerParty;
      this.wildCreature = wildCreature;
      StartCoroutine(SetupBattle());
   }
   
   public void StartTrainerBattle(CreatureParty playerParty, CreatureParty trainerParty)
   {
      this.playerParty = playerParty;
      this.trainerParty = trainerParty;

      isTrainerBattle = true;
      player = playerParty.GetComponent<PlayerController>();
      trainer = trainerParty.GetComponent<TrainerController>();
      
      StartCoroutine(SetupBattle());
   }
   
   public IEnumerator SetupBattle()
   {
      playerUnit.Clear();
      enemyUnit.Clear();
      if (!isTrainerBattle)
      {
         //Wild Creature battle
         playerUnit.Setup(playerParty.GetHeatlyCreature()); 
         enemyUnit.Setup(wildCreature); 
         
         dialogBox.SetMovesNames(playerUnit.Creature.Moves);
         yield return dialogBox.TypeDialog($"A wild {enemyUnit.Creature.Base.Name} appeared.");
      }
      else
      {
         //Trainer Battle
         
         //Show Trainer && Player Sprites
         playerUnit.gameObject.SetActive(false);
         enemyUnit.gameObject.SetActive(false);
         
         playerImage.gameObject.SetActive(true);
         trainerImage.gameObject.SetActive(true);
         playerImage.sprite = player.Sprite;
         trainerImage.sprite = trainer.Sprite;

         yield return dialogBox.TypeDialog($"{trainer.Name} wants to battle");
         
         //Send out first creature of trainer
         trainerImage.gameObject.SetActive(false);
         enemyUnit.gameObject.SetActive(true);
         var enemyCreature = trainerParty.GetHeatlyCreature();
         enemyUnit.Setup(enemyCreature);
         yield return dialogBox.TypeDialog($"{trainer.Name} send out {enemyCreature.Base.Name}");

         //Send out first creature of player
         playerImage.gameObject.SetActive(false);
         playerUnit.gameObject.SetActive(true);
         var playerCreature = playerParty.GetHeatlyCreature();
         playerUnit.Setup(playerCreature);
         yield return dialogBox.TypeDialog($"Go {playerCreature.Base.Name}");
         dialogBox.SetMovesNames(playerUnit.Creature.Moves);
         
      }
      
      partyScreen.Init();
      ActionSelection();
   }
   
   void BattleOver(bool won)
   {
      state = BattleState.BattleOver;
      playerParty.Creatures.ForEach(p => p.OnBattleOver());
      OnBattleOver(won);
   }
   void ActionSelection()
   {
      state = BattleState.ActionSelection;
      dialogBox.SetDialog("Choose an action");
      dialogBox.EnableActionSelector(true);
   }

   void OpenPartyScreen()
   {
      state = BattleState.PartyScreen;
      partyScreen.SetPartyData(playerParty.Creatures);
      partyScreen.gameObject.SetActive(true);
   }
   void MoveSelection()
   {
      state = BattleState.MoveSelection;
      dialogBox.EnableActionSelector(false);
      dialogBox.EnableDialogText(false);
      dialogBox.EnableMoveSelector(true);
   }

   IEnumerator RunTurns(BattleAction playerAction)
   {
      state = BattleState.RunningTurn;

      if (playerAction == BattleAction.Move)
      {
         playerUnit.Creature.CurrentMove = playerUnit.Creature.Moves[currentMove];
         enemyUnit.Creature.CurrentMove = enemyUnit.Creature.GetRandomMove();

         int playerMovePriority = playerUnit.Creature.CurrentMove.Base.Priority;
         int enemeyMovePriority = enemyUnit.Creature.CurrentMove.Base.Priority;
         
         //Check who goes first
         bool playerGoesFirst = true;
         if (enemeyMovePriority > playerMovePriority)
            playerGoesFirst = false;
         else if(enemeyMovePriority == playerMovePriority) 
            playerGoesFirst = playerUnit.Creature.Speed >= enemyUnit.Creature.Speed;

         var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
         var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

         var secondCreature = secondUnit.Creature;
         
         //First Turn
         yield return RunMove(firstUnit, secondUnit, firstUnit.Creature.CurrentMove);
         yield return RunAfterTurn(firstUnit);
         if(state==BattleState.BattleOver) yield break;

         if (secondCreature.HP > 0)
         {
            // Second Turn
            yield return RunMove(secondUnit, firstUnit, secondUnit.Creature.CurrentMove);
            yield return RunAfterTurn(secondUnit);
            if (state == BattleState.BattleOver) yield break;
         }
      }
      else
      {
         if (playerAction == BattleAction.SwitchCreature)
         {
            var selectedCreature = playerParty.Creatures[currentMember];
            state = BattleState.Busy;
            yield return SwitchCreature(selectedCreature);
         }
         
         //Enemy Turn
         var enemyMove = enemyUnit.Creature.GetRandomMove();
         yield return RunMove(enemyUnit, playerUnit, enemyMove);
         yield return RunAfterTurn(enemyUnit);
         if(state==BattleState.BattleOver) yield break;
      }

      if (state != BattleState.BattleOver)
         ActionSelection();
   }
   
   IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
   {
      bool canRunMove = sourceUnit.Creature.OnBeforeMove();
      if (!canRunMove)
      {
         yield return ShowStatusChanges(sourceUnit.Creature);
         yield break;
      }
      yield return ShowStatusChanges(sourceUnit.Creature);
      
      move.PP--;
      yield return dialogBox.TypeDialog($"{sourceUnit.Creature.Base.Name} used {move.Base.Name}");

      if (CheckIfMoveHits(move, sourceUnit.Creature, targetUnit.Creature))
      {
         sourceUnit.PlayAttackAnimation();
         yield return new WaitForSeconds(1f);
         targetUnit.PlayHitAnimation();

         if (move.Base.Category == MoveCategory.Status)
         {
            yield return RunMoveEffect(move.Base.Effects, sourceUnit.Creature, targetUnit.Creature,move.Base.Target);
         }
         else
         {
            var damageDetails = targetUnit.Creature.TakeDamge(move, sourceUnit.Creature);
            yield return targetUnit.Hud.UpdateHP();
            yield return ShowDamageDetails(damageDetails);
         }

         if (move.Base.SecondaryEffects != null && move.Base.SecondaryEffects.Count > 0 && targetUnit.Creature.HP > 0)
         {
            foreach (var secondary in move.Base.SecondaryEffects)
            {
               var rnd = UnityEngine.Random.Range(1, 101);
               if (rnd <= secondary.Chance)
                  yield return RunMoveEffect(secondary, sourceUnit.Creature, targetUnit.Creature, secondary.Target);
            }
         }

         if (targetUnit.Creature.HP <= 0)
         {
            yield return dialogBox.TypeDialog($"{targetUnit.Creature.Base.Name} fainted");
            targetUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);

            CheckForBattleOver(targetUnit);
         }
      }
      else
      {
         yield return dialogBox.TypeDialog($"{sourceUnit.Creature.Base.Name}'s attack missed");
      }
      
   }

   IEnumerator RunMoveEffect(MoveEffects effects, Creature source, Creature target, MoveTarget moveTarget)
   {
      //Stat Boosting
      if (effects.Boosts != null)
      {
         if (moveTarget == MoveTarget.Self)
            source.ApplyBoosts(effects.Boosts);
         else
            target.ApplyBoosts(effects.Boosts);
      }

      //Status Condition
      if (effects.Status != ConditionID.none)
      {
         target.SetStatus(effects.Status);
      }

      yield return ShowStatusChanges(source);
      yield return ShowStatusChanges(target);
   }

   IEnumerator RunAfterTurn(BattleUnit sourceUnit)
   {
      if(state==BattleState.BattleOver) yield break;
      yield return new WaitUntil(() => state == BattleState.RunningTurn);
      
      //Status like poison or burn will hurt the creature after the turn
      sourceUnit.Creature.OnAfterTurn();
      yield return ShowStatusChanges(sourceUnit.Creature);
      yield return sourceUnit.Hud.UpdateHP();
      if (sourceUnit.Creature.HP <= 0) 
      { 
         yield return dialogBox.TypeDialog($"{sourceUnit.Creature.Base.Name} fainted"); 
         sourceUnit.PlayFaintAnimation();
         yield return new WaitForSeconds(2f);
         
         CheckForBattleOver(sourceUnit);
      }
   }

   bool CheckIfMoveHits(Move move, Creature source, Creature target)
   {
      if (move.Base.AlwaysHits)
         return true;
      float moveAccuaracy = move.Base.Accuracy;

      int accuracy = source.StatBoosts[Stat.Accuracy];
      int evasion = target.StatBoosts[Stat.Evasion];
      
      var boostValues = new float[] {1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f};

      if (accuracy > 0)
         moveAccuaracy *= boostValues[accuracy];
      else
         moveAccuaracy /= boostValues[-accuracy];
      
      if (evasion > 0)
         moveAccuaracy /= boostValues[evasion];
      else
         moveAccuaracy *= boostValues[-evasion];
      
      return UnityEngine.Random.Range(1, 101) <= moveAccuaracy;
   }
   IEnumerator ShowStatusChanges(Creature creature)
   {
      while (creature.StatusChanges.Count > 0)
      {
         var message = creature.StatusChanges.Dequeue();
         yield return dialogBox.TypeDialog(message);
      }
   }

   void CheckForBattleOver(BattleUnit faintedUnit)
   {
      if (faintedUnit.IsPlayerUnit)
      {
         var nextCreature = playerParty.GetHeatlyCreature();
         if (nextCreature != null)
            OpenPartyScreen();
         else 
            BattleOver(false);
      }
      else
      {
         if(!isTrainerBattle)
            BattleOver(true);
         else
         {
            var nextCreature = trainerParty.GetHeatlyCreature();
            if (nextCreature != null)
               StartCoroutine(SendNextTrainerCreature(nextCreature));
            else
               BattleOver(true);
         }
      }
   }

   IEnumerator ShowDamageDetails(DamageDetails damageDetails )
   {
      if (damageDetails.Critical > 1f)
      {
         yield return dialogBox.TypeDialog("A critical hit!");
      }

      if (damageDetails.TypeEffectiveness > 1f)
      {
         yield return dialogBox.TypeDialog("It's super effective");
      }
      else if(damageDetails.TypeEffectiveness < 1f)
         yield return dialogBox.TypeDialog("It's not very effective");
   }

   public void HandleUpdate()
   {
      if (state == BattleState.ActionSelection)
      {
         HandleActionSelection();
      }
      else if (state == BattleState.MoveSelection)
      {
         HandleMoveSelection();
      }
      else if (state == BattleState.PartyScreen)
      {
         HandlePartySelection();
      }
   }

   void HandleActionSelection()
   {
      if (Input.GetKeyDown(KeyCode.RightArrow))
         ++currentAction;
      else if (Input.GetKeyDown(KeyCode.LeftArrow))
         --currentAction;
      else if (Input.GetKeyDown(KeyCode.DownArrow))
         currentAction += 2;
      else if (Input.GetKeyDown(KeyCode.UpArrow))
         currentAction -= 2;

      currentAction = Mathf.Clamp(currentAction, 0, 3);
      
      dialogBox.UpdateActionSelection(currentAction);

      if (Input.GetKeyDown(KeyCode.Z))
      {
         if (currentAction == 0)
         {
            //Fight
            MoveSelection();
         }
         else if (currentAction == 1)
         {
            //Bag
         }
         else if (currentAction == 2)
         {
            //Creature
            prevState = state;
            OpenPartyScreen();
         }
         else if (currentAction == 3)
         {
            //Run
         }
      }
   }

   void HandleMoveSelection()
   {
      if (Input.GetKeyDown(KeyCode.RightArrow))
         ++currentMove;
      else if (Input.GetKeyDown(KeyCode.LeftArrow))
         --currentMove;
      else if (Input.GetKeyDown(KeyCode.DownArrow))
         currentMove += 2;
      else if (Input.GetKeyDown(KeyCode.UpArrow))
         currentMove -= 2;

      currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Creature.Moves.Count - 1);
      
      dialogBox.UpdateMoveSelection(currentMove,playerUnit.Creature.Moves[currentMove]);

      if (Input.GetKeyDown(KeyCode.Z))
      {
         var move = playerUnit.Creature.Moves[currentMove];
         if (move.PP == 0) return;
         
         dialogBox.EnableMoveSelector(false);
         dialogBox.EnableDialogText(true);
         StartCoroutine(RunTurns(BattleAction.Move));
      }
      else if (Input.GetKeyDown(KeyCode.X))
      {
         dialogBox.EnableMoveSelector(false);
         dialogBox.EnableDialogText(true);
         ActionSelection();
      }
   }

   void HandlePartySelection()
   {
      if (Input.GetKeyDown(KeyCode.RightArrow))
         ++currentMember;
      else if (Input.GetKeyDown(KeyCode.LeftArrow))
         --currentMember;
      else if (Input.GetKeyDown(KeyCode.DownArrow))
         currentMember += 2;
      else if (Input.GetKeyDown(KeyCode.UpArrow))
         currentMember -= 2;

      currentMember = Mathf.Clamp(currentMember, 0, playerParty.Creatures.Count - 1);
      
      partyScreen.UpdateMemberSelection(currentMember);

      if (Input.GetKeyDown(KeyCode.Z))
      {
         var selectedMember = playerParty.Creatures[currentMember];
         if (selectedMember.HP <= 0)
         {
            partyScreen.SetMessageText("You can't send out a fainted creature");
            return;
         }
         if (selectedMember == playerUnit.Creature)
         {
            partyScreen.SetMessageText("This creature is already in battle");
            return;
         }
         
         partyScreen.gameObject.SetActive(false);

         if (prevState == BattleState.ActionSelection)
         {
            prevState = null;
            StartCoroutine(RunTurns(BattleAction.SwitchCreature));
         }
         else
         {
            state = BattleState.Busy;
            StartCoroutine(SwitchCreature(selectedMember));
         }
      }

      if (Input.GetKeyDown(KeyCode.X))
      {
         partyScreen.gameObject.SetActive(false);
         ActionSelection();
      }
   }

   IEnumerator SwitchCreature(Creature newCreature)
   {
      if (playerUnit.Creature.HP > 0)
      {
         yield return dialogBox.TypeDialog($"Come back {playerUnit.Creature.Base.Name}");
         playerUnit.PlayFaintAnimation();
         yield return new WaitForSeconds(2f);
      }

      playerUnit.Setup(newCreature);
      dialogBox.SetMovesNames(newCreature.Moves);
      yield return dialogBox.TypeDialog($"Go {newCreature.Base.Name}!");

      state = BattleState.RunningTurn;
   }

   IEnumerator SendNextTrainerCreature(Creature nextCreature)
   {
      state = BattleState.Busy;
      
      enemyUnit.Setup(nextCreature);
      yield return dialogBox.TypeDialog($"{trainer.Name} send out {nextCreature.Base.Name}");
      
      state = BattleState.RunningTurn;
   }
   
}
