using Oculus.Interaction;
using UnityEngine;

public class PlasticBucketInteraction : MonoBehaviour
{
    [SerializeField] private BucketContentScript _bucketContent;
    [SerializeField] private GrabInteractable _handleInteractable;

    private bool _isHandleGrabbed = false;
    public bool IsGrabbed => _isHandleGrabbed;

    private float _previousWeight = 0f;

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _groundCollidingSounds;

    public void Update()
    {
        if (GameManager.Instance.isVrMode)
        {
            bool handleCurrentlyGrabbed = _handleInteractable.State == InteractableState.Select;
            if (handleCurrentlyGrabbed && !_isHandleGrabbed)
            {
                OnHandleGrab();
            }
            else if (!handleCurrentlyGrabbed && _isHandleGrabbed)
            {
                OnHandleRelease();
            }
        }

        if (_isHandleGrabbed && _previousWeight != _bucketContent.GetWeight())
        {
            float weightDifference = _bucketContent.GetWeight() - _previousWeight;
            GameManager.Instance.UpdatePlayerSpeed(-weightDifference);
            _previousWeight = _bucketContent.GetWeight();
        }
    }

    public void OnHandleGrab()
    {
        _isHandleGrabbed = true;

        if (_bucketContent != null)
        {
            GameManager.Instance.UpdatePlayerSpeed(-_bucketContent.GetWeight());
            _previousWeight = _bucketContent.GetWeight();
        }
    }

    public void OnHandleRelease()
    {
        _isHandleGrabbed = false;

        if (_bucketContent != null)
        {
            GameManager.Instance.UpdatePlayerSpeed(+_bucketContent.GetWeight());
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Terrain") && !_isHandleGrabbed)
        {
            int randomIndex = Random.Range(0, _groundCollidingSounds.Length);
            AudioClip clipToPlay = _groundCollidingSounds[randomIndex];
            _audioSource.PlayOneShot(clipToPlay);
        }
    }
}
