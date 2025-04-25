using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PatrolPoints", menuName = "Scriptable Objects/PatrolPoints")]
public class PatrolPoints : ScriptableObject
{
    public List<Vector3> patrolPoints = new List<Vector3>();
}
