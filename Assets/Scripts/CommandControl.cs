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
    public class CommandControl : MonoBehaviour
    {
        [SerializeField]
        private BVHPicker bvhPicker;

        private int current_command_type;
        private int prev_command_type;
        private List<string> current_targets;
        private List<string> prev_targets;
        private bool newBVH = false;
        private BVH currentBVH;

        const string CommandTextFile = "/Users/hsn/Desktop/Spatial_Gesture/Parser/command.txt";

        public bool NewBVH
        {
            get { return newBVH; }
        }

        public BVH CurrentBVH
        {
            get { return currentBVH; }
        }

        // Use this for initialization
        void Start()
        {
            prev_command_type = current_command_type = checkCommand(CommandTextFile);
            prev_targets = getTargets(CommandTextFile);
        }

        // Update is called once per frame
        void Update()
        {
            current_targets = getTargets(CommandTextFile);
            current_command_type = checkCommand(CommandTextFile);

            if (current_command_type != prev_command_type || !current_targets.SequenceEqual(prev_targets))
            {
                newBVH = true;
                prev_command_type = current_command_type;
                prev_targets = current_targets;
                currentBVH = bvhPicker.pickBVHList(current_command_type, current_targets);
            } else
            {
                newBVH = false;
            }
        }

        //function for reading the command
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
    }
}
