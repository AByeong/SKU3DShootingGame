using System.Collections.Generic;
using UnityEngine;

public class SwapWeapon : MonoBehaviour
{
   public List<Weapon> Weapons;
   [SerializeField] private PlayerFire _playerFire;
   [SerializeField] private int _weaponIndex;

   private void Start()
   {
      Initialize();
   }
   
   public void Initialize()
   {
      _playerFire.Weapon = Weapons[0];
   }

   public void swap(int i)
   {

      Debug.Log($"{i}로 스왑!");
      _playerFire.Weapon = Weapons[i];
      
      DeactivateAllWeapons();
      ActivateWeapon(i);
      
   }

   private void DeactivateAllWeapons()
   {

      foreach (var weapen in Weapons)
      {
         weapen.gameObject.SetActive(false);
      }
      
   }

   private void ActivateWeapon(int i)
   {
      Weapons[i].gameObject.SetActive(true);
   }
   
   
   
   
}
