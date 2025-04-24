using System;
using System.Collections;
using UnityEngine;

// Enemy 컴포넌트를 풀링하는 GenericObjectPool을 상속
public class EnemyPool : GenericObjectPool<Enemy>
{
     [Tooltip("스폰 활성화 여부")]
     public bool SpawnSwitch = true;
     [Tooltip("스폰 간격 (초)")]
     public float SpawnInterval = 2f;


     private void Start()
     {

          StartCoroutine(SpawnEnemy());
     }

     // 적을 스폰하는 코루틴
     IEnumerator SpawnEnemy()
     {
          // SpawnSwitch가 true인 동안 반복
          while (SpawnSwitch)
          {
               Enemy enemyInstance = GetObjectFromPool();
               
               if (enemyInstance != null)
               {

                    enemyInstance.transform.position = this.transform.position; // 풀 매니저 위치에 스폰
                    enemyInstance.transform.rotation = Quaternion.identity;   // 기본 회전값

                     enemyInstance.Initialize(); // Enemy 스크립트에 이런 함수가 있다고 가정
               }
               else
               {

                    Debug.LogWarning("EnemyPool에서 사용 가능한 Enemy 인스턴스를 가져오지 못했습니다. 풀 크기나 설정을 확인하세요.");

               }


               yield return new WaitForSeconds(SpawnInterval);
          }
     }

     // 풀에서 오브젝트를 가져오는 래퍼 함수 (필수는 아님)
     public Enemy GetObjectFromPool()
     {
          // Base 클래스의 GetPooledObject() 호출
          return GetPooledObject();
     }

     // 적이 죽었을 때 풀에 반환하는 래퍼 함수 (Enemy 스크립트에서 호출)
     public void EnemyDie(Enemy enemy)
     {
          // Base 클래스의 ReturnPooledObject() 호출
          ReturnPooledObject(enemy);
     }
}