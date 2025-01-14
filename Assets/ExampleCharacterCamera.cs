using Netick;
using Netick.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SixAmGames
{
	[ExecutionOrder(10000)]
	public class ExampleCharacterCamera : NetickBehaviour
	{
		[Header("Framing")]
		public Camera Camera;
		public Vector2 FollowPointFraming = new Vector2(0f, 0f);
		public float FollowingSharpness = 10000f;

		[Header("Distance")]
		public float DefaultDistance = 6f;
		public float MinDistance = 0f;
		public float MaxDistance = 10f;
		public float DistanceMovementSpeed = 5f;
		public float DistanceMovementSharpness = 10f;

		[Header("Rotation")]
		public bool InvertX = false;
		public bool InvertY = false;
		[Range(-90f, 90f)]
		public float DefaultVerticalAngle = 20f;
		[Range(-90f, 90f)]
		public float MinVerticalAngle = -90f;
		[Range(-90f, 90f)]
		public float MaxVerticalAngle = 90f;
		public float RotationSpeed = 1f;
		public float RotationSharpness = 10000f;
		public bool RotateWithPhysicsMover = false;

		[Header("Obstruction")]
		public float ObstructionCheckRadius = 0.2f;
		public LayerMask ObstructionLayers = -1;
		public float ObstructionSharpness = 10000f;
		public List<Collider> IgnoredColliders = new List<Collider>();

		public Transform Transform;
		public Transform FollowTransform;
		public Vector3 PlanarDirection
		{
			get; set;
		}
		public float TargetDistance
		{
			get; set;
		}

		private bool _distanceIsObstructed;
		private float _currentDistance;
		private float _targetVerticalAngle;
		private RaycastHit _obstructionHit;
		private int _obstructionCount;
		private RaycastHit[] _obstructions = new RaycastHit[MaxObstructions];
		private float _obstructionTime;
		private Vector3 _currentFollowPosition;

		private const int MaxObstructions = 32;

		public override void NetworkStart()
		{
			Transform = this.transform;
			DefaultDistance = Mathf.Clamp(DefaultDistance, MinDistance, MaxDistance);
			DefaultVerticalAngle = Mathf.Clamp(DefaultVerticalAngle, MinVerticalAngle, MaxVerticalAngle);
			_currentDistance = DefaultDistance;
			TargetDistance = _currentDistance;

			_targetVerticalAngle = 0f;

			PlanarDirection = Vector3.forward;
		}

		// Set the transform that the camera will orbit around
		public void SetFollowTransform(Transform t)
		{
			FollowTransform = t;
			PlanarDirection = FollowTransform.forward;
			_currentFollowPosition = FollowTransform.position;
		}

		private const string MouseXInput = "Mouse X";
		private const string MouseYInput = "Mouse Y";
		public override void NetworkRender()
		{
			float mouseLookAxisUp = Input.GetAxisRaw(MouseYInput);
			float mouseLookAxisRight = Input.GetAxisRaw(MouseXInput);
			Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);
			UpdateWithInput(Sandbox.DeltaTime, 0, lookInputVector);
		}

		public void UpdateWithInput(float deltaTime, float zoomInput, Vector3 rotationInput)
		{
			if (FollowTransform)
			{
				if (InvertX)
				{
					rotationInput.x *= -1f;
				}
				if (InvertY)
				{
					rotationInput.y *= -1f;
				}

				// Process rotation input
				Quaternion rotationFromInput = Quaternion.Euler(Vector3.up * (rotationInput.x * RotationSpeed));
				PlanarDirection = rotationFromInput * PlanarDirection;
				PlanarDirection = Vector3.Cross(Vector3.up, Vector3.Cross(PlanarDirection, Vector3.up));
				Quaternion planarRot = Quaternion.LookRotation(PlanarDirection, Vector3.up);

				_targetVerticalAngle -= (rotationInput.y * RotationSpeed);
				_targetVerticalAngle = Mathf.Clamp(_targetVerticalAngle, MinVerticalAngle, MaxVerticalAngle);
				Quaternion verticalRot = Quaternion.Euler(_targetVerticalAngle, 0, 0);
				Quaternion targetRotation = Quaternion.Slerp(Transform.rotation, planarRot * verticalRot, 1f - Mathf.Exp(-RotationSharpness * deltaTime));

				// Apply rotation
				Transform.rotation = targetRotation;

				// Process distance input
				if (_distanceIsObstructed && Mathf.Abs(zoomInput) > 0f)
				{
					TargetDistance = _currentDistance;
				}
				TargetDistance += zoomInput * DistanceMovementSpeed;
				TargetDistance = Mathf.Clamp(TargetDistance, MinDistance, MaxDistance);

				// Find the smoothed follow position
				_currentFollowPosition = Vector3.Lerp(_currentFollowPosition, FollowTransform.position, 1f - Mathf.Exp(-FollowingSharpness * deltaTime));

				// Handle obstructions
				{
					RaycastHit closestHit = new RaycastHit();
					closestHit.distance = Mathf.Infinity;
					_obstructionCount = Sandbox.Physics.SphereCast(_currentFollowPosition, ObstructionCheckRadius, -Transform.forward, _obstructions, TargetDistance, ObstructionLayers, QueryTriggerInteraction.Ignore);
					for (int i = 0; i < _obstructionCount; i++)
					{
						bool isIgnored = false;
						for (int j = 0; j < IgnoredColliders.Count; j++)
						{
							if (IgnoredColliders[j] == _obstructions[i].collider)
							{
								isIgnored = true;
								break;
							}
						}
						for (int j = 0; j < IgnoredColliders.Count; j++)
						{
							if (IgnoredColliders[j] == _obstructions[i].collider)
							{
								isIgnored = true;
								break;
							}
						}

						if (!isIgnored && _obstructions[i].distance < closestHit.distance && _obstructions[i].distance > 0)
						{
							closestHit = _obstructions[i];
						}
					}

					// If obstructions detecter
					if (closestHit.distance < Mathf.Infinity)
					{
						_distanceIsObstructed = true;
						_currentDistance = Mathf.Lerp(_currentDistance, closestHit.distance, 1 - Mathf.Exp(-ObstructionSharpness * deltaTime));
					}
					// If no obstruction
					else
					{
						_distanceIsObstructed = false;
						_currentDistance = Mathf.Lerp(_currentDistance, TargetDistance, 1 - Mathf.Exp(-DistanceMovementSharpness * deltaTime));
					}
				}

				// Find the smoothed camera orbit position
				Vector3 targetPosition = _currentFollowPosition - ((targetRotation * Vector3.forward) * _currentDistance);

				// Handle framing
				targetPosition += Transform.right * FollowPointFraming.x;
				targetPosition += Transform.up * FollowPointFraming.y;

				// Apply position
				Transform.position = targetPosition;
			}
		}

		public void Register(Transform t)
		{
			SetFollowTransform(t);
		}
	}
}