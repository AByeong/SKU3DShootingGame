using UnityEngine;
using UnityEngine.UI;

public class UI_Enemy : MonoBehaviour
{
   [SerializeField] private Slider _hpBar;
   public Enemy Enemy;
   private Transform camTransform;
   
   public void Refresh_HPBar(float currentHP)
   {
       _hpBar.maxValue = Enemy.MaxHealth;
       _hpBar.value = currentHP;

   }
   
   private void LateUpdate()
   {
       camTransform = Camera.main.transform;
       transform.LookAt(transform.position + (camTransform.rotation * Vector3.forward));
   }
   
}
