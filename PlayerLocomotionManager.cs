using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DK
{
    public class PlayerLocomotionManager : CharacterLocomotionManager
    {
        PlayerManager player;

        [HideInInspector] public float verticalMovement;
        [HideInInspector] public float horizontalMovement;
        [HideInInspector] public float moveAmount;

        [Header("Movement Settings")]
        private Vector3 movementDirection;
        private Vector3 targetRotatioDirection;
        [SerializeField] float walkingSpeed =  2;
        [SerializeField] float runningSpeed = 5;
        [SerializeField] float rotationSpeed = 15;

        [Header("Dodge")]
        private Vector3 rollDirection;

        protected override void Awake()
        {
            base.Awake();

            player = GetComponent<PlayerManager>();
        }

        protected override void Update()
        {
            base.Update();

            if (player.IsOwner)
            {
                player.characterNetworkManager.verticalMovement.Value = verticalMovement;
                player.characterNetworkManager.horizontalMovement.Value = horizontalMovement;
                player.characterNetworkManager.moveAmount.Value = moveAmount;
            }
            else
            {
                verticalMovement = player.characterNetworkManager.verticalMovement.Value;
                horizontalMovement = player.characterNetworkManager.horizontalMovement.Value;
                moveAmount = player.characterNetworkManager.moveAmount.Value;

                // IF NOT LOCKED ON, PASS MOVE AMOUNT
                player.playerAnimatorManager.UpdateAnimatorMovementParameters(0, moveAmount);

                // IF LOCKED ON, PASS THE HORIZONTAL AND VERTICAL MOVEMENT
            }
        }

        public void HandleAllMovement()
        { 
            HandleGroundedMovement();
            HandleRotation();
            // AERIAL MOVEMENT
        }

        private void GetMovementValues()
        {
            verticalMovement = PlayerInputManager.instance.verticalInput;
            horizontalMovement = PlayerInputManager.instance.horizontalInput;
            moveAmount = PlayerInputManager.instance.moveAmount;

            // CLAMP THE MOVEMENTS
        }

        private void HandleGroundedMovement()
        {
            if (!player.canMove)
                return;

            GetMovementValues();

            // OUR MOVEMENT DIRECTION IS BASED ON OUR CAMERAS FACING PERSPECTIVE & OUR MOVEMENT INPUTS
            movementDirection = PlayerCamera.instance.transform.forward * verticalMovement;
            movementDirection = movementDirection + PlayerCamera.instance.transform.right * horizontalMovement;
            movementDirection.Normalize();
            movementDirection.y = 0;

            if (PlayerInputManager.instance.moveAmount > 0.5f)
            {
                // MOVE AT RUNNING SPEED
                player.characterController.Move(movementDirection * runningSpeed * Time.deltaTime);
            }
            else if (PlayerInputManager.instance.moveAmount <= 0.5f)
            {
                // MOVE AT WALKING SPEED
                player.characterController.Move(movementDirection * walkingSpeed * Time.deltaTime);
            }
        }

        private void HandleRotation()
        {
            if (!player.canRotate)
                return;

            targetRotatioDirection = Vector3.zero;
            targetRotatioDirection = PlayerCamera.instance.cameraObject.transform.forward * verticalMovement;
            targetRotatioDirection = targetRotatioDirection + PlayerCamera.instance.cameraObject.transform.right * horizontalMovement;
            targetRotatioDirection.Normalize();
            targetRotatioDirection.y = 0;

            if (targetRotatioDirection == Vector3.zero)
            { 
                targetRotatioDirection = transform.forward;
            }

            Quaternion newRotation = Quaternion.LookRotation(targetRotatioDirection);
            Quaternion targetRotation = Quaternion.Slerp(transform.rotation, newRotation, rotationSpeed * Time.deltaTime);
            transform.rotation = targetRotation;
        }

        public void AttemptToPerformDodge()
        {
            if (player.isPerformingAction)
                return;

            // IF WE ARE MOVING WHEN WE ATTEMPT TO DODGE, WE PERFORM A ROLL
            if (PlayerInputManager.instance.moveAmount > 0)
            {
                rollDirection = PlayerCamera.instance.cameraObject.transform.forward * PlayerInputManager.instance.verticalInput;
                rollDirection += PlayerCamera.instance.cameraObject.transform.right * PlayerInputManager.instance.horizontalInput;

                rollDirection.y = 0;
                rollDirection.Normalize();
                Quaternion playerRotation = Quaternion.LookRotation(rollDirection);
                player.transform.rotation = playerRotation;

                // PERFORM A ROLL ANIMATION
                player.playerAnimatorManager.PlayTargetActionAnimation("Roll_Forward_01", true, true);
            }
            // IF WE ARE STATIONARY, WE PERFORM A BACKSTEP
            else
            {
                // PERFORM A BACKSTEP ANIMATION
                player.playerAnimatorManager.PlayTargetActionAnimation("BackStep_01", true, true);
            }
        }
    }
}