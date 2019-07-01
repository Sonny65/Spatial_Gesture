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
    public class BVHPicker : MonoBehaviour
    {

        [SerializeField]
        private BVHReader bvhReader;

        // Han pick the right bvh list
        public BVH pickBVHList(int type, List<string> targets)
        {
            switch (type)
            {
                case 0:
                    // EDEICTIC
                    return pickBVHSingle(bvhReader.EDEICT, targets[0]);
                case 3:
                    // SIZE
                    return pickBVHSingle(bvhReader.SIZE, targets[0]);
                default:
                    return null;
            }
        }

        // Han pick the closet
        private BVH pickBVHSingle(List<BVH> BVHList, string target)
        {
            BVH tempBVH = BVHList[0];
            Vector3 motionPosition;

            if (bvhReader.LOC.ContainsKey(target))
            {
                Vector3 targetpos = bvhReader.LOC[target].transform.position;
                float distance = float.MaxValue;

                foreach (BVH motion in BVHList)
                {
                    // Debug.Log(allLoadedBVHs[current_command_type].frames[-1][0]);
                    // Debug.Log(motion.frames[-1][0]);
                    // float[] lastframe = motion.frames[motion.frames.Count - 1];
                    motionPosition = calculateRightWristDistance(motion);
                    // motionPosition = new Vector3(lastframe[25],lastframe[26],lastframe[27]);

                    float currentDistance = Vector3.Distance(targetpos, motionPosition);

                    if (currentDistance < distance)
                    {
                        tempBVH = motion;
                        distance = currentDistance;
                    }
                }

            }
            else
            {
                return tempBVH;
            }

            return tempBVH;
        }

        // calculate the distance between wrist
        private Vector3 calculateRightWristDistance(BVH clip)
        {
            int frameNum = (int)(clip.endTime / clip.frameTime);
            float[] frame = clip.frames[frameNum];

            foreach (Joint joint in clip.jointList)
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
                joint.jointTransform.localPosition = position;

                if (joint.name == "Right_Hand")
                {
                    return joint.jointTransform.position;
                }

                joint.jointTransform.localRotation = Quaternion.Euler(new Vector3(rotX, rotY, rotZ));
            }

            return new Vector3(0, 0, 0);
        }
    }
}