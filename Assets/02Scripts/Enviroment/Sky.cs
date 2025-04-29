using UnityEngine;

public class Sky : MonoBehaviour
{
    public Material skyboxMaterial; // 인스펙터에서 연결

    [Range(0, 360)]
    public float rotation = 0f; // 초기 회전값

    public float rotationSpeed = 10f;

    void Update()
    {
        if (skyboxMaterial != null)
        {
            rotation += rotationSpeed * Time.deltaTime;
            skyboxMaterial.SetFloat("_Rotation", rotation);
        }
    }
}
