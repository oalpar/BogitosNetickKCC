using Netick;
using Netick.Unity;
using SixAmGames;
using UnityEngine;

public class CameraAttacher : NetworkBehaviour
{
	public Transform FollowTransform;
	public override void NetworkStart()
	{
		SetCamera();
	}
	public override void OnInputSourceChanged(NetworkPlayer previous)
	{
		SetCamera();
	}

	public void SetCamera()
	{
		if (Sandbox.LocalPlayer == InputSource)
		{
			Sandbox.Log("HELLO STREAM");
			Sandbox.FindObjectOfType<ExampleCharacterCamera>().SetFollowTransform(FollowTransform);
		}
	}
}
