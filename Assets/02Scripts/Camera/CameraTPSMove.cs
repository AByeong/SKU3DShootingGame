using System;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraTPSMove : MonoBehaviour
{
    public Transform ObjectToFollow;
    [SerializeField] private float _followSpeed = 10f;
    [SerializeField] private float _sensitivity = 100f;
    [SerializeField] private float _clampAngle = 70f;

    [SerializeField] private float _rotX;
    [SerializeField] private float _rotY;
    
    public Transform CameraTransform;
    [SerializeField] private Vector3 _dirNormalized;
    [SerializeField] private Vector3 _finalDirenction;
    [SerializeField] private float _maxDistance;
    [SerializeField] private float _minDistance;
    [SerializeField] private float _finalDistance;
    [SerializeField] private float _smoothing = 10f;
    

    private void Awake()
    {
        
    }

    private void Start()
    {
        _rotX = transform.localRotation.eulerAngles.x;
        _rotY = transform.localRotation.eulerAngles.y;
        
        
        
        _dirNormalized = CameraTransform.localPosition.normalized;
        _finalDistance = CameraTransform.localPosition.magnitude;
        //Cursor.visible = false;
    }


    private void Update()
    {
        _rotX += Input.GetAxis("Mouse Y") * _sensitivity*Time.deltaTime;
        _rotY += Input.GetAxis("Mouse X") * _sensitivity*Time.deltaTime;
        
        _rotX = Mathf.Clamp(_rotX,-_clampAngle,_clampAngle);
        
        Quaternion rot = Quaternion.Euler(_rotX, _rotY, 0);
        transform.rotation = rot;
    }

    private void LateUpdate()
    {
        transform.position = Vector3.MoveTowards(transform.position, ObjectToFollow.position, _followSpeed * Time.deltaTime);

        _finalDirenction = transform.TransformPoint(_dirNormalized * _maxDistance);

        RaycastHit hit;

        if (Physics.Linecast(transform.position, _finalDirenction, out hit))
        {
            _finalDistance = Mathf.Clamp(hit.distance, _minDistance, _maxDistance);
        }
        else
        {
            _finalDistance = _maxDistance;
        }
        CameraTransform.localPosition = Vector3.Lerp(CameraTransform.localPosition, _dirNormalized*_finalDistance, Time.deltaTime*_smoothing);
    }
}
