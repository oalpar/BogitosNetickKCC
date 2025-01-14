using Netick;
using Netick.Unity;

namespace BogitosKCC
{
	[ExecutionOrder(-2)]
	public class KCCMoverPhase2 : NetworkBehaviour
	{
		NetworkedPhysicsMover Mover;
		public override void NetworkStart()
		{
			Mover = GetComponent<NetworkedPhysicsMover>();
		}

		public override void NetworkFixedUpdate()
		{
			Mover.Transform.SetPositionAndRotation(Mover.TransientPosition, Mover.TransientRotation);
			Mover.Rigidbody.position = Mover.TransientPosition;
			Mover.Rigidbody.rotation = Mover.TransientRotation;
		}
	}
}