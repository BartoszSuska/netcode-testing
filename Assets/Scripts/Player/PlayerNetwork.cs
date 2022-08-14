using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace NetcodeTesting {
    public class PlayerNetwork : NetworkBehaviour {
        private NetworkVariable<PlayerNetworkData> netState = new NetworkVariable<PlayerNetworkData>(writePerm: NetworkVariableWritePermission.Owner);

        [SerializeField]
        private float cheapInterpolationTime;
        private Rigidbody rigidBody;
        [SerializeField]
        private bool serverAuth;

        private void Awake() {
            rigidBody = GetComponent<Rigidbody>();

            var permission = serverAuth ? NetworkVariableWritePermission.Server : NetworkVariableWritePermission.Owner;
            netState = new NetworkVariable<PlayerNetworkData>(writePerm: permission);
        }

        public override void OnNetworkSpawn() {
            if (!IsOwner) Destroy(transform.GetComponent<PlayerController>());
        }

        private void Update() {
            if (IsOwner) {
                TransmitState();
            }
            else {
                ConsumeState();
            }
        }

        private void TransmitState() {
            PlayerNetworkData state = new PlayerNetworkData {
                Position = rigidBody.position,
                Rotation = transform.rotation.eulerAngles
            };

            if (IsServer || !serverAuth)
                netState.Value = state;
            else
                TransmitStateServerRpc(state);
        }

        [ServerRpc]
        private void TransmitStateServerRpc(PlayerNetworkData state) {
            netState.Value = state;
        }

        Vector3 posVel;
        float rotVelY;

        void ConsumeState() {
            rigidBody.MovePosition(Vector3.SmoothDamp(rigidBody.position, netState.Value.Position, ref posVel, cheapInterpolationTime));

            transform.rotation = Quaternion.Euler(
                0, Mathf.SmoothDampAngle(transform.rotation.eulerAngles.y, netState.Value.Rotation.y, ref rotVelY, cheapInterpolationTime), 0f);
        }

        struct PlayerNetworkData : INetworkSerializable {
            private float x, y, z;
            private float yRot;

            internal Vector3 Position {
                get => new Vector3(x, y, z);
                set {
                    x = value.x;
                    y = value.y;
                    z = value.z;
                }
            }

            internal Vector3 Rotation {
                get => new Vector3(0, yRot, 0);
                set => yRot = value.y;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
                serializer.SerializeValue(ref x);
                serializer.SerializeValue(ref y);
                serializer.SerializeValue(ref z);

                serializer.SerializeValue(ref yRot);
            }
        }
    }
}
