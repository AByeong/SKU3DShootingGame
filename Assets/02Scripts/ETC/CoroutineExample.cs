using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineExample : MonoBehaviour
{
   public float Cooltime = 0;
   private float _timer = 0;

   private void Start()
   {
      StartCoroutine(WaitTime(3f));
      
      
       Debug.Log("안농안농~~");
   }

   private IEnumerator WaitTime(float waitTime)
   {
      
      yield return new WaitForSeconds(waitTime);
      Debug.Log("Cool~~");
   }
   
   
}
