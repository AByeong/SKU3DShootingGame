using UnityEngine;

public class Bomb : MonoBehaviour
{
    public GameObject ExplosionEffectPrefab;
    //충돌했을 때

    public int ExplosionDamage = 1;
    public float KnockbackPower = 10f;
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag != "Player")
        {
            IDamagable damagable = collision.gameObject.GetComponent<IDamagable>();

            if (damagable != null)
            {
                Damage damage = new Damage();
                damage.Value = ExplosionDamage;
                damage.HitDirection = collision.contacts[0].normal;
                damage.KnockBackPower = KnockbackPower;
                damage.From = this.gameObject;

                collision.gameObject.GetComponent<Enemy>().TakeDamage(damage);
            }

            Explode();
        }
    }


    private void Explode()
    {
        Instantiate(ExplosionEffectPrefab,this.transform.position,Quaternion.identity);
        this.gameObject.SetActive(false);
    }
    
}
