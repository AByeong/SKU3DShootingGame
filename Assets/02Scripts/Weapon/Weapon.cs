using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class Weapon : MonoBehaviour
{
    public abstract void Fire();
    public abstract void Reload();
    
}