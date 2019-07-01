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
        public int currentBVH = 0;

        [SerializeField]
        private Reader reader;

        [SerializeField]
        private Player player;

        [SerializeField]
        private CommandControl commandControl;

        IDictionary<string, GameObject> locations = new Dictionary<string, GameObject>();

        private IEnumerator coroutine;

        private List<BVH> allLoadedBVHs = new List<BVH>();
        private BVH bvh;

        private List<BVH> EDEICTICBVHs = new List<BVH>();
        private List<BVH> SIZEBVHs = new List<BVH>();
        Vector3 pos = new Vector3(0, 0, 0);

        public List<BVH> EDEICT
        {
            get { return EDEICTICBVHs; }
        }

        public List<BVH> SIZE
        {
            get { return SIZEBVHs; }
        }

        public IDictionary<string, GameObject> LOC
        {
            get { return locations; }
        }

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
            //frameCountText = GameObject.Find("Frame Count").GetComponent<Text>();
            //model = GameObject.Find("ChrRachelDanceAvatar");

            //Load different type of BVH
            EDEICTICBVHs = reader.EDEICTICBVH;
            SIZEBVHs = reader.SIZEBVH;
            allLoadedBVHs = reader.LoadedBVH;

            bvh = allLoadedBVHs[currentBVH];
            player.Play(bvh);
        }

        // Update is called once per frame
        void Update()
        {
            if (commandControl.NewBVH)
            {
                bvh = commandControl.CurrentBVH;
                player.Play(bvh);
            }
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

