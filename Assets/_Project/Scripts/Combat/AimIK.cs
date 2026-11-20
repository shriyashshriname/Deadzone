using UnityEngine;

namespace Deadzone.Combat
{
    /// <summary>
    /// Humanoid IK that makes the character hold and fire the weapon shown by WeaponVisual:
    /// right hand on the grip, left hand on the fore-grip, head and upper body turned toward the aim.
    /// Needs "IK Pass" on the Animator's base layer (set by CharacterArtBuilder). Blends off while
    /// downed/dead or when no gun is shown.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AimIK : MonoBehaviour
    {
        [SerializeField] private float blendSpeed = 8f;
        private Animator anim;
        private WeaponVisual gun;
        private Deadzone.Core.Health health;
        private float weight;
        public int IKCalls { get; private set; }
        public float Weight => weight;

        public void Bind(WeaponVisual v) => gun = v;

        private void Awake()
        {
            anim = GetComponent<Animator>();
            if (gun == null) gun = GetComponentInParent<WeaponVisual>();
            health = GetComponentInParent<Deadzone.Core.Health>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (anim == null || !anim.isHuman) return;
            IKCalls++;
            bool active = gun != null && gun.HasGun && (health == null || !health.IsDead) && gun.enabled;
            weight = Mathf.MoveTowards(weight, active ? 1f : 0f, Time.deltaTime * blendSpeed);
            if (weight <= 0.001f)
            {
                anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
                anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                anim.SetLookAtWeight(0f);
                return;
            }

            Quaternion gunRot = gun.GunRotation;
            // Hand bones point their palm "down" along the gun: roll the grip rotation so fingers wrap it.
            Quaternion rightRot = gunRot * Quaternion.Euler(0f, 0f, -90f);
            Quaternion leftRot = gunRot * Quaternion.Euler(0f, 0f, 90f);

            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, weight);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, weight * 0.8f);
            anim.SetIKPosition(AvatarIKGoal.RightHand, gun.GripPosition);
            anim.SetIKRotation(AvatarIKGoal.RightHand, rightRot);

            float leftW = weight * (gun.IsPistol ? 0.85f : 1f);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftW);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftW * 0.6f);
            anim.SetIKPosition(AvatarIKGoal.LeftHand, gun.IsPistol ? gun.GripPosition + gunRot * new Vector3(-0.04f, -0.02f, 0.02f) : gun.ForeGripPosition);
            anim.SetIKRotation(AvatarIKGoal.LeftHand, leftRot);

            anim.SetLookAtWeight(weight, 0.35f, 0.8f, 0f, 0.5f);
            anim.SetLookAtPosition(gun.AimPoint);
        }
    }
}
