using UnityEngine;

public class Bomb : MonoBehaviour
{
    public CameraManager cameraManager;
    public GameObject ExplosionEffectPrefab;
    public GameObject DecalProjectorPrefab;
    public float DecalLifetime = 10f;

    [SerializeField] private float _radius = 5f;
    private bool _isExploded = false;
    public int ExplosionDamage = 30;
    public float KnockbackPower = 10f;

    private void Awake()
    {
        // CameraManager는 FindObjectOfType보다 인스펙터에서 할당하는 것이 더 안정적일 수 있습니다.
        if (cameraManager == null)
            cameraManager = FindObjectOfType<CameraManager>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_isExploded)

        {

            cameraManager.ShakeCamera(0.5f, 5f);


            // 플레이어 태그가 아닌 오브젝트와 충돌했을 때만 폭발 처리
            if (collision.gameObject.tag != "Player")
            {
                // IDamagable 인터페이스를 가진 객체에게 데미지 전달 시도
                Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, _radius, ~LayerMask.NameToLayer("Player"));
                //Collider[] hitDrumColliders = Physics.OverlapSphere(this.transform.position, _radius, (1 << 10));
                foreach (Collider victim in hitColliders)
                {
                    if (victim.gameObject.tag == "Player")
                    {
                        continue;
                    }

                    _isExploded = true;
                    Damage damage = new Damage
                    {
                        Value = ExplosionDamage,
                        HitDirection = collision.contacts[0].normal * -1, // 충돌 지점의 반대 방향 (밀려나는 방향)
                        KnockBackPower = KnockbackPower,
                        From = this.gameObject
                    };

                    if (victim.TryGetComponent<IDamagable>(out IDamagable damagable))
                    {
                        damagable.TakeDamage(damage);
                    }

                }
                
            }



            // 데미지 전달 여부와 관계없이 폭발 실행
            Explode(collision.contacts[0].point, collision.contacts[0].normal); // 충돌 지점과 법선 전달
        }
    }





    // 폭발 위치와 표면 법선을 인자로 받도록 수정
    private void Explode(Vector3 explosionPoint, Vector3 surfaceNormal)
    {
        
        
        


        // 폭발 이펙트 생성 (폭탄의 현재 위치가 아닌 실제 충돌 지점에 생성하는 것이 더 자연스러울 수 있음)
        if (ExplosionEffectPrefab != null)
        {
            Instantiate(ExplosionEffectPrefab, explosionPoint, Quaternion.LookRotation(surfaceNormal)); // 이펙트 방향도 법선에 맞춤
        }

        // 데칼 프로젝터 생성
        if (DecalProjectorPrefab != null)
        {
            // 데칼 프로젝터의 방향 설정 (예: 바닥이면 아래, 벽이면 벽 반대 방향)
            // 여기서는 충돌 표면 법선의 반대 방향으로 데칼을 투사하도록 설정
            Quaternion decalRotation = Quaternion.LookRotation(-surfaceNormal);

            // 충돌 지점에 데칼 생성. Z-Fighting 방지를 위해 법선 방향으로 약간 띄움
            Vector3 decalPosition = explosionPoint + surfaceNormal * 0.01f;

            GameObject decalInstance = Instantiate(DecalProjectorPrefab, decalPosition, decalRotation);

            // 일정 시간 후 데칼 파괴
            Destroy(decalInstance, DecalLifetime);
        }
        _isExploded = false;
        // 폭탄 오브젝트 비활성화
        this.gameObject.SetActive(false);
    }
}