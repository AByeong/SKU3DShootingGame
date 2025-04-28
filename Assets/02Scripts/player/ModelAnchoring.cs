using System;
using UnityEngine;

public class ModelAnchoring : MonoBehaviour
{
    public Transform Anchor;
    public Vector3 Offset;
    private void Update()
    {
        this.transform.position = Anchor.position + Offset;
    }
}
