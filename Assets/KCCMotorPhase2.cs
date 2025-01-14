using Netick;
using Netick.Unity;

namespace BogitosKCC
{
	[ExecutionOrder(-1)]
	public class KCCMotorPhase2 : NetworkBehaviour
	{
		NetworkedKinematicCharacterMotor Motor;
		public override void NetworkStart()
		{
			Motor = GetComponent<NetworkedKinematicCharacterMotor>();
		}

		public override void NetworkFixedUpdate()
		{
			Motor.UpdatePhase2(Sandbox.FixedDeltaTime);
			Motor.Transform.SetPositionAndRotation(Motor.TransientPosition, Motor.TransientRotation);
		}
	}
}