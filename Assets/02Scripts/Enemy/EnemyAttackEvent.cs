using UnityEngine;

public class EnemyAttackEvent : MonoBehaviour
{
    public Enemy MyEnemy;

    public void AttackEvent()
    {
        //MyEnemy.Attack();
        MyEnemy.Hit();
    }
}
