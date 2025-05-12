using UnityEngine;
using UnityEditor;

public class Knife : Weapon
{
    [Header("칼파라미터")]
    public int DamageValue;
    public float DamageAngle;

    private Vector3 lastFireCenter;
    private Vector3 lastFireForward;
    [SerializeField] private float _closeDistance = 1.5f;

    public override void Initialize()
    {

    }

    public override void Fire()
    {
        Debug.Log("칼!");

        Vector3 targetPoint;
        if (GetAimTargetPoint(out targetPoint))
        {
            Vector3 center = FirePosition.position;
            Vector3 forward = (targetPoint - center).normalized;

            Collider[] colliders = Physics.OverlapSphere(center, MaxShotDistance);
            foreach (var collider in colliders)
            {
                Vector3 dirToTarget = (collider.transform.position - center).normalized;
                float angleToTarget = Vector3.Angle(forward, dirToTarget);
                float distanceToTarget = Vector3.Distance(center, collider.transform.position);

                bool isClose = distanceToTarget < _closeDistance; // 너무 가까운 적 판정 거리 

                if (isClose || angleToTarget <= DamageAngle / 2f)
                {
                    Debug.Log("공격 범위 안에 있는 적 발견: " + collider.name);

                    Damage damage = new Damage();
                    damage.Value = DamageValue;
                    damage.HitTransform = new Vector3(collider.transform.position.x, Camera.main.transform.position.y, collider.transform.position.z);
                    damage.HitDirection = dirToTarget;

                    if (collider.CompareTag("Enemy"))
                    {
                        Enemy enemy = collider.GetComponent<Enemy>();
                        if (enemy != null)
                        {
                            enemy.TakeDamage(damage);
                            //PlayImpactEffect(collider.ClosestPoint(FirePosition.position), (FirePosition.position - collider.transform.position).normalized, 0); // 피격 위치와 플레이어를 바라보는 법선 전달
                            Debug.Log($"{collider.name}에게 {damage.Value}만큼 데미지!");
                        }
                    }
                }
            }

            // 기즈모용 정보 저장
            lastFireCenter = center;
            lastFireForward = forward;
        }
    }
    private void PlayImpactEffect(Vector3 position, Vector3 normal, int type)
    {
        if (BulletEffect != null)
        {
            BulletEffect.transform.position = position; // 이펙트 위치 설정
            BulletEffect.transform.forward = normal; // 표면 노멀(법선)에 맞춰 이펙트 정렬
            BulletEffect.Play(); // 이펙트 재생
        }
        else
        {
            Debug.LogWarning("BulletEffect 배열이 제대로 설정되지 않았거나, 요청된 타입의 이펙트가 없습니다.", this);
        }
    }

    public override void Reroll()
    {

    }

    private void Update()
    {


    }

    private void OnDrawGizmos()
    {
        if (lastFireForward == Vector3.zero)
            return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // 반투명 빨간색

        int segments = 20;
        float deltaAngle = DamageAngle / segments;
        Vector3[] points = new Vector3[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = -DamageAngle / 2f + deltaAngle * i;
            Quaternion rotation = Quaternion.AngleAxis(currentAngle, Vector3.up);
            Vector3 dir = rotation * lastFireForward;
            points[i] = lastFireCenter + dir * MaxShotDistance;
        }

        // 선도 같이 그리기
        for (int i = 0; i < segments; i++)
        {
            Gizmos.DrawLine(points[i], points[i + 1]);
            Gizmos.DrawLine(lastFireCenter, points[i]);
        }

        // ★ Mesh처럼 칠해서 부채꼴을 만듦
        for (int i = 0; i < segments; i++)
        {
            DrawTriangle(lastFireCenter, points[i], points[i + 1]);
        }
    }

    private void DrawTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p1);

#if UNITY_EDITOR
        Handles.color = new Color(1f, 0f, 0f, 0.3f); // 반투명 빨간색
        Handles.DrawAAConvexPolygon(p1, p2, p3);
#endif
    }

}