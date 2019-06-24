using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

using System.IO;
using System;
using UnityEngine.UI;
using System.Text;
using Bvh;

namespace Bvh
{
    public class BVHReader : MonoBehaviour
    {

        public static BVHReader instance = null; //singleton pattern

        public GameObject model;

        public int currentFrame;
        public int currentBVH;
        private int previousBVH;

        [SerializeField]
        private Reader reader;

        [SerializeField]
        private Player player;

        //private DevelopingGUI developingGUI;
        private Text frameCountText;

        // define const
        const string CommandTextFile = "/Users/hsn/Desktop/Spatial_Gesture/Parser/command.txt";
        const int roomnum = 6;

        IDictionary<string, GameObject> locations = new Dictionary<string, GameObject>();

        private IEnumerator coroutine;

        private List<BVH> allLoadedBVHs = new List<BVH>();
        private BVH bvh;
        private int current_command_type;
        private int prev_command_type;
        private List<string> current_targets;
        private List<string> prev_targets;

        private List<BVH> EDEICTICBVHs = new List<BVH>();
        private List<BVH> SIZEBVHs = new List<BVH>();
        Vector3 pos = new Vector3(0, 0, 0);

        void Awake()
        {
            //singleton pattern
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
            DontDestroyOnLoad(gameObject);

            //Initialize rooms
            locations = reader.Location;
        }

        void Start()
        {
            
            //Initialize references to other GameObjects
            //developingGUI = DevelopingGUI.instance;
            frameCountText = GameObject.Find("Frame Count").GetComponent<Text>();
            model = GameObject.Find("ChrRachelDanceAvatar");
            prev_command_type = current_command_type = currentBVH = checkCommand(CommandTextFile);
            prev_targets = getTargets("/Users/hsn/Desktop/Spatial_Gesture/Parser/command.txt");

            //Load different type of BVH
            EDEICTICBVHs = reader.EDEICTICBVH;
            SIZEBVHs = reader.SIZEBVH;
            allLoadedBVHs = reader.LoadedBVH;

            if (currentBVH < 0 || currentBVH >= allLoadedBVHs.Count)
            {
                currentBVH = 0;
                previousBVH = currentBVH;
            }
            bvh = allLoadedBVHs[currentBVH];
            player.Play(bvh);
        }

        // Update is called once per frame
        void Update()
        {
            current_targets = getTargets("/Users/hsn/Desktop/Spatial_Gesture/Parser/command.txt");
            current_command_type = checkCommand("/Users/hsn/Desktop/Spatial_Gesture/Parser/command.txt");
            if (current_command_type != prev_command_type)
            {
                currentFrame = 0;
                prev_command_type = current_command_type;
                prev_targets = current_targets;
                // bvh = allLoadedBVHs[current_command_type];
                bvh = pickBVHList(current_command_type, current_targets);
                player.Play(bvh);
            }
            else if (!current_targets.SequenceEqual(prev_targets))
            {
                currentFrame = 0;
                prev_command_type = current_command_type;
                prev_targets = current_targets;
                // bvh = allLoadedBVHs[current_command_type];
                bvh = pickBVHList(current_command_type, current_targets);
                player.Play(bvh);
            }
        }

        void readTextFile(string file_path)
        {
            StreamReader inp_stm = new StreamReader(file_path);

            int count = 0;

            while (!inp_stm.EndOfStream)
            {
                string inp_ln = inp_stm.ReadLine();
                if (count == 0)
                {

                }
                else if (count == 1)
                {
                    // Debug.Log("Command Type: "+inp_ln);
                }
                else
                {
                    Debug.Log(inp_ln);
                }

                count++;
            }

            inp_stm.Close();
        }

        //check command
        int checkCommand(string file_path)
        {
            StreamReader inp_stm = new StreamReader(file_path);

            string inp_ln = inp_stm.ReadLine();

            inp_stm.Close();

            switch (inp_ln)
            {
                case "EDEICTIC":
                    return 0;
                case "SIZE":
                    return 3;
                case "Path":
                    return 4;
                default:
                    return 1;
            }
        }

        //Han check targets
        List<string> getTargets(string file_path)
        {
            StreamReader inp_stm = new StreamReader(file_path);

            List<string> targets = new List<string>(); ;

            string inp_ln = inp_stm.ReadLine();

            while (true)
            {
                inp_ln = inp_stm.ReadLine();
                if (inp_ln == null)
                {
                    break;
                }
                targets.Add(inp_ln);
            }

            inp_stm.Close();

            return targets;

        }

        // Han pick the right bvh list
        private BVH pickBVHList(int type, List<string> targets)
        {
            switch (type)
            {
                case 0:
                    // EDEICTIC
                    return pickBVHSingle(EDEICTICBVHs, targets[0]);
                case 3:
                    // SIZE
                    return pickBVHSingle(SIZEBVHs, targets[0]);
                default:
                    return null;
                    break;
            }
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

        // Han pick the closet
        private BVH pickBVHSingle(List<BVH> BVHList, string target)
        {
            BVH tempBVH = BVHList[0];
            Vector3 motionPosition;

            if (locations.ContainsKey(target))
            {
                Vector3 targetpos = locations[target].transform.position;
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
                return bvh;
            }

            return tempBVH;
        }
    }

    public class BVH
    {
        public string name;
        public List<Joint> jointList;
        public List<float[]> frames;
        public int frameCount;
        public float frameTime;
        public int totalChannels = 0;
        public float startTime;
        public float endTime;
    }

    public class Joint
    {
        public string name;
        public int channelCount;
        public int channelStart;
        public static int totalChannels = 0;
        public char[] channelOrder;
        public Transform jointTransform;
    }
}

