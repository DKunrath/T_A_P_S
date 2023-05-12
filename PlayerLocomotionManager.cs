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
            
            // INSTANTIATING THE PLAYER COMPONENT IN THE SCENE
            player = GetComponent<PlayerManager>();
        }

        protected override void Update()
        {
            base.Update();
            
            // IF THE PLAYER IN SCENE IS THE OBJECT THAT THIS IP IS CONTROLING
            // PERFORM THE MOVEMENT IN THIS OBJECT
            if (player.IsOwner)
            {
                player.characterNetworkManager.verticalMovement.Value = verticalMovement;
                player.characterNetworkManager.horizontalMovement.Value = horizontalMovement;
                player.characterNetworkManager.moveAmount.Value = moveAmount;
            }
            // IF THE PLAYER IN SCENE IS NOT HE OBJECT THAT THIS IP IS CONTROLING
            // PERFORM THE MOVEMENT FOR THE OTHER OBJECT IN SCENE
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
            // GET THE MOVEMENT AMOUNT FROM THE INPUT MANAGER
            verticalMovement = PlayerInputManager.instance.verticalInput;
            horizontalMovement = PlayerInputManager.instance.horizontalInput;
            moveAmount = PlayerInputManager.instance.moveAmount;

            // CLAMP THE MOVEMENTS
        }

        private void HandleGroundedMovement()
        {
            // IF THE PLAYER CANT MOVE, RETURN
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
            // IF THE PLAYER CANT ROTATE, RETURN
            if (!player.canRotate)
                return;
            
            // INITIATE THE VECTOR WITH ZERO
            // THEN GET THE VERTICAL AND HORIZONTAL MOVEMENT FROM THE CAMERA
            // NORMALIZE THE VECTOR TO GET RID OF SUBSTANCIAL VALUES
            // TRANSFORM THE MOVEMENT AMOUNT IN Y TO 0
            targetRotatioDirection = Vector3.zero;
            targetRotatioDirection = PlayerCamera.instance.cameraObject.transform.forward * verticalMovement;
            targetRotatioDirection = targetRotatioDirection + PlayerCamera.instance.cameraObject.transform.right * horizontalMovement;
            targetRotatioDirection.Normalize();
            targetRotatioDirection.y = 0;
            
            // IF THE CAMERA ROTATION IS ZERO, GIVE THE ROTATION VECTOR THE POSITION 0 OF THE CAMERA
            if (targetRotatioDirection == Vector3.zero)
            { 
                targetRotatioDirection = transform.forward;
            }

            // CREATE A NEW ROTATION QUATERNION TO PERFORM THE ROTATION IN THE PLAYER VIEW
            // USING SLERP TO ACHIEVE A SMOOTH TRANSITION
            Quaternion newRotation = Quaternion.LookRotation(targetRotatioDirection);
            Quaternion targetRotation = Quaternion.Slerp(transform.rotation, newRotation, rotationSpeed * Time.deltaTime);
            transform.rotation = targetRotation;
        }

        public void AttemptToPerformDodge()
        {
            // IF THE PLAYER IS DOING ANY ACTION, RETURN
            if (player.isPerformingAction)
                return;

            // IF WE ARE MOVING WHEN WE ATTEMPT TO DODGE, WE PERFORM A ROLL
            if (PlayerInputManager.instance.moveAmount > 0)
            {
                // GET THE VERTICAL AND HORIZONTAL MOVEMENT FROM THE CAMERA TIMES THE MOVEMENT FROM THE PLAYER INPUTS
                rollDirection = PlayerCamera.instance.cameraObject.transform.forward * PlayerInputManager.instance.verticalInput;
                rollDirection += PlayerCamera.instance.cameraObject.transform.right * PlayerInputManager.instance.horizontalInput;
                
                // NORMALIZE THE VECTOR TO GET RID OF SUBSTANCIAL VALUES
                // TRANSFORM THE MOVEMENT AMOUNT IN Y TO 0
                // CREATE A QUATERNION VALUE TO GIVE THE PLAYER A SMOOTH ROTATION
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
