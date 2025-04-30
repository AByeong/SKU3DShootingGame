using UnityEngine;

public class EliteAttackEvent : MonoBehaviour
{
    public EliteEnemy MyEnemy;

    public void AttackEvent()
    {
        //MyEnemy.Attack();
        MyEnemy.Hit();
    }
}
