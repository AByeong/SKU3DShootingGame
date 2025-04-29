using System;
using NUnit.Framework.Internal.Execution;
using UnityEngine;

public class ModelAnchoring : MonoBehaviour
{
    public Transform Anchor;
    public Vector3 Offset;
    private void Update()
    {
        this.transform.position = Anchor.position + Offset;
    }

    public void Sword()
    {
        
    }

    public void Shot()
    {
        
    }

    public void Bomb()
    {
        
    }
}
