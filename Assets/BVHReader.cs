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
        private List<float[]> EDEICTICTimings = new List<float[]>();
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
        }

        void Start()
        {
            //Initialize rooms
            registerRoom();

            //Initialize references to other GameObjects
            //developingGUI = DevelopingGUI.instance;
            frameCountText = GameObject.Find("Frame Count").GetComponent<Text>();
            model = GameObject.Find("ChrRachelDanceAvatar");
            prev_command_type = current_command_type = currentBVH = checkCommand(CommandTextFile);
            prev_targets = getTargets("/Users/hsn/Desktop/Spatial_Gesture/Parser/command.txt");

            //Load different type of BVH
            EDEICTICBVHs = reader.EDEICTICBVH;
            SIZEBVHs = reader.SIZEBVH;

            // loadBVH("DeicticIndex01");
            // loadBVH("DeicticIndexVariations_RoughSolve");
            // loadBVH("1001");
            // loadBVH("zero_pose");
            // loadBVH("DeicticIndex01");
            // loadBVH("DeicticIndex02");
            // loadBVH("DeicticIndex03");
            // loadBVH("DeicticIndex04");
            // loadBVH("DeicticIndex05");
            // loadBVH("DeicticIndex06");
            // loadBVH("DeicticIndex07");
            // loadBVH("DeicticIndex08");
            loadBVH("zero_pose");
            // loadBVH("head_y_z_90");
            //loadBVH("frame_from_100");
            // loadBVH("bvh1001_0d_fingers");
            // loadBVH("1005");
            // loadBVH("1006");
            // loadBVH("1007");
            //loadBVH("Shakespeare_HSHP");
            //loadBVH("roadrunner_HSHP");
            //GoToFrame(0);

            if (currentBVH < 0 || currentBVH >= allLoadedBVHs.Count)
            {
                currentBVH = 0;
                previousBVH = currentBVH;
            }
            bvh = allLoadedBVHs[currentBVH];
            coroutine = PlayAnimation();
            StartCoroutine(coroutine);
        }

        // Update is called once per frame
        void Update()
        {
            // Han comment out because of not including
            // if (currentBVH != previousBVH && currentBVH >= 0 && currentBVH < allLoadedBVHs.Count)
            // {
            //     if (currentBVH < 0 || currentBVH >= allLoadedBVHs.Count)
            //     {
            //         currentBVH = previousBVH;
            //     }
            //     previousBVH = currentBVH;
            //     currentFrame = 0;
            //     bvh = allLoadedBVHs[currentBVH];
            //     Debug.Log(bvh.name);
            //     coroutine = PlayAnimation();
            //     StartCoroutine(coroutine);
            // }

            current_targets = getTargets("/Users/hsn/Desktop/Spatial_Gesture/Parser/command.txt");
            current_command_type = checkCommand("/Users/hsn/Desktop/Spatial_Gesture/Parser/command.txt");
            if (current_command_type != prev_command_type)
            {
                currentFrame = 0;
                prev_command_type = current_command_type;
                prev_targets = current_targets;
                // bvh = allLoadedBVHs[current_command_type];
                bvh = pickBVHList(current_command_type, current_targets);
                coroutine = PlayAnimation();
                StartCoroutine(coroutine);
            }
            else if (!current_targets.SequenceEqual(prev_targets))
            {
                currentFrame = 0;
                prev_command_type = current_command_type;
                prev_targets = current_targets;
                // bvh = allLoadedBVHs[current_command_type];
                bvh = pickBVHList(current_command_type, current_targets);
                coroutine = PlayAnimation();
                StartCoroutine(coroutine);
            }

            // Debug.Log(current_command_type);
            // Debug.Log(prev_command_type);
            // currentFrame = 0;
            // prev_command_type = current_command_type;
            // bvh = allLoadedBVHs[current_command_type];
            // bvh = pickBVHList(current_command_type,getTargets("/Users/hsn/Desktop/Spatial_Gesture/Parser/command.txt"));
            // coroutine = PlayAnimation();
            // StartCoroutine(coroutine);
        }

        void loadBVH(string name)
        {
            //when we get this from a dll we'll need to change this to something else
            string path = "Assets/BVH/" + name + ".bvh";

            try
            {
                bvh = LoadBVHFromAssets(path);
                allLoadedBVHs.Add(bvh);
            }
            catch
            {
                //StartCoroutine(GetBVHFromServer(name));
                throw new Exception("Couldn't load bvh named " + name);
            }
        }

        // This is sloppy and needs to be rethought out
        IEnumerator GetBVHFromServer(string name)
        {
            BVH aBvh = new BVH();

            WWW www = new WWW("http://hjessmith.com/BVH/" + name + ".bvh");
            yield return www;

            byte[] byteArray = Encoding.ASCII.GetBytes(www.text);
            MemoryStream stream = new MemoryStream(byteArray);
            StreamReader reader = new StreamReader(stream);

            ParseHierarchy(reader, aBvh);
            ParseMotion(reader, aBvh);

            allLoadedBVHs.Add(aBvh);
            bvh = aBvh;
        }

        BVH LoadBVHFromAssets(string path)
        {
            BVH bvh = new BVH();
            StreamReader reader;

            try { bvh.name = Path.GetFileName(path); }
            catch (Exception) { throw new Exception("Could not extract filename from '" + path + "'"); }

            try { reader = new StreamReader(path); }
            catch (Exception) { throw new Exception("Could not create StreamReader from " + path + "."); }

            ParseHierarchy(reader, bvh);
            ParseMotion(reader, bvh);

            return bvh;
        }

        void ParseHierarchy(StreamReader reader, BVH bvh)
        {
            string line;
            bvh.jointList = new List<Joint>();

            line = reader.ReadLine().Trim();
            if (line != "HIERARCHY")
            {
                throw new Exception("BVH does not start with 'HIERARCHY'");
            }

            while (!reader.EndOfStream)
            {
                line = reader.ReadLine().Trim();

                if (line.IndexOf("MOTION") != -1)
                {
                    break;
                }

                if (line.IndexOf("ROOT") != -1 || line.IndexOf("JOINT") != -1)
                {
                    if (bvh.jointList.Count != 0 && bvh.jointList[bvh.jointList.Count - 1].channelOrder == null)
                    {
                        throw new Exception("Attempted to create new joint before joint " + bvh.jointList[bvh.jointList.Count - 1].name + " had its channel data parsed.");
                    }

                    Joint joint = parseNewJoint(line, reader);
                    joint.channelStart = bvh.totalChannels;
                    bvh.totalChannels += joint.channelCount;
                    bvh.jointList.Add(joint);
                    continue;
                }
            }
        }

        Joint parseNewJoint(string line, StreamReader reader)
        {
            string jointName = line.Split(' ')[1];
            Debug.Assert(jointName != null, "Couldn't extract name from line '" + line + "'");
            Joint joint;
            if (jointName == "Head_comp")
            {
                joint = new Joint() { name = "JtSkullA" };
                jointName = joint.name;
            }
            else
            {
                joint = new Joint() { name = jointName };
            }

            //TODO: This will be problematic if there are two skeletons in the scene with the same joint names. Fix this.
            Transform jointTransform = GameObject.Find(jointName).transform;
            if (jointTransform == null)
            {
                throw new Exception("Could not find joint named '" + jointName + "' on model.");
            }
            joint.jointTransform = jointTransform;

            line = reader.ReadLine().Trim();
            if (line != "{")
            {
                throw new Exception("Malformed BVH in line after '" + line + "'. '{' Expected.");
            }

            line = reader.ReadLine().Trim();
            if (line.Split(' ')[0] != "OFFSET")
            {
                throw new Exception("Malformed BVH in second line after '" + line + "'. Does not start with 'OFFSET'");
            }

            line = reader.ReadLine().Trim();
            string[] contents = line.Split(' ');
            if (contents[0] != "CHANNELS")
            {
                throw new Exception("Malformed BVH in third line after '" + line + "'. Does not start with 'CHANNELS'");
            }

            int channelCount = ParseFast(contents[1]);
            if (channelCount + 2 != contents.Length)
            {
                throw new Exception("Channel count mismatch for joint '" + joint.name + "'. Expected " + channelCount.ToString() + " channels, but detected " + (contents.Length - 2).ToString() + ".");
            }

            joint.channelCount = channelCount;
            char[] channelOrder = new char[joint.channelCount];
            for (int i = 0; i < joint.channelCount; ++i)
            {
                string channel = contents[i + 2]; //skip OFFSET and channelCount
                switch (channel)
                {
                    case "Xposition":
                        channelOrder[i] = 'x';
                        break;
                    case "Yposition":
                        channelOrder[i] = 'y';
                        break;
                    case "Zposition":
                        channelOrder[i] = 'z';
                        break;
                    case "Xrotation":
                        channelOrder[i] = 'X';
                        break;
                    case "Yrotation":
                        channelOrder[i] = 'Y';
                        break;
                    case "Zrotation":
                        channelOrder[i] = 'Z';
                        break;
                    default:
                        throw new Exception("Unrecognized channel when parsing joint" + joint.name);
                }

            }
            joint.channelOrder = channelOrder;

            return joint;
        }

        void ParseMotion(StreamReader reader, BVH bvh)
        {
            string line;
            bvh.frames = new List<float[]>();

            line = reader.ReadLine().Trim();
            try { bvh.frameCount = ParseFast(line.Split(' ')[1]); }
            catch (Exception) { throw new Exception("Could not extract frame count from line directly after MOTION"); }

            try { bvh.frameTime = float.Parse(reader.ReadLine().Trim().Split(' ')[2]); }
            catch (Exception) { throw new Exception("Count not extract frame time from second line after MOTION"); }


            while (!reader.EndOfStream)
            {
                line = reader.ReadLine().Trim();
                float[] frame = Array.ConvertAll(line.Split(' '), element =>
                {
                    try
                    {
                        return float.Parse(element);
                    }
                    catch (Exception)
                    {
                        //TODO: Add Debug message here indicating failure
                        return 0f;
                    }
                });

                if (frame.Length != bvh.totalChannels)
                {
                    throw new Exception("Length of frame " + bvh.frames.Count.ToString() + "does not equal total number of channels (" + bvh.totalChannels.ToString() + ")");
                }

                bvh.frames.Add(frame);
            }

            if (bvh.frames.Count != bvh.frameCount)
            {
                throw new Exception("Total number of frames in bvh (" + bvh.frames.Count.ToString() + ") does not equal specified count (" + bvh.frameCount.ToString() + ")");
            }

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
            frameCountText.text = "Frame: " + currentFrame + "/" + (bvh.frameCount - 1);
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

        int ParseFast(string s)
        {
            int r = 0;
            for (var i = 0; i < s.Length; i++)
            {
                char letter = s[i];
                r = 10 * r;
                r = r + (int)char.GetNumericValue(letter);
            }
            return r;
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

        //Han get the location of all the according rooms
        private void registerRoom()
        {
            GameObject[] rooms;

            rooms = GameObject.FindGameObjectsWithTag("Location");

            foreach (GameObject room in rooms)
            {
                locations.Add(new KeyValuePair<string, GameObject>(room.transform.name, room));
            }
        }

        //Han load bvh into of different type
        void loadBVHType(string type)
        {

            // Han get all the files name inside a directory
            string[] allfiles = Directory.GetFiles("Assets/BVH/" + type + "/", "*.bvh", SearchOption.AllDirectories);
            string timingFile = "Assets/BVH/" + type + "/timing.txt";
            List<float[]> timingsFrames = readTiming(timingFile);
            int count = 0;

            foreach (string i in allfiles)
            {
                try
                {
                    bvh = LoadBVHFromAssets(i);
                    bvh.startTime = timingsFrames[count][0];
                    bvh.endTime = timingsFrames[count][1];
                    addBVH(type, bvh);
                }
                catch
                {
                    //StartCoroutine(GetBVHFromServer(name));
                    // throw new Exception("Couldn't load bvh named " + i);
                }
                count += 1;
            }
        }

        // Han load BVH into according array
        void addBVH(string type, BVH abvh)
        {
            switch (type)
            {
                case "EDEICTIC":
                    // EDEICTIC
                    EDEICTICBVHs.Add(abvh);
                    break;
                case "SIZE":
                    // SIZE
                    SIZEBVHs.Add(abvh);
                    break;
                default:
                    break;
            }
        }

        // Han load the timeing information
        private List<float[]> readTiming(string file)
        {
            List<float[]> timings = new List<float[]>();

            using (var reader = new StreamReader(file))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] timingString = line.Split(' ');
                    float[] timing = new float[] { float.Parse(timingString[0]), float.Parse(timingString[1]) };
                    timings.Add(timing);
                }
            }

            return timings;
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

