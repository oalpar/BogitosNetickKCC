using Netick;
using Netick.Unity;
using UnityEngine;

[ExecutionOrder(-9999)]
public class InputSetter : NetworkBehaviour
{
	private const string HorizontalInput = "Horizontal";
	private const string VerticalInput = "Vertical";

	public Camera playerCam = null;
	public override void NetworkStart()
	{
		Sandbox.Events.OnSceneOperationDone += OnSceneOperationDone;
		FindCamera();
	}

	private void FindCamera()
	{
		var sceneCameras = Sandbox.FindObjectsOfType<Camera>();

		Camera mainCamera = null;
		foreach (Camera cam in sceneCameras)
		{
			if (cam.gameObject.tag == "MainCamera")
			{
				mainCamera = cam;

			}
		}

		playerCam = mainCamera;
	}

	public void OnSceneOperationDone(NetworkSandbox sandbox, NetworkSceneOperation sceneOperation)
	{

		if (!sceneOperation.IsDone)
		{
			return;
		}

		if (Sandbox != sandbox)
			return;

		FindCamera();
	}

	public override void NetworkUpdate()
	{
		if (Sandbox.LocalPlayer == null)
		{
			return;
		}

		var input = Sandbox.GetInput<PlayerCharacterInputs>();
		input.moveAxes = new Vector3(Input.GetAxisRaw(HorizontalInput), 0, Input.GetAxisRaw(VerticalInput));
		input.leftMouseClickDown |= Input.GetMouseButtonDown(0);
		input.leftMouse |= Input.GetMouseButton(0);
		input.leftMouseClickUp |= Input.GetMouseButtonUp(0);
		input.interact |= Input.GetKeyDown(KeyCode.E);
		input.weapon1 |= Input.GetKeyDown(KeyCode.Alpha1);
		input.weapon2 |= Input.GetKeyDown(KeyCode.Alpha2);
		input.weapon3 |= Input.GetKeyDown(KeyCode.Alpha3);
		input.weapon4 |= Input.GetKeyDown(KeyCode.Alpha4);
		input.dropWeapon |= Input.GetKeyDown(KeyCode.F);
		input.tilde |= Input.GetKeyDown(KeyCode.Tilde);
		input.Jump |= Input.GetKeyDown(KeyCode.Space);
		input.rightMouseClickDown |= Input.GetMouseButtonDown(1);

		if (playerCam != null)
		{
			Vector3 mouseScreenPosition = Input.mousePosition;
			input.mouseWorldPosition = playerCam.ScreenToWorldPoint(mouseScreenPosition);
			input.mouseWorldPosition.z = 0;
			input.cameraRotation = playerCam.transform.rotation;
			input.mouseScreenPos = mouseScreenPosition;
			Ray ray = playerCam.ScreenPointToRay(mouseScreenPosition);
			input.CameraPos = playerCam.transform.position;
			input.ScreenPointToRayDirection = ray.direction;
		}

		Sandbox.SetInput<PlayerCharacterInputs>(input);
	}
}
