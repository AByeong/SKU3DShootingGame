using System;
using UnityEngine;

public class CameraQuerterView : MonoBehaviour
{
    public Transform Target;
public Vector3 Offset;
    private void Start()
    { Camera camera = this.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 30;
        Offset = new Vector3(60, 150, -60);
    }

    private void Update()
    {
         
        //interpolling,smoothibg같은 보간 기법이 들어갈 예정
        // 쿼터뷰 기본 각도
        
        transform.position = Target.position + Offset;

        // 타겟을 바라보게 회전
        transform.LookAt(Target);
        
    }
}
