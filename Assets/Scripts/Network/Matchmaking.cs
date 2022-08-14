using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using ParrelSync;
#endif

namespace NetcodeTesting {
    public class Matchmaking : MonoBehaviour {
        [SerializeField]
        private GameObject startGameButton;

        private Lobby connectedLobby;
        private QueryResponse lobbies;
        private UnityTransport transport;
        private const string joinCodeKey = "j";
        private string playerID;

        private void Awake() => transport = FindObjectOfType<UnityTransport>();

        public async void CreateOrJoinLobby() {
            await Authenticate();

            connectedLobby = await QuickJoinLobby() ?? await CreateLobby();

            if (connectedLobby != null) {
                startGameButton.SetActive(false);

                NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            }
        }

        private async Task Authenticate() {
            InitializationOptions options = new InitializationOptions();

#if UNITY_EDITOR
            options.SetProfile(ClonesManager.IsClone() ? ClonesManager.GetArgument() : "Primary");
#endif
            await UnityServices.InitializeAsync(options);

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            playerID = AuthenticationService.Instance.PlayerId;
        }

        private async Task<Lobby> QuickJoinLobby() {
            try {
                Debug.LogError(Lobbies.Instance.QuickJoinLobbyAsync());
                Lobby lobby = await Lobbies.Instance.QuickJoinLobbyAsync();

                var a = await RelayService.Instance.JoinAllocationAsync(lobby.Data[joinCodeKey].Value);

                SetTransformAsClient(a);

                NetworkManager.Singleton.StartClient();
                return lobby;
            }
            catch (Exception e) {
                Debug.Log("No lobbies available");
                return null;
            }
        }

        private async Task<Lobby> CreateLobby() {
            try {
                const int maxPlayers = 10;

                var a = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
                var joinCode = await RelayService.Instance.GetJoinCodeAsync(a.AllocationId);

                var options = new CreateLobbyOptions {
                    Data = new Dictionary<string, DataObject> { { joinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, joinCode) } }
                };

                var lobby = await Lobbies.Instance.CreateLobbyAsync("Lobby Name", maxPlayers, options);

                StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));

                transport.SetHostRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData);

                NetworkManager.Singleton.StartHost();
                return lobby;
            }
            catch (Exception e) {
                Debug.Log("Failed creating a lobby");
                return null;
            }
        }

        private void SetTransformAsClient(JoinAllocation a) {
            transport.SetClientRelayData(a.RelayServer.IpV4, (ushort)a.RelayServer.Port, a.AllocationIdBytes, a.Key, a.ConnectionData, a.HostConnectionData);
        }

        private static IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds) {
            var delay = new WaitForSecondsRealtime(waitTimeSeconds);
            while (true) {
                Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
                yield return delay;
            }
        }

        private void OnDestroy() {
            try {
                StopAllCoroutines();
                if(connectedLobby != null) {
                    if (connectedLobby.HostId == playerID) { Lobbies.Instance.DeleteLobbyAsync(connectedLobby.Id); }
                    else { Lobbies.Instance.RemovePlayerAsync(connectedLobby.Id, playerID); }
                }
            }
            catch (Exception e) {
                Debug.Log($"Error shuting dow lobby: {e}");
            }
        }

    }
}
