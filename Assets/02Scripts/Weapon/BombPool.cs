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
        for(int i = 0; i < _maxBombNumber; i++){
            GameObject bomb = Instantiate(BombPrefab);
            Bombs.Add(bomb);
            bomb.SetActive(false);
        }

    }

    public void FireBomb(Transform FirePosition, float ThrowPower)
    {
        foreach (GameObject bomb in Bombs)
        {
            if (!bomb.gameObject.activeInHierarchy)
            {
                bomb.SetActive(true);
                bomb.transform.position = FirePosition.position;
                Rigidbody bombRigidbidy = bomb.GetComponent<Rigidbody>();
                bombRigidbidy.AddForce(Camera.main.transform.forward * ThrowPower, ForceMode.Impulse);
                bombRigidbidy.AddTorque(Vector3.one);
                return;
            }
        }
        

        
    }
}
