using System;
using System.Collections.Generic;
using UnityEngine;

public class BombPool : MonoBehaviour
{
    public GameObject BombPrefab;
    public List<GameObject> Bombs;
    
    [SerializeField] private int _maxBombNumber;

    private void Start()
    {
        for (int i = 0; i < _maxBombNumber; i++)
        {
            GameObject bomb = Instantiate(BombPrefab);
            Bombs.Add(bomb);
            bomb.SetActive(false);
        }
    }
    

    public void FireBomb(Transform firePosition, Vector3 direction, float throwPower)
    {
        foreach (GameObject bomb in Bombs)
        {
            if (!bomb.gameObject.activeInHierarchy)
            {
                bomb.SetActive(true);
                bomb.transform.position = firePosition.position;

                Rigidbody bombRigidbody = bomb.GetComponent<Rigidbody>();

                // 기존 속도 초기화
                bombRigidbody.linearVelocity = Vector3.zero;
                bombRigidbody.angularVelocity = Vector3.zero;

                // 방향과 세기를 적용하여 폭탄 던지기
                bombRigidbody.AddForce(direction.normalized * throwPower, ForceMode.Impulse);
                bombRigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * 5f); // 회전 추가

                return;
            }
        }
    }
}