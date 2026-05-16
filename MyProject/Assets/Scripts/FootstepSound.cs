using UnityEngine;

public class FootstepSound : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _footstepSounds;
    public float footstepInterval = 1f;

    private float _timer;
    private Vector3 _lastPos;

    public void Start()
    {
        _lastPos = transform.position;
    }

    public void Update()
    {
        _timer += (transform.position - _lastPos).magnitude / footstepInterval;
        _lastPos = transform.position;

        if (_timer >= 1f)
        {
            PlayRandomFootstep();
            _timer -= 1f;
        }
    }

    private void PlayRandomFootstep()
    {
        if (_audioSource == null || _footstepSounds.Length == 0)
            return;

        int randomIndex = Random.Range(0, _footstepSounds.Length);
        AudioClip clipToPlay = _footstepSounds[randomIndex];

        _audioSource.PlayOneShot(clipToPlay);
    }
}
