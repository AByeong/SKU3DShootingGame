using UnityEngine;

public interface IDamagable
{

  public DamagedEffect DamagedEffect {get;set; }
  public void TakeDamage(Damage damage);
  
}
