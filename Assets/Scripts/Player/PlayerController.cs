using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace NetcodeTesting {
    public class PlayerController : NetworkBehaviour {

        [SerializeField]
        protected float velocity;
        [SerializeField]
        protected float maxVelocity;

        Rigidbody rigidBody;

        Vector3 input;

        public override void OnNetworkSpawn() {
            if (!IsOwner) Destroy(this);

            rigidBody = GetComponent<Rigidbody>();
        }

        private void Update() {
            input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        }

        private void FixedUpdate() {
            HandleMovement();
        }

        void HandleMovement() {
            rigidBody.velocity += input.normalized * (velocity * Time.deltaTime);
            rigidBody.velocity = Vector3.ClampMagnitude(rigidBody.velocity, maxVelocity);
        }
    }
}
