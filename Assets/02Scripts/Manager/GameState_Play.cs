using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;

public class GameState_Play : GameState
{
   [SerializeField] private Canvas _playCanvas;
   private bool isStarted = false;
   public override void Excute()
   {
      
      
      _playCanvas.gameObject.SetActive(true);
   }

   
   
}
