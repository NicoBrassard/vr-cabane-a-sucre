using UnityEngine;

public class UIScript : MonoBehaviour
{
    [SerializeField] private Transform _camera;
    [SerializeField] private float _distanceFromCamera = 1.2f;

    private Vector3 _positionVelocity;
    private float _rotVelY;

    public void Update()
    {
        Vector3 forward = _camera.forward;
        Vector3 forwardXZ = new Vector3(forward.x, 0f, forward.z);

        const float eps = 1e-4f;
        Vector3 direction;
        if (forwardXZ.sqrMagnitude > eps)
            direction = forwardXZ.normalized;
        else
        {
            // Cas extrême : caméra parfaitement verticale -> utiliser la direction horizontale de la rotation Y
            float yaw = _camera.rotation.eulerAngles.y;
            direction = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        }

        Vector3 targetPos = _camera.position + direction * _distanceFromCamera;
        targetPos.y = transform.position.y;

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _positionVelocity, 0.12f);

        float smoothY = Mathf.SmoothDampAngle(transform.eulerAngles.y, _camera.rotation.eulerAngles.y, ref _rotVelY, 0.12f);
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, smoothY, transform.eulerAngles.z);
    }
}
