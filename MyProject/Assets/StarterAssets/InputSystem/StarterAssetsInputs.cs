using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool crouch;

		public bool leftHandInteract;
		public bool rightHandInteract;
		public bool leftHandMove;
		public bool rightHandMove;

        [Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}

		public void OnCrouch(InputValue value)
		{
			CrouchInput(value.isPressed);
        }

		public void OnLeftHandInteract(InputValue value)
		{
            LeftHandInteractInput(value.isPressed);
        }

		public void OnRightHandInteract(InputValue value)
        {
            RightHandInteractInput(value.isPressed);
        }

		public void OnLeftHandMove(InputValue value)
		{
			LeftHandMoveInput(value.isPressed);
        }

		public void OnRightHandMove(InputValue value)
		{
			RightHandMoveInput(value.isPressed);
        }
#endif


        public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		public void CrouchInput(bool newCrouchState)
		{
			crouch = newCrouchState;
        }

		public void LeftHandInteractInput(bool newLeftHandInteractState)
        {
			leftHandInteract = newLeftHandInteractState;
        }

		public void RightHandInteractInput(bool newRightHandInteractState)
        {
			rightHandInteract = newRightHandInteractState;
        }

		public void LeftHandMoveInput(bool newLeftHandMoveState)
        {
			leftHandMove = newLeftHandMoveState;
        }

		public void RightHandMoveInput(bool newRightHandMoveState)
        {
			rightHandMove = newRightHandMoveState;
        }

        private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}