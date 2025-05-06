using UnityEngine;
 // using System;

 // 제공된 GenericObjectPool<Enemy>를 상속받습니다.
 public class EnemyPool : GenericObjectPool<Enemy>
 {
     [Header("Enemy 스폰 특정 설정")]
     [Tooltip("순찰 지점 (월드 좌표 사용 가정)")]
     public PatrolPoints PatrolPoints;

     [Tooltip("스폰 활성화 여부")]
     public bool SpawnSwitch = true;

     [Header("Enemy 스폰 파라미터")]
     [Tooltip("적 스폰 간격 (초)")]
     public float SpawnInterval = 2f;
     [Tooltip("스포너 기준 스폰 반경")]
     public float Radius = 5f;

     // 타이머 변수
     private float _timer = 0f;

     // --- Unity Lifecycle Methods ---

     private void Update()
     {
         // GameManager 인스턴스 존재 및 게임 상태 확인 (GameManager가 있다고 가정)
         // 현재 날짜: 2025년 5월 6일
         if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Play && SpawnSwitch)
         {
             _timer += Time.deltaTime;

             if (_timer >= SpawnInterval)
             {
                 SpawnEnemy();
                 _timer = 0f;
             }
         }
         else
         {
             _timer = 0f;
         }
     }

     // --- Public Methods ---

     /// <summary>
     /// 풀에서 사용 가능한 Enemy 오브젝트를 가져옵니다. (외부 호출용 래퍼 함수)
     /// 다른 스크립트와의 호환성을 위해 유지합니다.
     /// </summary>
     /// <returns>활성화된 Enemy 인스턴스 또는 null</returns>
     public Enemy GetObjectFromPool()
     {
         // 내부적으로는 베이스 클래스의 GetPooledObject를 호출합니다.
         return GetPooledObject();
     }

     /// <summary>
     /// 사용이 끝난 Enemy 오브젝트를 풀에 반환합니다. (원래 이름 유지)
     /// </summary>
     /// <param name="enemy">반환할 Enemy 인스턴스</param>
     public void EnemyDie(Enemy enemy) // 요청하신대로 원래 메소드 이름 사용
     {
         // 베이스 클래스의 ReturnPooledObject 호출
         ReturnPooledObject(enemy);
     }


     // --- Private Methods ---

     // 실제 적을 스폰하는 로직
     private void SpawnEnemy()
     {
         // 베이스 클래스의 GetPooledObject()를 직접 호출합니다.
         Enemy enemyInstance = GetPooledObject();

         if (enemyInstance != null)
         {
             

             // 3. 최종 스폰 위치 설정: 계산된 월드 좌표의 X, Z 값을 사용하고, Y 값은 1.1f로 고정
             Vector3 finalSpawnPosition = new Vector3(transform.position.x + Random.Range(0,Radius), 1.1f, transform.position.z+ Random.Range(0,Radius));

             // 4. 부모 설정 해제 및 최종 위치/회전 적용
             enemyInstance.transform.SetParent(null);
             enemyInstance.transform.position = finalSpawnPosition;
             enemyInstance.transform.rotation = Quaternion.identity;

             // 5. 월드 좌표를 사용하는 PatrolPoints 할당
             enemyInstance.PatrolPoints = PatrolPoints;

             // 6. Enemy 초기화 (Enemy 스크립트 내 Initialize 구현 필요)
             enemyInstance.Initialize();

             // Debug.Log($"Enemy spawned: {enemyInstance.name} at world position: {finalSpawnPosition}");
         }
         else
         {
             Debug.LogWarning($"[{typeof(Enemy).Name} Pool] 사용 가능한 Enemy 인스턴스를 가져오지 못했습니다.");
         }
     }
 }