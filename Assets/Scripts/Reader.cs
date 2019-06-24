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
    public class Reader : MonoBehaviour
    {
        // loaded bvh, outdated
        private List<BVH> allLoadedBVHs = new List<BVH>();
        // where all loaded bvhs are stored
        // Edeitic
        private List<BVH> EDEICTICBVHs = new List<BVH>();
        private List<float[]> EDEICTICTimings = new List<float[]>();
        private List<BVH> SIZEBVHs = new List<BVH>();

        IDictionary<string, GameObject> locations = new Dictionary<string, GameObject>();

        public List<BVH> LoadedBVH
        {
            get { return allLoadedBVHs; }
        }

        public List<BVH> EDEICTICBVH
        {
            get { return EDEICTICBVHs; }
        }

        public List<BVH> SIZEBVH
        {
            get { return SIZEBVHs; }
        }

        // Use this for initialization
        void Start()
        {
            loadBVHType("EDEICTIC");
            loadBVHType("SIZE");

            loadBVH("zero_pose");
        }

        // Update is called once per frame
        void Update()
        {

        }

        void loadBVHType(string type)
        {
            BVH bvh = new BVH();
            // get all the files name inside a directory
            string[] allfiles = Directory.GetFiles("Assets/BVH/" + type + "/", "*.bvh", SearchOption.AllDirectories);
            string timingFile = "Assets/BVH/" + type + "/timing.txt";
            List<float[]> timingsFrames = readTiming(timingFile);
            int count = 0;

            foreach (string i in allfiles){
              try
              {
                  bvh = LoadBVHFromAssets(i);
                  bvh.startTime = timingsFrames[count][0];
                  bvh.endTime = timingsFrames[count][1];
                  addBVH(type,bvh);
              }
              catch
              {
                  //StartCoroutine(GetBVHFromServer(name));
                  // throw new Exception("Couldn't load bvh named " + i);
              }
              count += 1;
            }
        }

        void loadBVH(string name)
        {
            BVH bvh = new BVH();
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

        // Load the timeing information
        private List<float[]> readTiming(string file){
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

        // Load single BVH
        BVH LoadBVHFromAssets(string path)
        {
            BVH bvh = new BVH();
            StreamReader reader;

            try { bvh.name = Path.GetFileName(path); }
                catch(Exception) { throw new Exception("Could not extract filename from '" + path + "'"); }

            try { reader = new StreamReader(path); }
                catch(Exception) { throw new Exception("Could not create StreamReader from " + path + "."); }

            ParseHierarchy(reader, bvh);
            ParseMotion(reader, bvh);

            return bvh;
        }

        void addBVH(string type, BVH abvh){
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

        void ParseHierarchy(StreamReader reader, BVH bvh)
        {
            string line;
            bvh.jointList = new List<Joint>();

            line = reader.ReadLine().Trim();
            if (line != "HIERARCHY"){
                throw new Exception("BVH does not start with 'HIERARCHY'");}

            while (!reader.EndOfStream)
            {
                line = reader.ReadLine().Trim();

                if(line.IndexOf("MOTION") != -1){
                    break;}

                if(line.IndexOf("ROOT") != -1 || line.IndexOf("JOINT") != -1){
				    if (bvh.jointList.Count != 0 && bvh.jointList[bvh.jointList.Count -1].channelOrder == null){
                        throw new Exception("Attempted to create new joint before joint " + bvh.jointList[bvh.jointList.Count-1].name + " had its channel data parsed.");}

                    Joint joint = parseNewJoint(line, reader);
                    joint.channelStart = bvh.totalChannels;
                    bvh.totalChannels += joint.channelCount;
                    bvh.jointList.Add(joint);
                    continue;
                }
            }
        }

        void ParseMotion(StreamReader reader, BVH bvh)
        {
            string line;
            bvh.frames = new List<float[]>();

            line = reader.ReadLine().Trim();
            try{ bvh.frameCount = ParseFast(line.Split(' ')[1]); }
                catch(Exception){ throw new Exception("Could not extract frame count from line directly after MOTION"); }

            try{ bvh.frameTime = float.Parse(reader.ReadLine().Trim().Split(' ')[2]); }
                catch (Exception){ throw new Exception("Count not extract frame time from second line after MOTION"); }


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

                if (frame.Length != bvh.totalChannels){
                    throw new Exception("Length of frame " + bvh.frames.Count.ToString() + "does not equal total number of channels (" + bvh.totalChannels.ToString() + ")");}

                bvh.frames.Add(frame);
            }

            if (bvh.frames.Count != bvh.frameCount){
                throw new Exception("Total number of frames in bvh (" + bvh.frames.Count.ToString() + ") does not equal specified count (" + bvh.frameCount.ToString() + ")");}

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

        Joint parseNewJoint(string line, StreamReader reader)
        {
            string jointName = line.Split(' ')[1];
            Debug.Assert(jointName != null,"Couldn't extract name from line '" + line + "'");
		    Joint joint;
		    if (jointName == "Head_comp") {
			    joint = new Joint (){ name = "JtSkullA" };
			    jointName = joint.name;
		    } else {
			    joint = new Joint (){ name = jointName };
		    }

            //TODO: This will be problematic if there are two skeletons in the scene with the same joint names. Fix this.
		    Transform jointTransform = GameObject.Find(jointName).transform;
            if (jointTransform == null){
                throw new Exception("Could not find joint named '" + jointName + "' on model.");}
		    joint.jointTransform = jointTransform;

            line = reader.ReadLine().Trim();
            if (line != "{"){
                throw new Exception("Malformed BVH in line after '" + line + "'. '{' Expected.");}

            line = reader.ReadLine().Trim();
            if (line.Split(' ')[0] != "OFFSET"){
                throw new Exception("Malformed BVH in second line after '" + line + "'. Does not start with 'OFFSET'");}

            line = reader.ReadLine().Trim();
            string[] contents = line.Split(' ');
            if (contents[0] != "CHANNELS"){
                throw new Exception("Malformed BVH in third line after '" + line + "'. Does not start with 'CHANNELS'");}

            int channelCount = ParseFast(contents[1]);
            if (channelCount + 2 != contents.Length){
                throw new Exception("Channel count mismatch for joint '" + joint.name + "'. Expected " + channelCount.ToString() + " channels, but detected " + (contents.Length - 2).ToString() + ".");}

            joint.channelCount = channelCount;
            char[] channelOrder = new char[joint.channelCount];
            for(int i = 0; i < joint.channelCount; ++i)
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
    }
}
