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
    public class BVHManager : MonoBehaviour
    {
        // frame rate
        private Text frameCountText;

        // the model itself
        public GameObject model;

        // BVH file directory
        const string CommandTextFile = "/Users/hsn/Desktop/Spatial_Gesture/Parser/command.txt";
        // Total room number
        const int roomnum = 6;

        Reader reader = new Reader();

        Processor processor = new Processor();

        // store the location of all the room
        IDictionary<string, GameObject> locations = new Dictionary<string, GameObject>();

        // Use this for initialization
        void Start()
        {
            frameCountText = GameObject.Find("Frame Count").GetComponent<Text>();
            model = GameObject.Find("ChrRachelDanceAvatar");
        }

        // Update is called once per frame
        void Update()
        {

        }

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