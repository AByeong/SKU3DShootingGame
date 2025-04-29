using UnityEngine;
using UnityEditor;

public class Knife : Weapon
{
    [Header("칼파라미터")]
    public int DamageValue;
    public float DamageAngle;

    private Vector3 lastFireCenter;
    private Vector3 lastFireForward;


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

                bool isClose = distanceToTarget < 1.5f; // 너무 가까운 적 판정 거리 (원하면 숫자 조정 가능)

                if (isClose || angleToTarget <= DamageAngle / 2f)
                {
                    Debug.Log("공격 범위 안에 있는 적 발견: " + collider.name);

                    Damage damage = new Damage();
                    damage.Value = DamageValue;

                    if (collider.CompareTag("Enemy"))
                    {
                        Enemy enemy = collider.GetComponent<Enemy>();
                        if (enemy != null)
                        {
                            enemy.TakeDamage(damage);
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
