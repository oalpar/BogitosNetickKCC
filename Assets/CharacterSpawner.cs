using Netick;
using Netick.Unity;
using UnityEngine;

public class CharacterSpawner : NetworkBehaviour
{
	public GameObject PlayerPrefab;
	public override void NetworkAwake()
	{
		Sandbox.Events.OnPlayerConnected += SpawnPlayerCharacter;
	}

	private void SpawnPlayerCharacter(NetworkSandbox sandbox, NetworkPlayer player)
	{
		if (!IsServer)
			return;
		Sandbox.NetworkInstantiate(PlayerPrefab, Vector3.zero, Quaternion.identity, player);
	}
}
