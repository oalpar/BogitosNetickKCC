using Netick;
using Netick.Unity;
using System;
using UnityEngine;

namespace BogitosKCC
{
	[Networked]
	public struct PhysicsMoverState
	{
		public Vector3 Position;
		public Quaternion Rotation;
		public Vector3 Velocity;
		public Vector3 AngularVelocity;
	}

	/// <summary>
	/// Component that manages the movement of moving kinematic rigidbodies for
	/// proper interaction with characters
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(KCCMoverPhase1))]
	[RequireComponent(typeof(KCCMoverPhase2))]

	public class NetworkedPhysicsMover : NetworkBehaviour
	{

		public Collider[] Colliders;



		[Networked]
		public PhysicsMoverState NetworkedState
		{
			get; set;
		}

		[Networked(precision: 0.000001f)]
		[Smooth]
		Vector3 smoothPosition
		{
			get; set;
		}

		[Networked(precision: 0.000001f)]
		[Smooth]
		Quaternion smoothRotation
		{
			get; set;
		} = Quaternion.identity;

		public Vector3 Velocity
		{
			get; set;
		}

		public Vector3 AngularVelocity
		{
			get; set;
		}

		public Transform Visuals;
		/// <summary>
		/// The mover's Rigidbody
		/// </summary>
		public Rigidbody Rigidbody;
		/// <summary>
		/// Index of this motor in KinematicCharacterSystem arrays
		/// </summary>
		[NonSerialized] public KinematicCharacterController.IMoverController MoverController;

		/// <summary>
		/// Remembers latest position in interpolation
		/// </summary>
		[NonSerialized] public Vector3 LatestInterpolationPosition;

		/// <summary>
		/// Remembers latest rotation in interpolation
		/// </summary>
		[NonSerialized] public Quaternion LatestInterpolationRotation;

		/// <summary>
		/// The latest movement made by interpolation
		/// </summary>
		[NonSerialized] public Vector3 PositionDeltaFromInterpolation;

		/// <summary>
		/// The latest rotation made by interpolation
		/// </summary>
		[NonSerialized] public Quaternion RotationDeltaFromInterpolation;

		/// <summary>
		/// Index of this motor in KinematicCharacterSystem arrays
		/// </summary>
		public int IndexInCharacterSystem
		{
			get; set;
		}


		/// <summary>
		/// Remembers initial position before all simulation are done
		/// </summary>
		public Vector3 InitialTickPosition
		{
			get; set;
		}

		/// <summary>
		/// Remembers initial rotation before all simulation are done
		/// </summary>
		public Quaternion InitialTickRotation
		{
			get; set;
		}

		/// <summary>
		/// The mover's Transform
		/// </summary>
		public Transform Transform
		{
			get; private set;
		}

		/// <summary>
		/// The character's position before the movement calculations began
		/// </summary>
		public Vector3 InitialSimulationPosition
		{
			get; private set;
		}

		/// <summary>
		/// The character's rotation before the movement calculations began
		/// </summary>
		public Quaternion InitialSimulationRotation
		{
			get; private set;
		}
		private Vector3 _internalTransientPosition;

		/// <summary>
		/// The mover's rotation (always up-to-date during the character update phase)
		/// </summary>
		public Vector3 TransientPosition
		{
			get
			{
				return _internalTransientPosition;
			}
			private set
			{
				_internalTransientPosition = value;
			}
		}
		private Quaternion _internalTransientRotation;
		/// <summary>
		/// The mover's rotation (always up-to-date during the character update phase)
		/// </summary>
		public Quaternion TransientRotation
		{
			get
			{
				return _internalTransientRotation;
			}
			private set
			{
				_internalTransientRotation = value;
			}
		}

		private void Reset()
		{
			ValidateData();
		}

		private void OnValidate()
		{
			ValidateData();
		}

		/// <summary>
		/// Handle validating all required values
		/// </summary>
		public void ValidateData()
		{
			Rigidbody = gameObject.GetComponent<Rigidbody>();

			Rigidbody.centerOfMass = Vector3.zero;
			Rigidbody.maxAngularVelocity = Mathf.Infinity;
			Rigidbody.maxDepenetrationVelocity = Mathf.Infinity;
			Rigidbody.isKinematic = true;
			Rigidbody.interpolation = RigidbodyInterpolation.None;
		}

		private void Awake()
		{
			Transform = this.transform;
			Colliders = GetComponentsInChildren<Collider>();
			ValidateData();

			TransientPosition = Rigidbody.position;
			TransientRotation = Rigidbody.rotation;
			InitialSimulationPosition = Rigidbody.position;
			InitialSimulationRotation = Rigidbody.rotation;
			LatestInterpolationPosition = Transform.position;
			LatestInterpolationRotation = Transform.rotation;
		}

		public override void NetworkAwake()
		{
			_internalTransientRotation = Transform.rotation;
		}

		/// <summary>
		/// Sets the mover's position directly
		/// </summary>
		public void SetPosition(Vector3 position)
		{
			Transform.position = position;
			Rigidbody.position = position;
			InitialSimulationPosition = position;
			TransientPosition = position;
		}

		/// <summary>
		/// Sets the mover's rotation directly
		/// </summary>
		public void SetRotation(Quaternion rotation)
		{
			Transform.rotation = rotation;
			Rigidbody.rotation = rotation;
			InitialSimulationRotation = rotation;
			TransientRotation = rotation;
		}

		/// <summary>
		/// Sets the mover's position and rotation directly
		/// </summary>
		public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
		{
			Transform.SetPositionAndRotation(position, rotation);
			Rigidbody.position = position;
			Rigidbody.rotation = rotation;
			InitialSimulationPosition = position;
			InitialSimulationRotation = rotation;
			TransientPosition = position;
			TransientRotation = rotation;
		}

		/// <summary>
		/// Returns all the state information of the mover that is pertinent for simulation
		/// </summary>
		public PhysicsMoverState GetState()
		{
			PhysicsMoverState state = new PhysicsMoverState();

			state.Position = TransientPosition;
			state.Rotation = TransientRotation;
			state.Velocity = Velocity;
			state.AngularVelocity = AngularVelocity;

			return state;
		}

		/// <summary>
		/// Applies a mover state instantly
		/// </summary>
		public void ApplyState(PhysicsMoverState state)
		{
			//state.Rotation = state.Rotation.normalized;
			SetPositionAndRotation(state.Position, state.Rotation);
			Velocity = state.Velocity;
			AngularVelocity = state.AngularVelocity;
		}
		public override void NetcodeIntoGameEngine()
		{
			ApplyState(NetworkedState);
			smoothPosition = NetworkedState.Position;
			smoothRotation = NetworkedState.Rotation;
		}

		public override void GameEngineIntoNetcode()
		{
			NetworkedState = GetState();
			smoothPosition = NetworkedState.Position;
			smoothRotation = NetworkedState.Rotation;
		}

		/// <summary>
		/// Caches velocity values based on deltatime and target position/rotations
		/// </summary>
		public void VelocityUpdate(float deltaTime)
		{
			InitialSimulationPosition = TransientPosition;
			InitialSimulationRotation = TransientRotation;
			MoverController.UpdateMovement(out _internalTransientPosition, out _internalTransientRotation, deltaTime);

			if (deltaTime > 0f)
			{
				Velocity = (TransientPosition - InitialSimulationPosition) / deltaTime;
				Quaternion rotationFromCurrentToGoal =
					TransientRotation * (Quaternion.Inverse(InitialSimulationRotation));
				AngularVelocity = (Mathf.Deg2Rad * rotationFromCurrentToGoal.eulerAngles) / deltaTime;
			}
		}

		public override void NetworkRender()
		{
			Visuals.transform.position = smoothPosition;
			Visuals.transform.rotation = smoothRotation;
		}
	}
}