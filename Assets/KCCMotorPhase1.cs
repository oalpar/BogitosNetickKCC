using Netick;
using Netick.Unity;

namespace BogitosKCC
{
	[ExecutionOrder(-3)]
	public class KCCMotorPhase1 : NetworkBehaviour
	{
		NetworkedKinematicCharacterMotor Motor;
		public override void NetworkStart()
		{
			Motor = GetComponent<NetworkedKinematicCharacterMotor>();
		}

		public override void NetworkFixedUpdate()
		{
			Motor.UpdatePhase1(Sandbox.FixedDeltaTime);
		}
	}
}