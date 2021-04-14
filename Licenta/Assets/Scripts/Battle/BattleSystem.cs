﻿using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, PartyScreen, AboutToUse, BattleOver}

public enum BattleAction{ Move, SwitchCreature, UseItem, Run}

public class BattleSystem : MonoBehaviour
{
   [SerializeField] BattleUnit playerUnit;
   [SerializeField] BattleUnit enemyUnit;
   [SerializeField] BattleDialogBox dialogBox;
   [SerializeField] PartyScreen partyScreen;
   [SerializeField] Image playerImage;
   [SerializeField] Image trainerImage;
   [SerializeField] GameObject creatureBallSprite;

   public event Action<bool> OnBattleOver;

   BattleState state;
   BattleState? prevState;
   int currentAction;
   int currentMove;
   int currentMember;
   bool aboutToUseChoice = true;

   CreatureParty playerParty;
   CreatureParty trainerParty;
   Creature wildCreature;

   bool isTrainerBattle = false;
   PlayerController player;
   TrainerController trainer;

   int escapeAttempts;

   public void StartBattle(CreatureParty playerParty, Creature wildCreature)
   {
      this.playerParty = playerParty;
      this.wildCreature = wildCreature;
      player = playerParty.GetComponent<PlayerController>();
      
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

      escapeAttempts = 0;
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

   IEnumerator AboutToUse(Creature newCreature)
   {
      state = BattleState.Busy;
      yield return dialogBox.TypeDialog(
         $"{trainer.Name} is about to use {newCreature.Base.Name}. Do you want to change your creature?");

      state = BattleState.AboutToUse;
      dialogBox.EnableChoiceBox(true);
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
         else if (enemeyMovePriority == playerMovePriority)
            playerGoesFirst = playerUnit.Creature.Speed >= enemyUnit.Creature.Speed;

         var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
         var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

         var secondCreature = secondUnit.Creature;

         //First Turn
         yield return RunMove(firstUnit, secondUnit, firstUnit.Creature.CurrentMove);
         yield return RunAfterTurn(firstUnit);
         if (state == BattleState.BattleOver) yield break;

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
         else if (playerAction == BattleAction.UseItem)
         {
            dialogBox.EnableActionSelector(false);
            yield return ThrowCreatureBall();
         }
         else if (playerAction == BattleAction.Run)
         {
            yield return TryToEscape();
         }

         //Enemy Turn
         var enemyMove = enemyUnit.Creature.GetRandomMove();
         yield return RunMove(enemyUnit, playerUnit, enemyMove);
         yield return RunAfterTurn(enemyUnit);
         if (state == BattleState.BattleOver) yield break;
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
            yield return RunMoveEffect(move.Base.Effects, sourceUnit.Creature, targetUnit.Creature, move.Base.Target);
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
      if (state == BattleState.BattleOver) yield break;
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
         yield return new WaitUntil(() => state == BattleState.RunningTurn);
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
         if (!isTrainerBattle)
            BattleOver(true);
         else
         {
            var nextCreature = trainerParty.GetHeatlyCreature();
            if (nextCreature != null)
               StartCoroutine(AboutToUse(nextCreature));
            else
               BattleOver(true);
         }
      }
   }

   IEnumerator ShowDamageDetails(DamageDetails damageDetails)
   {
      if (damageDetails.Critical > 1f)
      {
         yield return dialogBox.TypeDialog("A critical hit!");
      }

      if (damageDetails.TypeEffectiveness > 1f)
      {
         yield return dialogBox.TypeDialog("It's super effective");
      }
      else if (damageDetails.TypeEffectiveness < 1f)
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
      else if (state == BattleState.AboutToUse)
      {
         HandleAboutToUse();
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
            StartCoroutine(RunTurns(BattleAction.UseItem));
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
            StartCoroutine(RunTurns(BattleAction.Run));
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

      dialogBox.UpdateMoveSelection(currentMove, playerUnit.Creature.Moves[currentMove]);

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
         if (playerUnit.Creature.HP <= 0)
         {
            partyScreen.SetMessageText("You have to choose a creature to continue");
            return;
         }

         partyScreen.gameObject.SetActive(false);

         if (prevState == BattleState.AboutToUse)
         {
            prevState = null;
            StartCoroutine(SendNextTrainerCreature());
         }
         else
            ActionSelection();
      }
   }

   void HandleAboutToUse()
   {
      if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
         aboutToUseChoice = !aboutToUseChoice;

      dialogBox.UpdateChoiceBox(aboutToUseChoice);

      if (Input.GetKeyDown(KeyCode.Z))
      {
         dialogBox.EnableChoiceBox(false);
         if (aboutToUseChoice == true)
         {
            //Yes option
            prevState = BattleState.AboutToUse;
            OpenPartyScreen();
         }
         else
         {
            //No option
            StartCoroutine(SendNextTrainerCreature());
         }
      }
      else if (Input.GetKeyDown(KeyCode.X))
      {
         dialogBox.EnableChoiceBox(false);
         StartCoroutine(SendNextTrainerCreature());
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

      if (prevState == null)
      {
         state = BattleState.RunningTurn;
      }
      else if (prevState == BattleState.AboutToUse)
      {
         prevState = null;
         StartCoroutine(SendNextTrainerCreature());
      }
   }

   IEnumerator SendNextTrainerCreature()
   {
      state = BattleState.Busy;

      var nextCreature = trainerParty.GetHeatlyCreature();
      enemyUnit.Setup(nextCreature);
      yield return dialogBox.TypeDialog($"{trainer.Name} send out {nextCreature.Base.Name}");

      state = BattleState.RunningTurn;
   }

   IEnumerator ThrowCreatureBall()
   {
      state = BattleState.Busy;

      if (isTrainerBattle)
      {
         yield return dialogBox.TypeDialog($"Can't catch a trainer's creature");
         state = BattleState.RunningTurn;
         yield break;
      }

      yield return dialogBox.TypeDialog($"{player.Name} used creature ball!");

      var creatureBallObj = Instantiate(creatureBallSprite, playerUnit.transform.position - new Vector3(2,0), Quaternion.identity);
      var creatureBall = creatureBallObj.GetComponent<SpriteRenderer>();
      
      //Animations
      yield return creatureBall.transform.DOJump(enemyUnit.transform.position + new Vector3(0, 2), 2f, 1, 1f).WaitForCompletion();
      yield return enemyUnit.PlayCaptureAnimation();
      yield return creatureBall.transform.DOMoveY(enemyUnit.transform.position.y - 1.3f, 0.5f).WaitForCompletion();

      int shakeCount = TryToCatchCreature(enemyUnit.Creature);

      for (int i = 0; i < Mathf.Min(shakeCount,3); i++)
      {
         yield return new WaitForSeconds(0.5f);
         yield return creatureBall.transform.DOPunchRotation(new Vector3(0, 0, 10f), 0.8f).WaitForCompletion();
      }

      if (shakeCount == 4)
      {
         //Creature is caught
         yield return dialogBox.TypeDialog($"{enemyUnit.Creature.Base.Name} was caught");
         yield return creatureBall.DOFade(0, 1.5f).WaitForCompletion();
         
         playerParty.AddCreature(enemyUnit.Creature);
         yield return dialogBox.TypeDialog($"{enemyUnit.Creature.Base.Name} has been added to your party");
         
         Destroy(creatureBall);
         BattleOver(true);
      }
      else
      {
         //Creature broke free
         yield return new WaitForSeconds(1f);
         creatureBall.DOFade(0, 0.2f);
         yield return enemyUnit.PlayBreakOutAnimation();

         if (shakeCount < 2)
            yield return dialogBox.TypeDialog($"{enemyUnit.Creature.Base.Name} broke free");
         else
            yield return dialogBox.TypeDialog($"Almost caught it");
         
         Destroy(creatureBall);
         state = BattleState.RunningTurn;
      }
   }

   int TryToCatchCreature(Creature creature)
   {
      float a = (3 * creature.MaxHp - 2 * creature.HP) * creature.Base.CatchRate *
         ConditionDB.GetStatusBonus(creature.Status) / (3 * creature.MaxHp);

      if (a >= 255)
         return 4;

      float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680 / a));

      int shakeCount = 0;
      while (shakeCount < 4)
      {
         if (UnityEngine.Random.Range(0, 65535) >= b)
            break;

         ++shakeCount;
      }

      return shakeCount;
   }

   IEnumerator TryToEscape()
   {
      state = BattleState.Busy;
      if (isTrainerBattle)
      {
         yield return dialogBox.TypeDialog($"You can't run from a trainer battle");
         state = BattleState.RunningTurn;
         yield break;
      }

      ++escapeAttempts;
      
      int playerSpeed = playerUnit.Creature.Speed;
      int enemySpeed = enemyUnit.Creature.Speed;

      if (enemySpeed < playerSpeed)
      {
         yield return dialogBox.TypeDialog($"Ran away safetly.");
         BattleOver(true);
      }
      else
      {
         float f = (playerSpeed * 128) / enemySpeed + 30 * escapeAttempts;
         f = f % 256;

         if (UnityEngine.Random.RandomRange(0, 256) < f)
         {
            yield return dialogBox.TypeDialog($"Ran away safetly.");
            BattleOver(true);
         }
         else
         {
            yield return dialogBox.TypeDialog($"Cant't escape");
            state = BattleState.RunningTurn; 
         }
      }
   }
   

}
