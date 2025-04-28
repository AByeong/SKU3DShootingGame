using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    public Transform Target;
    public Vector3 Offset;
    private void Update()
    {
        
        //interpolling,smoothibg같은 보간 기법이 들어갈 예정
        transform.position = Target.position;
        transform.localPosition += Offset;

    }


}
