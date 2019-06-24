using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using System.IO;
using System;
using UnityEngine.UI;
using System.Text;

namespace Bvh
{
    public class Player : MonoBehaviour
    {
        private IEnumerator coroutine;
        private int currentFrame;
        private BVH bvh;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Play(BVH currentbvh)
        {
            bvh = currentbvh;
            currentFrame = 0;
            coroutine = PlayAnimation();
            StartCoroutine(coroutine);
        }

        public IEnumerator PlayAnimation()
        {
            float startTime = Time.time;
            int startingFrame = currentFrame;
            while (currentFrame < bvh.frameCount)
            {
                GoToFrame(currentFrame);
                yield return new WaitForSecondsRealtime(bvh.frameTime);
                currentFrame = startingFrame + (int)Mathf.Floor((Time.time - startTime) / bvh.frameTime);
            }
            yield break;
        }

        public void GoToFrame(int newFrame)
        {
            if (newFrame < 0 || newFrame >= bvh.frameCount)
            {
                Debug.Log("Requested Frame " + newFrame.ToString() + " is outside bounds [0," + bvh.frameCount.ToString() + ")");
                return;
            }

            currentFrame = newFrame;
            //frameCountText.text = "Frame: " + currentFrame + "/" + (bvh.frameCount - 1);
            UpdateJointTransforms();

            //GoToFrame(++newFrame);
        }

        void UpdateJointTransforms()
        {
            float[] frame = bvh.frames[currentFrame];

            foreach (Joint joint in bvh.jointList)
            {
                Vector3 position = joint.jointTransform.localPosition;
                float rotX = 0f, rotY = 0f, rotZ = 0f;

                int channel_idx = 0;
                foreach (char channel in joint.channelOrder)
                {
                    int channelPosition = joint.channelStart + channel_idx++;
                    switch (channel)
                    {
                        case 'x':
                            position.x = -1 * frame[channelPosition]; //TODO: Confirm that axis are reversed in Unity
                            break;
                        case 'y':
                            position.y = frame[channelPosition];
                            break;
                        case 'z':
                            position.z = frame[channelPosition];
                            break;
                        case 'X':
                            rotX = 1 * frame[channelPosition];
                            break;
                        case 'Y':
                            rotY = -1 * frame[channelPosition]; //TODO: Confirm that axes are reversed in Unity
                            if (joint.name == "JtSkullA") rotY -= 90; //TODO: Find fix so that this line isn't needed
                            break;
                        case 'Z':
                            rotZ = -1 * frame[channelPosition]; //TODO: Confirm that axes are reversed in Unity
                            if (joint.name == "JtSkullA") rotZ -= 90; //TODO: Find fix so that this line isn't neede
                            break;
                        default:
                            throw new Exception("Unrecognized channelOrder char: " + channel);
                    }
                }
                //joint.jointTransform.localPosition = position;

                joint.jointTransform.localRotation = Quaternion.Euler(new Vector3(rotX, rotY, rotZ));
            }
        }
    }
}