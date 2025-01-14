using Netick;
using Netick.Unity;
using UnityEngine;

public struct PlayerCharacterInputs : INetworkInput
{
	public Vector3 moveAxes;
	public bool leftMouseClickDown;
	public bool leftMouse;
	public bool leftMouseClickUp;

	public bool rightMouseClickDown;

	public Vector3 mouseWorldPosition;
	public Vector3 mouseScreenPos;
	public bool interact;
	public bool weapon1;
	public bool weapon2;
	public bool weapon3;
	public bool weapon4;
	public bool dropWeapon;
	public bool tilde;
	public bool Jump;
	public Quaternion cameraRotation;

	public Vector3 CameraPos;
	public Vector3 ScreenPointToRayDirection;
	public override string ToString()
	{
		return $"PlayerCharacterInputs: " +
			   $"\nMove Axes: {moveAxes}" +
			   $"\nLeft Mouse Click Down: {leftMouseClickDown}" +
			   $"\nLeft Mouse: {leftMouse}" +
			   $"\nLeft Mouse Click Up: {leftMouseClickUp}" +
			   $"\nMouse World Position: {mouseWorldPosition}" +
			   $"\nmouseScreenPos: {mouseScreenPos}" +
			   $"\nInteract: {interact}" +
			   $"\nWeapon1: {weapon1}" +
			   $"\nWeapon2: {weapon2}" +
			   $"\nWeapon3: {weapon3}" +
			   $"\nWeapon4: {weapon4}" +
			   $"\nDrop Weapon: {dropWeapon}" +
			   $"\nTilde: {tilde}" +
			   $"\nJump: {Jump}" +
			   $"\nCamera Rotation: {cameraRotation}";
	}

}

[ExecutionOrder(-9000)]
public class InputGetter : NetworkBehaviour
{
	PlayerCharacterInputs defaultInputs = new PlayerCharacterInputs();
	PlayerCharacterInputs emptyInputs = new PlayerCharacterInputs();

	int count = 0;

	[Networked]
	public PlayerCharacterInputs Input
	{
		get; set;
	}

	public void Awake()
	{
	}

	public void OnEnable()
	{

	}

	public void OnDisable()
	{

	}

	public override void NetworkFixedUpdate()
	{
		var curInput = emptyInputs;
		if (FetchInput(out PlayerCharacterInputs myInput))
		{
			count = 10;
			curInput = myInput;
			defaultInputs = myInput;
		}
		else if (IsServer)
		{
			count--;
			if (count < 0)
			{
				curInput = emptyInputs;
				defaultInputs = emptyInputs;
			}
			else
			{
				curInput = defaultInputs;
			}
		}

		if (!enabled)
		{
			curInput = emptyInputs;
		}

		Input = curInput;
	}
}