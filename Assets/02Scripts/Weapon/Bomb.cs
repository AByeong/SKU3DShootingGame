using UnityEngine;

public class Bomb : MonoBehaviour
{
    public CameraManager cameraManager;
    public GameObject ExplosionEffectPrefab;
    public GameObject DecalProjectorPrefab; // <<< 데칼 프로젝터 프리팹 변수 추가
    public float DecalLifetime = 10f; // <<< 데칼 지속 시간 (초)

    public int ExplosionDamage = 1;
    public float KnockbackPower = 10f;

    private void Awake()
    {
        // CameraManager는 FindObjectOfType보다 인스펙터에서 할당하는 것이 더 안정적일 수 있습니다.
        if (cameraManager == null)
            cameraManager = FindObjectOfType<CameraManager>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 플레이어 태그가 아닌 오브젝트와 충돌했을 때만 폭발 처리
        if (collision.gameObject.tag != "Player")
        {
            // IDamagable 인터페이스를 가진 객체에게 데미지 전달 시도
            IDamagable damagable = collision.gameObject.GetComponent<IDamagable>();
            if (damagable != null)
            {
                Damage damage = new Damage
                {
                    Value = ExplosionDamage,
                    HitDirection = collision.contacts[0].normal * -1, // 충돌 지점의 반대 방향 (밀려나는 방향)
                    KnockBackPower = KnockbackPower,
                    From = this.gameObject
                };

                // Enemy 컴포넌트를 직접 참조하기보다 IDamagable 인터페이스를 통해 데미지를 주는 것이 좋습니다.
                // collision.gameObject.GetComponent<Enemy>().TakeDamage(damage); // 이전 코드
                damagable.TakeDamage(damage); // 수정된 코드 (IDamagable 인터페이스 사용)
            }
            else
            {
                // IDamagable이 없는 경우, Enemy 컴포넌트가 있는지 확인 (하위 호환성 또는 다른 로직)
                Enemy enemy = collision.gameObject.GetComponent<Enemy>();
                if (enemy != null)
                {
                     Damage damage = new Damage
                    {
                        Value = ExplosionDamage,
                        HitDirection = collision.contacts[0].normal * -1,
                        KnockBackPower = KnockbackPower,
                        From = this.gameObject
                    };
                    enemy.TakeDamage(damage);
                }
            }

            // 데미지 전달 여부와 관계없이 폭발 실행
            Explode(collision.contacts[0].point, collision.contacts[0].normal); // 충돌 지점과 법선 전달
        }
    }

    // 폭발 위치와 표면 법선을 인자로 받도록 수정
    private void Explode(Vector3 explosionPoint, Vector3 surfaceNormal)
    {
        if (cameraManager != null)
        {
            cameraManager.ShakeCamera();
        }
        else
        {
             Debug.LogWarning("CameraManager가 Bomb 스크립트에 할당되지 않았습니다.");
        }


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

        // 폭탄 오브젝트 비활성화 또는 파괴
        // Destroy(this.gameObject); // SetActive(false) 대신 Destroy를 고려해볼 수 있습니다.
        this.gameObject.SetActive(false);
    }
}