using System;
using UnityEngine;

public abstract class GameState : MonoBehaviour
{

   public virtual void Excute()
   {
   }

   public void ChangeState(GameManager.GameState newState)
   {
      Debug.Log($"{GameManager.Instance.CurrentState.ToString()} -> {newState.ToString()}");
      GameManager.Instance.CurrentState = newState;
   }
   
}
