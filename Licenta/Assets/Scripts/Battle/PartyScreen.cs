using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyScreen : MonoBehaviour
{
    [SerializeField] Text messageText;
    
    PartyMemberUi[] memberSlots;
    List<Creature> creatures;

    int selection = 0;

    public Creature Selectedmember => creatures[selection];

    //Party screen can be called from different states(calledFrom = prevState before we open the party screen)
    public BattleState? CalledFrom { get; set; }
    
    public void Init()
    {
        memberSlots = GetComponentsInChildren<PartyMemberUi>(true);
    }

    public void SetPartyData(List<Creature> creatures)
    {
        this.creatures = creatures;
        for (int i = 0; i < memberSlots.Length; i++)
        {
            if (i < creatures.Count)
            {
                memberSlots[i].gameObject.SetActive(true);
                memberSlots[i].SetData(creatures[i]);
            }
            else
                memberSlots[i].gameObject.SetActive(false);
        }

        messageText.text = "Choose a Creature";
    }

       public void HandleUpdate(Action onSelected, Action onBack)
       {
          if (Input.GetKeyDown(KeyCode.RightArrow))
             ++selection;
          else if (Input.GetKeyDown(KeyCode.LeftArrow))
             --selection;
          else if (Input.GetKeyDown(KeyCode.DownArrow))
             selection += 2;
          else if (Input.GetKeyDown(KeyCode.UpArrow))
             selection -= 2;
    
          selection = Mathf.Clamp(selection, 0, creatures.Count - 1);
          
          UpdateMemberSelection(selection);
    
          if (Input.GetKeyDown(KeyCode.Z))
          {
             onSelected?.Invoke();
          }
    
          if (Input.GetKeyDown(KeyCode.X))
          {
              onBack.Invoke();
          }
       }
       
    public void UpdateMemberSelection(int selectedMember)
    {
        for (int i = 0; i < creatures.Count; i++)
        {
            if(i==selectedMember)
                memberSlots[i].SetSelected(true);
            else
                memberSlots[i].SetSelected(false);
        }
    }

    public void SetMessageText(string message)
    {
        messageText.text = message;
    }
}
