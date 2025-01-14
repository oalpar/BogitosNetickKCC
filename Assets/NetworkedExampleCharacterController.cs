/// <summary>
/// This is called every frame by ExamplePlayer in order to tell the character what its inputs are
/// </summary>

using KinematicCharacterController;
using Netick;
using Netick.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BogitosKCC
{
	public enum OrientationMethod
	{
		TowardsCamera,
		TowardsMovement,
	}

	public enum BonusOrientationMethod
	{
		None,
		TowardsGravity,
		TowardsGroundSlopeAndGravity,
	}

	public class NetworkedExampleCharacterController : NetworkBehaviour, ICharacterController
	{
		[Networked]
		public PlayerCharacterInputs input
		{
			get; set;
		}
		NetworkedKinematicCharacterMotor Motor;

		public bool canMoveInTheAir = false;
		[Header("Stable Movement")] public float MaxStableMoveSpeed = 10f;
		public float StableMovementSharpness = 15f;
		public float OrientationSharpness = 10f;
		public OrientationMethod OrientationMethod = OrientationMethod.TowardsCamera;

		[Header("Air Movement")] public float MaxAirMoveSpeed = 15f;
		public float AirAccelerationSpeed = 15f;
		public float Drag = 0.1f;

		[Header("Jumping")] public bool AllowJumpingWhenSliding = false;
		public float JumpUpSpeed = 10f;
		public float JumpScalableForwardSpeed = 10f;
		public float JumpPreGroundingGraceTime = 0f;
		public float JumpPostGroundingGraceTime = 0f;

		[Header("Misc")] public List<Collider> IgnoredColliders = new List<Collider>();
		public BonusOrientationMethod BonusOrientationMethod = BonusOrientationMethod.None;
		public float BonusOrientationSharpness = 10f;
		[Range(0.0f, 1f)]
		public float BonusOrientationSharpnessDecayRate = 0.02f; //lower means less decay as we go further from the gravity source (its exponential)
		[Networked]
		public Vector3 Gravity
		{
			get; set;
		}
		public Transform MeshRoot;
		public Transform CameraFollowPoint;
		public float CrouchedCapsuleHeight = 1f;

		private Collider[] _probedColliders = new Collider[8];
		private RaycastHit[] _probedHits = new RaycastHit[8];
		[Networked]
		private Vector3 _moveInputVector
		{
			get; set;
		}
		[Networked]
		private Vector3 _lookInputVector
		{
			get; set;
		}
		[Networked]
		private bool _jumpRequested
		{
			get; set;
		}

		[Networked]
		private bool _jumpConsumed
		{
			get; set;
		}
		[Networked]
		private bool _jumpedThisFrame
		{
			get; set;
		}
		[Networked]
		private NetworkTimer jumpRequestedTimer
		{
			get; set;
		}
		[Networked]
		private NetworkTimer ableToJumpPostGroundingTimer
		{
			get; set;
		}
		[Networked]
		private Vector3 _internalVelocityAdd
		{
			get; set;
		}
		[Networked]
		private bool _shouldBeCrouching
		{
			get; set;
		}
		[Networked]
		private bool _isCrouching
		{
			get; set;
		}

		[Networked]
		private Vector3 lastInnerNormal
		{
			get; set;
		}
		[Networked]
		private Vector3 lastOuterNormal
		{
			get; set;
		}
		public override void NetworkFixedUpdate()
		{
			var inputGetter = GetComponent<InputGetter>();
			SetInputs(inputGetter.Input);
		}

		public override void NetworkStart()
		{
			Motor = GetComponent<NetworkedKinematicCharacterMotor>();
			Motor.CharacterController = this;
		}

		/// <summary>
		/// (Called by KinematicCharacterMotor during its update cycle)
		/// This is where you tell your character what its rotation should be right now. 
		/// This is the ONLY place where you should set the character's rotation
		/// </summary>
		public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
		{
			if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f)
			{
				// Smoothly interpolate from current to target look direction
				Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

				// Set the current rotation (which will be used by the KinematicCharacterMotor)
				currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
			}

			Vector3 currentUp = (currentRotation * Vector3.up);

			if (BonusOrientationMethod == BonusOrientationMethod.TowardsGravity)
			{
				float amplifier = MathF.Exp(-BonusOrientationSharpnessDecayRate);
				// Rotate from current up to invert gravity
				Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized,
					1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime * amplifier));
				currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
				//currentRotation = Quaternion.FromToRotation(currentUp, -Gravity.normalized) * currentRotation;
			}
			else if (BonusOrientationMethod == BonusOrientationMethod.TowardsGroundSlopeAndGravity)
			{
				if (Motor.GroundingStatus.IsStableOnGround)
				{
					Vector3 initialCharacterBottomHemiCenter =
						Motor.TransientPosition + (currentUp * Motor.Capsule.radius);

					Vector3 smoothedGroundNormal = Vector3.Slerp(Motor.CharacterUp,
						Motor.GroundingStatus.GroundNormal,
						1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
					currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) *
									  currentRotation;

					// Move the position to create a rotation around the bottom hemi center instead of around the pivot
					Motor.SetTransientPosition(initialCharacterBottomHemiCenter +
											   (currentRotation * Vector3.down * Motor.Capsule.radius));
				}
				else
				{
					Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized,
						1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
					currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) *
									  currentRotation;
				}
			}
			else
			{
				Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up,
					1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
				currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
			}

		}

		/// <summary>
		/// (Called by KinematicCharacterMotor during its update cycle)
		/// This is where you tell your character what its velocity should be right now. 
		/// This is the ONLY place where you can set the character's velocity
		/// </summary>
		public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			// Ground movement
			if (Motor.GroundingStatus.IsStableOnGround)
			{
				float currentVelocityMagnitude = currentVelocity.magnitude;

				Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

				// Reorient velocity on slope
				currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) *
								  currentVelocityMagnitude;

				// Calculate target velocity
				Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
				Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized *
										  _moveInputVector.magnitude;
				Vector3 targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

				// Smooth movement Velocity
				currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity,
					1f - Mathf.Exp(-StableMovementSharpness * deltaTime));
			}
			// Air movement
			else
			{
				// Add move input
				if (_moveInputVector.sqrMagnitude > 0f && canMoveInTheAir)
				{
					Vector3 addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime;

					Vector3 currentVelocityOnInputsPlane =
						Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

					// Limit air velocity from inputs
					if (currentVelocityOnInputsPlane.magnitude < MaxAirMoveSpeed)
					{
						// clamp addedVel to make total vel not exceed max vel on inputs plane
						Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity,
							MaxAirMoveSpeed);
						addedVelocity = newTotal - currentVelocityOnInputsPlane;
					}
					else
					{
						// Make sure added vel doesn't go in the direction of the already-exceeding velocity
						if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
						{
							addedVelocity = Vector3.ProjectOnPlane(addedVelocity,
								currentVelocityOnInputsPlane.normalized);
						}
					}

					// Prevent air-climbing sloped walls
					if (Motor.GroundingStatus.FoundAnyGround)
					{
						if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
						{
							Vector3 perpenticularObstructionNormal = Vector3
								.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal),
									Motor.CharacterUp).normalized;
							addedVelocity =
								Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
						}
					}

					// Apply added velocity
					currentVelocity += addedVelocity;
				}

				// Gravity
				currentVelocity += Gravity * deltaTime;

				// Drag
				currentVelocity *= (1f / (1f + (Drag * deltaTime)));
			}

			// Handle jumping
			_jumpedThisFrame = false;
			if (_jumpRequested)
			{
				jumpRequestedTimer = NetworkTimer.StartTimer(JumpPreGroundingGraceTime, Sandbox.Engine, true);
				// See if we actually are allowed to jump
				if (!_jumpConsumed &&
					((AllowJumpingWhenSliding
						 ? Motor.GroundingStatus.FoundAnyGround
						 : Motor.GroundingStatus.IsStableOnGround) ||
					 ableToJumpPostGroundingTimer.TargetTick >= Sandbox.Tick))
				{
					// Calculate jump direction before ungrounding
					Vector3 jumpDirection = Motor.CharacterUp;
					if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
					{
						jumpDirection = Motor.GroundingStatus.GroundNormal;
					}

					// Makes the character skip ground probing/snapping on its next update. 
					// If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
					Motor.ForceUnground();

					// Add to the return velocity and reset jump state
					currentVelocity += (jumpDirection * JumpUpSpeed) -
									   Vector3.Project(currentVelocity, Motor.CharacterUp);
					currentVelocity += (_moveInputVector * JumpScalableForwardSpeed);
					_jumpRequested = false;
					_jumpConsumed = true;
					_jumpedThisFrame = true;
				}
			}

			// Take into account additive velocity
			if (_internalVelocityAdd.sqrMagnitude > 0f)
			{
				currentVelocity += _internalVelocityAdd;
				_internalVelocityAdd = Vector3.zero;
			}

		}

		/// <summary>
		/// (Called by KinematicCharacterMotor during its update cycle)
		/// This is called after the character has finished its movement update
		/// </summary>
		public void AfterCharacterUpdate(float deltaTime)
		{
			// Handle jump-related values
			{
				// Handle jumping pre-ground grace period
				if (_jumpRequested && jumpRequestedTimer.TargetTick < Sandbox.Tick)
				{
					_jumpRequested = false;
				}

				if (AllowJumpingWhenSliding
						? Motor.GroundingStatus.FoundAnyGround
						: Motor.GroundingStatus.IsStableOnGround)
				{
					// If we're on a ground surface, reset jumping values
					if (!_jumpedThisFrame)
					{
						_jumpConsumed = false;
					}

					ableToJumpPostGroundingTimer = NetworkTimer.StartTimer(JumpPreGroundingGraceTime, Sandbox.Engine, true);
				}
			}

			// Handle uncrouching
			if (_isCrouching && !_shouldBeCrouching)
			{
				// Do an overlap test with the character's standing height to see if there are any obstructions
				Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
				if (Motor.CharacterOverlap(
						Motor.TransientPosition,
						Motor.TransientRotation,
						_probedColliders,
						Motor.CollidableLayers,
						QueryTriggerInteraction.Ignore) > 0)
				{
					// If obstructions, just stick to crouching dimensions
					Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
				}
				else
				{
					// If no obstructions, uncrouch
					MeshRoot.localScale = new Vector3(1f, 1f, 1f);
					_isCrouching = false;
				}
			}
		}

		public void PostGroundingUpdate(float deltaTime)
		{
			// Handle landing and leaving ground
			if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
			{
				OnLanded();
			}
			else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
			{
				OnLeaveStableGround();
			}
		}

		public bool IsColliderValidForCollisions(Collider coll)
		{
			if (IgnoredColliders.Count == 0)
			{
				return true;
			}

			if (IgnoredColliders.Contains(coll))
			{
				return false;
			}

			return true;
		}

		public void AddVelocity(Vector3 velocity, bool cancelAttachedRBVelocity = false)
		{
			Motor.ForceUnground(0.3f);
			_internalVelocityAdd += velocity;
			if (cancelAttachedRBVelocity && Motor.AttachedRigidbodyVelocity.magnitude > 0)
			{

				_internalVelocityAdd -= Motor.AttachedRigidbodyVelocity;
			}
		}
		protected void OnLanded()
		{
		}

		protected void OnLeaveStableGround()
		{
		}



		public void SetInputs(PlayerCharacterInputs inputs)
		{
			input = inputs;
			// Clamp input
			Vector3 moveInputVector =
				Vector3.ClampMagnitude(inputs.moveAxes, 1f);

			// Calculate camera direction and rotation on the character plane
			Vector3 cameraPlanarDirection =
				Vector3.ProjectOnPlane(inputs.cameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
			if (cameraPlanarDirection.sqrMagnitude == 0f)
			{
				cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.cameraRotation * Vector3.up, Motor.CharacterUp)
					.normalized;
			}

			_jumpRequested = input.Jump;

			Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);
			_moveInputVector = cameraPlanarRotation * moveInputVector;

			switch (OrientationMethod)
			{
				case OrientationMethod.TowardsCamera:
					_lookInputVector = cameraPlanarDirection;
					break;
				case OrientationMethod.TowardsMovement:
					_lookInputVector = _moveInputVector.normalized;
					break;
			}
		}

		public void BeforeCharacterUpdate(float deltaTime)
		{
		}

		public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
		{
		}

		public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
		{
		}

		public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
		{
		}

		public void OnDiscreteCollisionDetected(Collider hitCollider)
		{
		}
	}
}