using System.Collections;
using UnityEngine;

public class TractorScript : MonoBehaviour
{
    [SerializeField] private Vector3 _endPosition;
    [SerializeField] private Rigidbody _rigidbody;

    [SerializeField] private GameObject[] _backWheels = new GameObject[2];
    [SerializeField] private float _backWheelRadius = 0.6f;
    [SerializeField] private GameObject[] _frontWheels = new GameObject[2];
    [SerializeField] private float _frontWheelRadius = 0.4f;

    private Vector3 _startPosition;
    private float _speed;

    [SerializeField] private AudioSource _beepAudioSource;

    public void Init()
    {
        _startPosition = transform.position;
        _speed = Vector3.Distance(_startPosition, _endPosition) / GameManager.Instance.gameTime;
    }

    public void FixedUpdate()
    {
        Vector3 direction = (_endPosition - _startPosition).normalized;
        _rigidbody.MovePosition(_rigidbody.position + direction * _speed * Time.fixedDeltaTime);

        float rotationAngleBack = (_speed * Time.fixedDeltaTime) / (2 * Mathf.PI * _backWheelRadius) * 360f;
        foreach (var wheel in _backWheels)
        {
            wheel.transform.Rotate(Vector3.right, rotationAngleBack);
        }

        float rotationAngleFront = (_speed * Time.fixedDeltaTime) / (2 * Mathf.PI * _frontWheelRadius) * 360f;
        foreach (var wheel in _frontWheels)
        {
            wheel.transform.Rotate(Vector3.back, rotationAngleFront);
        }
    }

    public void PlayBeepSound()
    {
        if (_beepAudioSource != null)
        {
            StartCoroutine(DoubleBeepCoroutine());
        }
    }

    private IEnumerator DoubleBeepCoroutine()
    {
        _beepAudioSource.Play();
        yield return new WaitForSeconds(0.4f);
        _beepAudioSource.Play();
    }
}
