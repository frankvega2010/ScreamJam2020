using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (CharacterController))]
    [RequireComponent(typeof (AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        public bool m_IsWalking;
        public float m_WalkSpeed;
        [Range(0f, 2f)] public float m_WalkstepLenghten;
        public float m_RunSpeed;
        [Range(0f, 2f)] public float m_RunstepLenghten;
        public bool enableJump;
        public float m_JumpSpeed;
        public float m_StickToGroundForce;
        public float m_GravityMultiplier;
        public MouseLook m_MouseLook;
        public bool m_UseFovKick;
        public FOVKick m_FovKick = new FOVKick();
        public bool m_UseHeadBob;
        public CurveControlledBob m_HeadBob = new CurveControlledBob();
        public float m_BobInterval;
        public LerpControlledBob m_JumpBob = new LerpControlledBob();
        public float m_StepInterval;
        public AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        public AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        public AudioClip m_LandSound;           // the sound played when character touches back on ground.

        public Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;

        //Crouch

        public bool m_IsCrouching;
        public float originalHeight;
        public float crouchHeight=0.5f;
        public float t = 0;
        public float crouchSpeed;
        public float crouchMovSpeed;
        public LayerMask layer;

        public float originalSpeed;
        public float originalRunSpeed;

        // Use this for initialization
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
           // m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_BobInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);
            originalHeight = m_CharacterController.height;
            originalSpeed = m_WalkSpeed;
            originalRunSpeed = m_RunSpeed;
        }


        // Update is called once per frame
        private void Update()
        {
            RotateView();
            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump && CanStandUp() && enableJump)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            if (CrossPlatformInputManager.GetButtonDown("Crouch"))
            {
                if (CanStandUp())
                {
                    m_IsCrouching = !m_IsCrouching;
                    t = 0;
                }
                
            }

            CheckCrouch();
            
            m_PreviouslyGrounded = m_CharacterController.isGrounded;
            CharacterUpdate();
        }

        private void CheckCrouch()
        {
            if (m_IsCrouching)
            {
                t += Time.deltaTime;
                m_CharacterController.height = Mathf.Lerp(originalHeight, crouchHeight, t * crouchSpeed);
                m_WalkSpeed = crouchMovSpeed;
            }
            else
            {
                t += Time.deltaTime;
                m_CharacterController.height = Mathf.Lerp(crouchHeight, originalHeight, t * crouchSpeed);
                m_WalkSpeed = originalSpeed;
            }
        }

        private bool CanStandUp()
        {
            if (Physics.CheckCapsule(transform.position + Vector3.up * (crouchHeight),transform.position + Vector3.up * (originalHeight - m_CharacterController.radius),
                 m_CharacterController.radius - Physics.defaultContactOffset, layer, QueryTriggerInteraction.Ignore))
                return false;
            else
                return true; // can  stand up
        }

        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void CharacterUpdate()
        {
            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.x = desiredMove.x*speed;
            m_MoveDir.z = desiredMove.z*speed;


            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else
            {
                m_MoveDir += Physics.gravity*m_GravityMultiplier* Time.deltaTime;
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir* Time.deltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);

           //m_MouseLook.UpdateCursorLock();
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {

            if (speed > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += ((speed * (m_IsWalking ? m_WalkstepLenghten : m_RunstepLenghten))) *
                             Time.deltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob((speed*(m_IsWalking ? m_WalkstepLenghten : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxisRaw("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxisRaw("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            if (!m_IsCrouching)
            {
                m_IsWalking = !CrossPlatformInputManager.GetButton("Sprint");
            }
            
            if(m_IsCrouching && !m_IsWalking)
            {
                m_RunSpeed = crouchMovSpeed;
            }
            else
            {
                m_RunSpeed = originalRunSpeed;
            }
#endif
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            if(horizontal == 0 && vertical == 0)
            {
                speed = 0;
            }
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }


        private void RotateView()
        {
            m_MouseLook.LookRotation (transform, m_Camera.transform);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
        }
    }
}
