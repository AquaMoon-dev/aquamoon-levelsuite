using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[AddComponentMenu("AquaMoon/Character Physics Pusher")]
public class CharacterPhysicsPusher : MonoBehaviour
{
    [Header("Impulse Parameters")]
    [Tooltip("Base force applied to pushed rigidbodies.")]
    public float pushPower = 2.5f;

    [Tooltip("Bodies above this mass will decelerate the player's push.")]
    public float heavyMassCutoff = 8.0f;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody targetBody = hit.collider.attachedRigidbody;

        if (targetBody == null || targetBody.isKinematic) return;
        if (hit.moveDirection.y < -0.3f) return;

        Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z).normalized;
        float impulseForce = pushPower;

        if (targetBody.mass > heavyMassCutoff)
        {
            impulseForce /= (targetBody.mass / heavyMassCutoff);
        }

        targetBody.linearVelocity = pushDirection * impulseForce;
    }
}