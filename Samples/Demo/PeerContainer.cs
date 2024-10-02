using OdinNative.Unity.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OdinNative.Unity.Samples
{
    /// <summary>
    /// Manages the audio playback and visual representation of a peer in a 3D environment using the Odin SDK.
    /// Contains demo functionalities for movement, interaction, and audio control.
    /// </summary>
    public class PeerContainer : MonoBehaviour
    {
        public float HearingDistance;
        private PlaybackComponent Playback;
        private Color lastCubeColor;

        #region Demo

        public bool AutoMove = true; // Enables or disables automatic movement.
        private Vector3 MoveOffset;
        private float MouseZCoord;

        public float Speed = 5;
        public float WaitTime = 0.3f;
        public float TurnSpeed = 90;

        public Light Pointlight; // Light indicating the peer's state.
        public LayerMask HearingMask; // Mask to determine hearing obstacles.
        private Color originalPointlightColour;

        public Light Spotlight; // Light representing the peer's spotlight.
        public float ViewDistance; // The distance of the peer's spotlight.

        private UnityEngine.UI.RawImage Icon; // UI element for peer status.
        public Texture2D Area;
        public Texture2D Muff;
        public Texture2D Room;
        public Texture2D Voice;
        public Texture2D Walk;

        public Transform pathContainer; // Container for movement checkpoints.
        private GameObject Player; // Reference to the player object.

        private Coroutine MoveWorker; // Movement coroutine.
        private IEnumerator DoMove; // Enumerator for following the path.

        #endregion Demo


        private void Start()
        {
            // Cache the PlaybackComponent of this container
            Playback = gameObject.GetComponent<PlaybackComponent>();
            if (Playback == null)
            {
                Debug.LogError("PlaybackComponent is missing on this GameObject.");
                return;
            }
            
            // Set the AudioSource max distance to the hearing distance of this container
            Playback.PlaybackSource.maxDistance = HearingDistance;

            #region Demo

            Icon = gameObject.GetComponentInChildren<UnityEngine.UI.RawImage>();
            Player = GameObject.FindGameObjectWithTag("Player");

            if (Pointlight != null)
            {
                Pointlight.range = HearingDistance;
                originalPointlightColour = Pointlight.color;
            }

            if (Spotlight != null)
            {
                Spotlight.range = ViewDistance;
            }

            // Initialize path checkpoints if the path container exists
            if (pathContainer != null)
            {
                var checkpoints = new Vector3[pathContainer.childCount];
                for (var i = 0; i < checkpoints.Length; i++)
                {
                    checkpoints[i] = pathContainer.GetChild(i).position;
                    checkpoints[i] = new Vector3(checkpoints[i].x, transform.position.y, checkpoints[i].z);
                }

                DoMove = FollowPath(checkpoints);
            }
            else
            {
                AutoMove = false;
            }

            if (AutoMove)
                MoveWorker = StartCoroutine(DoMove);

            #endregion Demo
        }

        private void Update()
        {
            #region Demo

            // Handle movement
            if (AutoMove)
            {
                if (MoveWorker == null && DoMove != null)
                    MoveWorker = StartCoroutine(DoMove);
            }
            else if (AutoMove == false && MoveWorker != null)
            {
                StopCoroutine(MoveWorker);
                MoveWorker = null;
            }

            #endregion Demo

            if (Playback == null) return;
            CheckTalkIndicator();

            // Handle audio playback and visual feedback based on the player's hearing status
            switch (PlayerCanHear())
            {
                case DemoHearingType.Normal:
                    Icon.texture = Voice;
                    Pointlight.color = originalPointlightColour;
                    // set for this AudioSource in PlaybackComponent the AudioMixerGroup
                    Playback.PlaybackSource.outputAudioMixerGroup =
                        OdinHandler.Instance.PlaybackAudioMixer.FindMatchingGroups("Normal")[0];
                    // set how much this AudioSource is affected by 3D spatialisation calculations
                    Playback.PlaybackSource.spatialBlend = 1.0f;
                    break;
                case DemoHearingType.Blocked:
                    Icon.texture = Muff;
                    Pointlight.color = Color.yellow;
                    Playback.PlaybackSource.outputAudioMixerGroup =
                        OdinHandler.Instance.PlaybackAudioMixer.FindMatchingGroups("Muffled")[0];
                    Playback.PlaybackSource.spatialBlend = 1.0f;
                    break;
                case DemoHearingType.Echo:
                    Icon.texture = Room;
                    Pointlight.color = Color.red;
                    Playback.PlaybackSource.outputAudioMixerGroup =
                        OdinHandler.Instance.PlaybackAudioMixer.FindMatchingGroups("Room")[0];
                    Playback.PlaybackSource.spatialBlend = 1.0f;
                    break;
                case DemoHearingType.None:
                    Icon.texture = Walk;
                    Pointlight.color = Color.gray;
                    Playback.PlaybackSource.outputAudioMixerGroup =
                        OdinHandler.Instance.PlaybackAudioMixer.FindMatchingGroups("Radio")[0];
                    Playback.PlaybackSource.spatialBlend = 0.0f; // 0.0 makes the sound full 2D, 1.0 makes it full 3D
                    break;
            }
        }

        /// <summary>
        /// Checks if the peer is talking and update its visual indicator if talking
        /// </summary>
        private void CheckTalkIndicator()
        {
            var cubeMaterial = Playback.GetComponentInParent<Renderer>().material;
            if (Playback.HasActivity)
            {
                lastCubeColor = cubeMaterial.color;
                cubeMaterial.color = Color.green;
            }
            else
            {
                cubeMaterial.color = lastCubeColor;
            }
        }

        
        #region Demo
        
        /// <summary>
        /// Determines the way the playback component will play back the media stream. 
        /// </summary>
        private DemoHearingType PlayerCanHear()
        {
            if (Player == null) return DemoHearingType.None;
            if (Vector3.Distance(transform.position, Player.transform.position) < HearingDistance)
            {
                if (Physics.Linecast(transform.position, Player.transform.position, HearingMask))
                {
                    if (Physics.OverlapSphere(transform.position, HearingDistance / 2, HearingMask).Length > 1)
                        return DemoHearingType.Echo;
                    else
                        return DemoHearingType.Blocked;
                }
                else
                {
                    return DemoHearingType.Normal;
                }
            }

            return DemoHearingType.None;
        }

        /// <summary>
        /// Types of playback sound adjustments that can be applied.
        /// </summary>
        private enum DemoHearingType
        {
            None,
            Blocked,
            Echo,
            Normal
        }

        private void OnMouseDown()
        {
            if (AutoMove) return;
            MouseZCoord = Camera.main.WorldToScreenPoint(gameObject.transform.position).z;
            MoveOffset = gameObject.transform.position - GetMouseWorldPosition();
        }

        private void OnMouseDrag()
        {
            if (AutoMove) return;
            gameObject.transform.position = MoveOffset + GetMouseWorldPosition();
        }

        private Vector3 GetMouseWorldPosition()
        {
            var mousePoint = Input.mousePosition;
            mousePoint.z = MouseZCoord;

            return Camera.main.ScreenToWorldPoint(mousePoint);
        }

        

        /// <summary>
        /// Sample movement, game object will follow the given list of checkpoints.
        /// </summary>
        private IEnumerator FollowPath(Vector3[] checkpoints)
        {
            transform.position = checkpoints[0];

            var targetCheckpointIndex = 1;
            var targetCheckpoint = checkpoints[targetCheckpointIndex];
            transform.LookAt(targetCheckpoint);

            while (true)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetCheckpoint, Speed * Time.deltaTime);
                if (transform.position == targetCheckpoint)
                {
                    targetCheckpointIndex = (targetCheckpointIndex + 1) % checkpoints.Length;
                    targetCheckpoint = checkpoints[targetCheckpointIndex];
                    yield return new WaitForSeconds(WaitTime);
                    yield return StartCoroutine(TurnToFace(targetCheckpoint));
                }

                yield return null;
            }
        }

        /// <summary>
        /// Sample Movement
        /// </summary>
        private IEnumerator TurnToFace(Vector3 lookTarget)
        {
            var directionToLookTarget = (lookTarget - transform.position).normalized;
            var targetAngle = 90 - Mathf.Atan2(directionToLookTarget.z, directionToLookTarget.x) * Mathf.Rad2Deg;

            while (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, targetAngle)) > 0.05f)
            {
                var angle = Mathf.MoveTowardsAngle(transform.eulerAngles.y, targetAngle, TurnSpeed * Time.deltaTime);
                transform.eulerAngles = Vector3.up * angle;
                yield return null;
            }
        }

        /// <summary>
        /// Sample gizmos
        /// </summary>
        private void OnDrawGizmos()
        {
            // Path
            if (pathContainer != null)
            {
                var startPosition = pathContainer.GetChild(0).position;
                var previousPosition = startPosition;

                foreach (Transform checkpoint in pathContainer)
                {
                    Gizmos.DrawSphere(checkpoint.position, .3f);
                    Gizmos.DrawLine(previousPosition, checkpoint.position);
                    previousPosition = checkpoint.position;
                }

                Gizmos.DrawLine(previousPosition, startPosition);
            }

            // Sound
            Gizmos.DrawWireSphere(gameObject.transform.position, HearingDistance);
        }

        #endregion Demo
    }
}