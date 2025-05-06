using System;
using System.Collections;
using UnityEngine;
using Random = System.Random;

// Enemy 컴포넌트를 풀링하는 GenericObjectPool을 상속
public class EliteEnemyPool : MultiGenericObjectPool<EliteEnemy>
{
     
     [Header("스폰 설정")]
     public PatrolPoints PatrolPoints;
     [Tooltip("스폰 활성화 여부")]
     public bool SpawnSwitch = true;

     public GameObject[] Monsters;
     
     [Header("스폰파라미터")]
     public float SpawnInterval = 2f;
     public float Radius = 5f;


     private float _timer = 0f;
     

     private void Update()
     {
//          Debug.Log("Enemypool Triggered");
          if (GameManager.Instance.CurrentState == GameManager.GameState.Play && SpawnSwitch)
          {
               _timer += Time.deltaTime;
          }

          if (_timer >= SpawnInterval)
          {
               SpawnEnemy();
               _timer = 0f;
          }
     }


     private void SpawnEnemy()
     {
          
              

                    EliteEnemy enemyInstance = GetObjectFromPool();

                    if (enemyInstance != null)
                    {
                         Vector3 enemyPosition = new Vector3(transform.position.x + UnityEngine.Random.Range(0, Radius),
                              transform.position.y, transform.position.z + UnityEngine.Random.Range(0, Radius));

                         enemyInstance.transform.position = enemyPosition; // 풀 매니저 위치에 스폰
                         enemyInstance.transform.rotation = Quaternion.identity; // 기본 회전값

                         enemyInstance.Initialize(); // Enemy 스크립트에 이런 함수가 있다고 가정
                         enemyInstance.PatrolPoints = PatrolPoints;
                    }
                    else
                    {

                         Debug.LogWarning("EnemyPool에서 사용 가능한 Enemy 인스턴스를 가져오지 못했습니다. 풀 크기나 설정을 확인하세요.");

                    }
                    
               
          

     }

     // 풀에서 오브젝트를 가져오는 래퍼 함수 (필수는 아님)
     public EliteEnemy GetObjectFromPool()
     {
          // Base 클래스의 GetPooledObject() 호출
          return GetPooledObject();
     }

     // 적이 죽었을 때 풀에 반환하는 래퍼 함수 (Enemy 스크립트에서 호출)
     public void EnemyDie(EliteEnemy enemy)
     {
          // Base 클래스의 ReturnPooledObject() 호출
          ReturnPooledObject(enemy);
     }
}