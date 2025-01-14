using Netick;
using Netick.Unity;

namespace BogitosKCC
{
	[ExecutionOrder(-4)]
	public class KCCMoverPhase1 : NetworkBehaviour
	{
		NetworkedPhysicsMover Mover;
		public override void NetworkStart()
		{
			Mover = GetComponent<NetworkedPhysicsMover>();
		}

		public override void NetworkFixedUpdate()
		{
			Mover.VelocityUpdate(Sandbox.FixedDeltaTime);
		}
	}
}