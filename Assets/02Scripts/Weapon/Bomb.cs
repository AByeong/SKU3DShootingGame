using UnityEngine;

public class Bomb : MonoBehaviour
{
    //목표 : 마우스의 오른쪽 버튼을 누르면  카메라가 바라보는 방향으로 수류탄을 던지고 싶다.
    
    //1. 수류탄 오브젝트 만들기
    //2. 오른쪽 버튼 입력받기
    //3. 발사 위치에 수류탄 생성하지
    //4. 생성된 수류탄을 카메라 방향으로 물리적인 힘 가하기
    public GameObject ExplosionEffectPrefab;
    //충돌했을 때

    public int ExplosionDamage = 1;
    public float KnockbackPower = 10f;
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Enemy")
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


    private void Explode()
    {
        Instantiate(ExplosionEffectPrefab,this.transform.position,Quaternion.identity);
        this.gameObject.SetActive(false);
    }
    
}
