using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace NetcodeTesting {
    public class PlayerVisual : NetworkBehaviour {
        private readonly NetworkVariable<Color> netColor = new();
        private readonly Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow, Color.black, Color.white, Color.magenta, Color.gray };
        private int index;

        [SerializeField]
        private MeshRenderer meshRenderer;
        private void Awake() {
            netColor.OnValueChanged += OnValueChanged;
        }

        public override void OnDestroy() => netColor.OnValueChanged -= OnValueChanged;
        private void OnValueChanged(Color prev, Color next) => meshRenderer.material.color = next;

        public override void OnNetworkSpawn() {
            if (IsOwner) {
                index = (int)OwnerClientId;
                CommitNetworkColorServerRpc(GetNextColor());
            }
            else {
                meshRenderer.material.color = netColor.Value;
            }
        }

        [ServerRpc]
        private void CommitNetworkColorServerRpc(Color color) => netColor.Value = color;

        private void OnTriggerEnter(Collider other) {
            if (!IsOwner) return;
            CommitNetworkColorServerRpc(GetNextColor());
        }

        private Color GetNextColor() => colors[index++ % colors.Length];
    }
}
