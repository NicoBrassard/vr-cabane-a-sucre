using Oculus.Interaction;
using UnityEngine;

public class BucketInteraction : MonoBehaviour
{
    [SerializeField] private GrabInteractable _interactable;
    public Rigidbody connectedBody;

    private bool _isGrabbed = false;
    public bool IsGrabbed => _isGrabbed;

    private bool _isInHookRange = false;
    private bool _isHooked = true;
    public bool IsHooked => _isHooked;

    private Joint _joint;
    private Vector3 _originalPosition;
    private float _orginalYRotation;

    [SerializeField] private BucketContentScript _bucketContent;
    public BucketContentScript BucketContent => _bucketContent;

    private float _previousWeight = 0f;

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _hookInRangeSound;
    [SerializeField] private AudioClip _hookingSound;
    [SerializeField] private AudioClip[] _metalBucketSounds;
    [SerializeField] private AudioClip[] _groundCollidingSounds;

    public void Awake()
    {
        _originalPosition = transform.position;
        _orginalYRotation = transform.eulerAngles.y;

        if (connectedBody != null)
            CreateJoint();
    }

    public void Update()
    {
        if (GameManager.Instance.isVrMode)
        {
            bool currentlyGrabbed = _interactable.State == InteractableState.Select;
            if (currentlyGrabbed && !_isGrabbed)
            {
                OnGrab();
            }
            else if (!currentlyGrabbed && _isGrabbed)
            {
                OnRelease();
            }
        }

        if (_isGrabbed && _previousWeight != _bucketContent.GetWeight())
        {
            float weightDifference = _bucketContent.GetWeight() - _previousWeight;
            GameManager.Instance.UpdatePlayerSpeed(-weightDifference);
            _previousWeight = _bucketContent.GetWeight();
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Trigger entered: " + other.name);

        if (!_isHooked && other.CompareTag("Hook"))
        {
            _isInHookRange = true;
            _audioSource.PlayOneShot(_hookInRangeSound);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        //Debug.Log("Trigger exited: " + other.name);

        if (!_isHooked && other.CompareTag("Hook"))
        {
            _isInHookRange = false;
        }
    }

    public void OnGrab()
    {
        _isGrabbed = true;
        //Debug.Log("Bucket grabbed");

        if (_joint != null)
        {
            Destroy(_joint);
            _joint = null;
            _isHooked = false;

            int randomIndex = Random.Range(0, _metalBucketSounds.Length);
            AudioClip clipToPlay = _metalBucketSounds[randomIndex];
            _audioSource.PlayOneShot(clipToPlay);
        }

        if (_bucketContent != null)
        {
            GameManager.Instance.UpdatePlayerSpeed(-_bucketContent.GetWeight());
            _previousWeight = _bucketContent.GetWeight();
        }
    }

    public void OnRelease()
    {
        _isGrabbed = false;
        //Debug.Log("Bucket released");

        if (_isInHookRange)
        {
            CreateJoint();
            _isHooked = true;
            _audioSource.PlayOneShot(_hookingSound);
        }

        if (_bucketContent != null)
        {
            GameManager.Instance.UpdatePlayerSpeed(+_bucketContent.GetWeight());
        }
    }

    public void CreateJoint()
    {
        if (_joint == null)
        {
            transform.position = _originalPosition;
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, _orginalYRotation, 0);

            Joint newJoint = gameObject.AddComponent<HingeJoint>();
            newJoint.connectedBody = connectedBody;
            _joint = newJoint;
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Terrain") && !_isGrabbed)
        {
            int randomIndex = Random.Range(0, _groundCollidingSounds.Length);
            AudioClip clipToPlay = _groundCollidingSounds[randomIndex];
            _audioSource.PlayOneShot(clipToPlay);
        }
    }
}
