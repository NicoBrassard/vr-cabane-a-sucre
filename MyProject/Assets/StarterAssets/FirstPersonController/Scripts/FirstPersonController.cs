using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 4.0f;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;
        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.1f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.5f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

        // cinemachine
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // crouch
        [Header("Crouch")]
        [SerializeField] public float CrouchHeight = 1.0f;
        [SerializeField] public float CrouchSpeedMultiplier = 0.5f;
        [SerializeField] public float CrouchSmoothTime = 0.12f;

        private float _originalHeight;
        private Vector3 _originalCenter;
        private float _heightVelocity;
        private float _centerVelocity;
        private float _cameraYVelocity;
        private float _originalCameraY;

        // Hand interaction settings
        [Header("Hand Interaction")]
        [SerializeField] private float _grabMaxDistance = 2.0f;
        // rapprocher par défaut (z réduit)
        [SerializeField] private Vector3 _leftHoldLocal = new Vector3(-0.4f, 0f, 0.6f);
        [SerializeField] private Vector3 _rightHoldLocal = new Vector3(0.4f, 0f, 0.6f);
        [SerializeField] private float _rotationSpeedHands = 120f;
        [SerializeField] private Transform _leftHoldPoint;
        [SerializeField] private Transform _rightHoldPoint;
        [SerializeField] private bool _debugHands = false;

        // offset pour élever légèrement les seaux plastiques saisis
        [SerializeField] private float _plasticHoldHeightOffset = 0.15f;

        // vertical control settings for held objects (settable in Inspector)
        [SerializeField] private float _handVerticalSpeed = 0.8f;
        [SerializeField] private float _handVerticalLimit = 0.6f;

        // current vertical offsets applied to local hold positions
        private float _leftVerticalOffset = 0f;
        private float _rightVerticalOffset = 0f;

        // regular buckets
        private BucketInteraction _leftGrabbed;
        private Rigidbody _leftGrabbedRb;
        private Transform _leftOriginalParent;
        private bool _leftOriginalKinematic;
        private Quaternion _leftGrabbedRotOffset = Quaternion.identity;
        private float _leftManualSpin = 0f;
        private bool _leftReturnSpin = false;

        private BucketInteraction _rightGrabbed;
        private Rigidbody _rightGrabbedRb;
        private Transform _rightOriginalParent;
        private bool _rightOriginalKinematic;
        private Quaternion _rightGrabbedRotOffset = Quaternion.identity;
        private float _rightManualSpin = 0f;
        private bool _rightReturnSpin = false;

        // plastic buckets (distinct type)
        private PlasticBucketInteraction _leftPlasticGrabbed;
        private Rigidbody _leftPlasticRb;
        private Transform _leftPlasticOriginalParent;
        private bool _leftPlasticOriginalKinematic;
        private float _leftPlasticOriginalY;
        private Quaternion _leftPlasticRotOffset = Quaternion.identity;

        private PlasticBucketInteraction _rightPlasticGrabbed;
        private Rigidbody _rightPlasticRb;
        private Transform _rightPlasticOriginalParent;
        private bool _rightPlasticOriginalKinematic;
        private float _rightPlasticOriginalY;
        private Quaternion _rightPlasticRotOffset = Quaternion.identity;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private bool _prevLeftInteract = false;
        private bool _prevRightInteract = false;
        private bool _prevLeftMove = false;
        private bool _prevRightMove = false;

        private const float _threshold = 0.01f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            if (_controller != null)
            {
                _originalHeight = _controller.height;
                _originalCenter = _controller.center;
            }
            if (CinemachineCameraTarget != null)
            {
                _originalCameraY = CinemachineCameraTarget.transform.localPosition.y;
            }

            if (_mainCamera != null)
            {
                if (_leftHoldPoint == null)
                {
                    GameObject go = new GameObject("LeftHoldPoint");
                    go.transform.SetParent(_mainCamera.transform, false);
                    go.transform.localPosition = _leftHoldLocal;
                    go.transform.localRotation = Quaternion.identity;
                    _leftHoldPoint = go.transform;
                }
                if (_rightHoldPoint == null)
                {
                    GameObject go = new GameObject("RightHoldPoint");
                    go.transform.SetParent(_mainCamera.transform, false);
                    go.transform.localPosition = _rightHoldLocal;
                    go.transform.localRotation = Quaternion.identity;
                    _rightHoldPoint = go.transform;
                }
            }
        }

        private void Update()
        {
            JumpAndGravity();
            Crouch();
            GroundedCheck();
            HandInteractions();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
        }

        private void CameraRotation()
        {
            if (_input != null && (_input.leftHandMove || _input.rightHandMove))
                return;

            if (_input.look.sqrMagnitude >= _threshold)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }

        private void Move()
        {
            float baseSpeed = _input.crouch ? MoveSpeed * CrouchSpeedMultiplier : MoveSpeed;
            float targetSpeed = (_input.sprint && !_input.crouch) ? SprintSpeed : baseSpeed;

            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
            if (_input.move != Vector2.zero)
            {
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
            }

            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void Crouch()
        {
            if (_controller == null || _input == null)
                return;

            float targetHeight = _input.crouch ? CrouchHeight : _originalHeight;
            _controller.height = Mathf.SmoothDamp(_controller.height, targetHeight, ref _heightVelocity, CrouchSmoothTime);

            float targetCenterY = _originalCenter.y - (_originalHeight - targetHeight) / 2f;
            Vector3 center = _controller.center;
            center.y = Mathf.SmoothDamp(center.y, targetCenterY, ref _centerVelocity, CrouchSmoothTime);
            _controller.center = center;

            if (CinemachineCameraTarget != null)
            {
                Vector3 camLocal = CinemachineCameraTarget.transform.localPosition;
                float targetCamY = _originalCameraY - (_originalHeight - targetHeight);
                camLocal.y = Mathf.SmoothDamp(camLocal.y, targetCamY, ref _cameraYVelocity, CrouchSmoothTime);
                CinemachineCameraTarget.transform.localPosition = camLocal;
            }
        }

        private void HandInteractions()
        {
            if (_input == null || _mainCamera == null)
                return;

            // detect button edges
            bool leftInteractDown = _input.leftHandInteract && !_prevLeftInteract;
            bool rightInteractDown = _input.rightHandInteract && !_prevRightInteract;

            // LEFT HAND interact toggle
            if (leftInteractDown)
            {
                if (_leftGrabbed == null && _leftPlasticGrabbed == null)
                {
                    if (TryPickBucketInFront(out BucketInteraction found))
                    {
                        GrabLeft(found);
                    }
                    else if (TryPickPlasticBucketInFront(out PlasticBucketInteraction plasticFound))
                    {
                        GrabLeftPlastic(plasticFound);
                    }
                }
                else
                {
                    // release either kind
                    if (_leftPlasticGrabbed != null) ReleaseLeftPlastic();
                    else ReleaseLeft();
                }
            }

            // RIGHT HAND interact toggle
            if (rightInteractDown)
            {
                if (_rightGrabbed == null && _rightPlasticGrabbed == null)
                {
                    if (TryPickBucketInFront(out BucketInteraction found))
                    {
                        GrabRight(found);
                    }
                    else if (TryPickPlasticBucketInFront(out PlasticBucketInteraction plasticFound))
                    {
                        GrabRightPlastic(plasticFound);
                    }
                }
                else
                {
                    if (_rightPlasticGrabbed != null) ReleaseRightPlastic();
                    else ReleaseRight();
                }
            }

            // HANDLE manual rotation input while move button is held
            bool leftMove = _input.leftHandMove;
            bool rightMove = _input.rightHandMove;

            // accumulate manual spin; actual application happens in UpdateHeldObjects to preserve correct base rotation
            if (leftMove && (_leftGrabbed != null || _leftPlasticGrabbed != null))
            {
                _leftManualSpin += _input.look.x * _rotationSpeedHands * Time.deltaTime;
                _leftReturnSpin = false;
            }
            else if (!leftMove && _prevLeftMove && (_leftGrabbed != null || _leftPlasticGrabbed != null))
            {
                // release edge: start returning spin to zero
                _leftReturnSpin = true;
            }

            if (rightMove && (_rightGrabbed != null || _rightPlasticGrabbed != null))
            {
                _rightManualSpin += _input.look.x * _rotationSpeedHands * Time.deltaTime;
                _rightReturnSpin = false;
            }
            else if (!rightMove && _prevRightMove && (_rightGrabbed != null || _rightPlasticGrabbed != null))
            {
                // release edge: start returning spin to zero
                _rightReturnSpin = true;
            }

            // New: allow opposite-hand move to adjust vertical position when opposite hand is free
            // If left hand is moving and holding an object, and right move is also held and right hand empty -> adjust left vertical offset using look.y
            if (( _leftGrabbed != null || _leftPlasticGrabbed != null ) && leftMove && rightMove && _rightGrabbed == null && _rightPlasticGrabbed == null)
            {
                _leftVerticalOffset += _input.look.y * _handVerticalSpeed * Time.deltaTime;
                _leftVerticalOffset = Mathf.Clamp(_leftVerticalOffset, -_handVerticalLimit, _handVerticalLimit);
            }

            // Mirror: right hand vertical control when left hand is empty
            if (( _rightGrabbed != null || _rightPlasticGrabbed != null ) && rightMove && leftMove && _leftGrabbed == null && _leftPlasticGrabbed == null)
            {
                _rightVerticalOffset += _input.look.y * _handVerticalSpeed * Time.deltaTime;
                _rightVerticalOffset = Mathf.Clamp(_rightVerticalOffset, -_handVerticalLimit, _handVerticalLimit);
            }

            // Update positions & rotations of held objects
            UpdateHeldObjects();

            // update previous states
            _prevLeftInteract = _input.leftHandInteract;
            _prevRightInteract = _input.rightHandInteract;
            _prevLeftMove = _input.leftHandMove;
            _prevRightMove = _input.rightHandMove;
        }

        // Try to raycast from camera forward and return first suitable BucketInteraction within distance
        private bool TryPickBucketInFront(out BucketInteraction found)
        {
            found = null;
            Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _grabMaxDistance))
            {
                // prefer BucketInteraction on hit or parent
                var b = hit.collider.GetComponentInParent<BucketInteraction>();
                if (b != null && !b.IsGrabbed)
                {
                    // ensure not too far vertically (optional)
                    found = b;
                    if (_debugHands) Debug.Log("Found grabbable: " + b.name);
                    return true;
                }
            }
            return false;
        }

        private bool TryPickPlasticBucketInFront(out PlasticBucketInteraction found)
        {
            found = null;
            Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _grabMaxDistance))
            {
                // prefer PlasticBucketInteraction on hit or parent
                var p = hit.collider.GetComponentInParent<PlasticBucketInteraction>();
                if (p != null && !p.IsGrabbed)
                {
                    // ensure not too far vertically (optional)
                    found = p;
                    if (_debugHands) Debug.Log("Found plastic grabbable: " + p.name);
                    return true;
                }
            }
            return false;
        }

        private void GrabLeftPlastic(PlasticBucketInteraction target)
        {
            if (target == null) return;
            _leftPlasticGrabbed = target;
            _leftPlasticOriginalParent = target.transform.parent;
            _leftPlasticRb = target.GetComponent<Rigidbody>();
            if (_leftPlasticRb != null)
            {
                _leftPlasticOriginalKinematic = _leftPlasticRb.isKinematic;
                _leftPlasticRb.isKinematic = true;
            }

            target.transform.SetParent(null, true);
            _leftPlasticOriginalY = target.transform.position.y;

            // zero X/Z rotation (keep yaw)
            Vector3 e = target.transform.eulerAngles;
            float yaw = e.y;
            target.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // base rotation aligned with player yaw
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            Quaternion baseRot = Quaternion.LookRotation(forward, Vector3.up);

            _leftPlasticRotOffset = Quaternion.Inverse(baseRot) * target.transform.rotation;

            _leftManualSpin = 0f;
            _leftReturnSpin = false;

            // compute vertical offset so the object keeps same world Y relative to player/camera on grab
            float camY = (_mainCamera != null) ? _mainCamera.transform.position.y : 0f;
            // target.y should equal original object Y. For plastics we also include the plastic hold height offset as base
            _leftVerticalOffset = target.transform.position.y - camY - (_leftHoldLocal.y + _plasticHoldHeightOffset);
            _leftVerticalOffset = Mathf.Clamp(_leftVerticalOffset, -_handVerticalLimit, _handVerticalLimit);

            // position relative to camera/player; apply plastic height offset in local Y
            Vector3 localWithOffset = new Vector3(_leftHoldLocal.x, _leftHoldLocal.y + _plasticHoldHeightOffset + _leftVerticalOffset, _leftHoldLocal.z);
            target.transform.position = GetHoldTargetPosition(localWithOffset);
            target.transform.rotation = baseRot * _leftPlasticRotOffset;

            target.OnHandleGrab();
        }

        private void ReleaseLeftPlastic()
        {
            if (_leftPlasticGrabbed == null) return;
            var target = _leftPlasticGrabbed;
            target.OnHandleRelease();
            target.transform.SetParent(_leftPlasticOriginalParent, true);
            if (_leftPlasticRb != null)
            {
                _leftPlasticRb.isKinematic = _leftPlasticOriginalKinematic;
            }
            _leftPlasticGrabbed = null;
            _leftPlasticRb = null;
            _leftPlasticOriginalParent = null;
            _leftManualSpin = 0f;
            _leftPlasticRotOffset = Quaternion.identity;
            _leftReturnSpin = false;
            _leftVerticalOffset = 0f;
        }

        private void GrabRightPlastic(PlasticBucketInteraction target)
        {
            if (target == null) return;
            _rightPlasticGrabbed = target;
            _rightPlasticOriginalParent = target.transform.parent;
            _rightPlasticRb = target.GetComponent<Rigidbody>();
            if (_rightPlasticRb != null)
            {
                _rightPlasticOriginalKinematic = _rightPlasticRb.isKinematic;
                _rightPlasticRb.isKinematic = true;
            }

            target.transform.SetParent(null, true);
            _rightPlasticOriginalY = target.transform.position.y;

            // zero X/Z rotation (keep yaw)
            Vector3 e = target.transform.eulerAngles;
            float yaw = e.y;
            target.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            Quaternion baseRot = Quaternion.LookRotation(forward, Vector3.up);

            _rightPlasticRotOffset = Quaternion.Inverse(baseRot) * target.transform.rotation;
            _rightManualSpin = 0f;
            _rightReturnSpin = false;

            // compute vertical offset so the object keeps same world Y relative to player/camera on grab
            float camY = (_mainCamera != null) ? _mainCamera.transform.position.y : 0f;
            _rightVerticalOffset = target.transform.position.y - camY - (_rightHoldLocal.y + _plasticHoldHeightOffset);
            _rightVerticalOffset = Mathf.Clamp(_rightVerticalOffset, -_handVerticalLimit, _handVerticalLimit);

            Vector3 localWithOffset = new Vector3(_rightHoldLocal.x, _rightHoldLocal.y + _plasticHoldHeightOffset + _rightVerticalOffset, _rightHoldLocal.z);
            target.transform.position = GetHoldTargetPosition(localWithOffset);
            target.transform.rotation = baseRot * _rightPlasticRotOffset;

            target.OnHandleGrab();
        }

        private void ReleaseRightPlastic()
        {
            if (_rightPlasticGrabbed == null) return;
            var target = _rightPlasticGrabbed;
            target.OnHandleRelease();
            target.transform.SetParent(_rightPlasticOriginalParent, true);
            if (_rightPlasticRb != null)
            {
                _rightPlasticRb.isKinematic = _rightPlasticOriginalKinematic;
            }
            _rightPlasticGrabbed = null;
            _rightPlasticRb = null;
            _rightPlasticOriginalParent = null;
            _rightManualSpin = 0f;
            _rightPlasticRotOffset = Quaternion.identity;
            _rightReturnSpin = false;
            _rightVerticalOffset = 0f;
        }

        private void GrabLeft(BucketInteraction target)
        {
            if (target == null) return;
            _leftGrabbed = target;
            _leftOriginalParent = target.transform.parent;
            _leftGrabbedRb = target.GetComponent<Rigidbody>();
            if (_leftGrabbedRb != null)
            {
                _leftOriginalKinematic = _leftGrabbedRb.isKinematic;
                _leftGrabbedRb.isKinematic = true;
            }

            target.transform.SetParent(null, true);

            Vector3 e = target.transform.eulerAngles;
            float yaw = e.y;
            target.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            Quaternion baseRot = Quaternion.LookRotation(forward, Vector3.up);

            _leftGrabbedRotOffset = Quaternion.Inverse(baseRot) * target.transform.rotation;
            _leftManualSpin = 0f;
            _leftReturnSpin = false;

            // compute vertical offset so the object keeps same world Y relative to player/camera on grab
            float camY = (_mainCamera != null) ? _mainCamera.transform.position.y : 0f;
            _leftVerticalOffset = target.transform.position.y - camY - _leftHoldLocal.y;
            _leftVerticalOffset = Mathf.Clamp(_leftVerticalOffset, -_handVerticalLimit, _handVerticalLimit);

            // position now anchored relative to camera/player (no dependence on original world Y)
            Vector3 local = new Vector3(_leftHoldLocal.x, _leftHoldLocal.y + _leftVerticalOffset, _leftHoldLocal.z);
            target.transform.position = GetHoldTargetPosition(local);
            target.transform.rotation = baseRot * _leftGrabbedRotOffset;

            target.OnGrab();
        }

        private void ReleaseLeft()
        {
            if (_leftGrabbed == null) return;
            var target = _leftGrabbed;
            target.OnRelease();
            target.transform.SetParent(_leftOriginalParent, true);
            if (_leftGrabbedRb != null)
            {
                _leftGrabbedRb.isKinematic = _leftOriginalKinematic;
            }
            _leftGrabbed = null;
            _leftGrabbedRb = null;
            _leftOriginalParent = null;
            _leftManualSpin = 0f;
            _leftGrabbedRotOffset = Quaternion.identity;
            _leftReturnSpin = false;
            _leftVerticalOffset = 0f;
        }

        private void GrabRight(BucketInteraction target)
        {
            if (target == null) return;
            _rightGrabbed = target;
            _rightOriginalParent = target.transform.parent;
            _rightGrabbedRb = target.GetComponent<Rigidbody>();
            if (_rightGrabbedRb != null)
            {
                _rightOriginalKinematic = _rightGrabbedRb.isKinematic;
                _rightGrabbedRb.isKinematic = true;
            }

            target.transform.SetParent(null, true);

            Vector3 e = target.transform.eulerAngles;
            float yaw = e.y;
            target.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            Quaternion baseRot = Quaternion.LookRotation(forward, Vector3.up);

            _rightGrabbedRotOffset = Quaternion.Inverse(baseRot) * target.transform.rotation;
            _rightManualSpin = 0f;
            _rightReturnSpin = false;

            // compute vertical offset so the object keeps same world Y relative to player/camera on grab
            float camY = (_mainCamera != null) ? _mainCamera.transform.position.y : 0f;
            _rightVerticalOffset = target.transform.position.y - camY - _rightHoldLocal.y;
            _rightVerticalOffset = Mathf.Clamp(_rightVerticalOffset, -_handVerticalLimit, _handVerticalLimit);

            Vector3 local = new Vector3(_rightHoldLocal.x, _rightHoldLocal.y + _rightVerticalOffset, _rightHoldLocal.z);
            target.transform.position = GetHoldTargetPosition(local);
            target.transform.rotation = baseRot * _rightGrabbedRotOffset;

            target.OnGrab();
        }

        private void ReleaseRight()
        {
            if (_rightGrabbed == null) return;
            var target = _rightGrabbed;
            target.OnRelease();
            target.transform.SetParent(_rightOriginalParent, true);
            if (_rightGrabbedRb != null)
            {
                _rightGrabbedRb.isKinematic = _rightOriginalKinematic;
            }
            _rightGrabbed = null;
            _rightGrabbedRb = null;
            _rightOriginalParent = null;
            _rightManualSpin = 0f;
            _rightGrabbedRotOffset = Quaternion.identity;
            _rightReturnSpin = false;
            _rightVerticalOffset = 0f;
        }

        // Compute hold position relative to camera/player.
        // The provided localHold is in camera-local coordinates (x = right, y = up, z = forward).
        // This function uses the player's yaw for the forward direction so the held object doesn't follow camera pitch.
        private Vector3 GetHoldTargetPosition(Vector3 localHold)
        {
            Transform cam = _mainCamera.transform;

            // use player's yaw-only forward to avoid camera pitch affecting hold direction
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward = (forward.sqrMagnitude > 0.0001f) ? forward.normalized : Vector3.forward;

            Vector3 right = transform.right;
            right.y = 0f;
            right = right.normalized;

            Vector3 horizontalOffset = right * localHold.x + forward * localHold.z;

            Vector3 target = cam.position + horizontalOffset;
            // vertical offset is relative to camera height
            target.y = cam.position.y + localHold.y;
            return target;
        }

        // Update held objects positions and rotations (called each frame from HandInteractions)
        private void UpdateHeldObjects()
        {
            if (_mainCamera == null)
                return;

            // compute player forward yaw-only for base rotation (so object follows player yaw, not camera pitch)
            Vector3 playerForward = transform.forward;
            playerForward.y = 0f;
            playerForward = playerForward.sqrMagnitude > 0.0001f ? playerForward.normalized : Vector3.forward;
            Quaternion baseRot = Quaternion.LookRotation(playerForward, Vector3.up);

            // handle returning manual spin to zero if requested
            if (_leftReturnSpin)
            {
                float step = _rotationSpeedHands * 5 * Time.deltaTime;
                if (Mathf.Abs(_leftManualSpin) <= step)
                {
                    _leftManualSpin = 0f;
                    _leftReturnSpin = false;
                }
                else
                {
                    _leftManualSpin -= Mathf.Sign(_leftManualSpin) * step;
                }
            }

            if (_rightReturnSpin)
            {
                float step = _rotationSpeedHands * 5 * Time.deltaTime;
                if (Mathf.Abs(_rightManualSpin) <= step)
                {
                    _rightManualSpin = 0f;
                    _rightReturnSpin = false;
                }
                else
                {
                    _rightManualSpin -= Mathf.Sign(_rightManualSpin) * step;
                }
            }

            if (_leftGrabbed != null)
            {
                Vector3 local = new Vector3(_leftHoldLocal.x, _leftHoldLocal.y + _leftVerticalOffset, _leftHoldLocal.z);
                Vector3 target = GetHoldTargetPosition(local);
                _leftGrabbed.transform.position = Vector3.Lerp(_leftGrabbed.transform.position, target, 10f * Time.deltaTime);
                Quaternion spin = Quaternion.AngleAxis(_leftManualSpin, playerForward);
                _leftGrabbed.transform.rotation = spin * baseRot * _leftGrabbedRotOffset;
            }
            else if (_leftPlasticGrabbed != null)
            {
                Vector3 localWithOffset = new Vector3(_leftHoldLocal.x, _leftHoldLocal.y + _plasticHoldHeightOffset + _leftVerticalOffset, _leftHoldLocal.z);
                Vector3 target = GetHoldTargetPosition(localWithOffset);
                _leftPlasticGrabbed.transform.position = Vector3.Lerp(_leftPlasticGrabbed.transform.position, target, 10f * Time.deltaTime);
                Quaternion spin = Quaternion.AngleAxis(_leftManualSpin, playerForward);
                _leftPlasticGrabbed.transform.rotation = spin * baseRot * _leftPlasticRotOffset;
            }

            if (_rightGrabbed != null)
            {
                Vector3 local = new Vector3(_rightHoldLocal.x, _rightHoldLocal.y + _rightVerticalOffset, _rightHoldLocal.z);
                Vector3 target = GetHoldTargetPosition(local);
                _rightGrabbed.transform.position = Vector3.Lerp(_rightGrabbed.transform.position, target, 10f * Time.deltaTime);
                Quaternion spin = Quaternion.AngleAxis(_rightManualSpin, playerForward);
                _rightGrabbed.transform.rotation = spin * baseRot * _rightGrabbedRotOffset;
            }
            else if (_rightPlasticGrabbed != null)
            {
                Vector3 localWithOffset = new Vector3(_rightHoldLocal.x, _rightHoldLocal.y + _plasticHoldHeightOffset + _rightVerticalOffset, _rightHoldLocal.z);
                Vector3 target = GetHoldTargetPosition(localWithOffset);
                _rightPlasticGrabbed.transform.position = Vector3.Lerp(_rightPlasticGrabbed.transform.position, target, 10f * Time.deltaTime);
                Quaternion spin = Quaternion.AngleAxis(_rightManualSpin, playerForward);
                _rightPlasticGrabbed.transform.rotation = spin * baseRot * _rightPlasticRotOffset;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);

            if (_debugHands && Application.isPlaying && _mainCamera != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(_mainCamera.transform.position, _mainCamera.transform.position + _mainCamera.transform.forward * _grabMaxDistance);
            }
        }
    }
}