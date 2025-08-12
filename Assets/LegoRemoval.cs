using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using KModkit;
using Events;

public class LegoRemoval : MonoBehaviour
{

    public class ModSettingsJSON { public int countdownTime; public string note; }
    public KMAudio Audio; public KMBombInfo Info; public KMBombModule Module; public KMModSettings modSettings;

    public Material[] colorMats;
    public Material glow;
    public MeshRenderer[] bricks;

    public GameObject[] grid; public GameObject[] brickParent; public GameObject baseBricks; public MeshRenderer[] baseplate;
    public KMSelectable theModule; public KMSelectable[] brickSelect; public KMSelectable buttonSelect;
    public TextMesh[] screenText;

    public BoxCollider buttonBox; public BoxCollider[] screenBox;

    //public delegate void KMGameplayLightChange(bool on);
    //public KMGameplayLightChange OnLightChange;
    //UnityEngine.Object ceilingLight = FindObjectOfType(ReflectionHelper.FindGameType("CeilingLight"));
    //bool isLightOn = ceilingLight && ceilingLight.GetValue<int>("currentState") == 0;

    //public delegate void Action(KMHighlightable highBri);


    //List<int> plateColors = new List<int> { 3, 4, 5, 8,    4, 3, 8, 5 };
    List<int> plateColors = new List<int> { 3, 4, 5, 8,    4, 5, 4, 5  };

    List<int> rotations = new List<int> { 0, 90, 180, 270 };
    List<List<Bounds>> structureBounds = new List<List<Bounds>>();
    List<GameObject> glowyBricks = new List<GameObject>();

    
    public static bool LightsOn { get; set; }

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved = false;

    void Awake()
    {
        moduleId = moduleIdCounter++;

        int brickNum = UnityEngine.Random.Range(49, 71);        // BRICK NUMBERS
        int layerNum = UnityEngine.Random.Range(7, 11);         // LAYERS
        //Debug.Log(brickNum + "  " + layerNum);

        int plateColor = UnityEngine.Random.Range(0, 4);
        foreach (MeshRenderer elem in baseplate) { elem.material = colorMats[plateColors[plateColor]]; }
        buttonSelect.gameObject.GetComponent<MeshRenderer>().material = colorMats[plateColors[plateColor + 4]];

        int checker = brickNum;
        int[] layerCounts = new int[layerNum];
        int baseBriLayNum = (int)Math.Floor((double)brickNum / layerNum);

        for (int i7 = 0; i7 < layerNum; i7++)       { layerCounts[i7] = baseBriLayNum; checker -= baseBriLayNum; }
        for (int i8 = 0; i8 < checker; i8++)        { layerCounts[UnityEngine.Random.Range(0, layerNum)] += 1; }
        for (int i9 = 0; i9 < baseBriLayNum; i9++)  { layerCounts[UnityEngine.Random.Range(0, layerNum)] += 1; layerCounts[UnityEngine.Random.Range(0, layerNum)] -= 1; }
        Array.Sort(layerCounts); Array.Reverse(layerCounts);


        string aaaa = ""; foreach (int fucker in layerCounts) { aaaa += " " + fucker; }; Debug.LogFormat("[LEGO Removal #{0}] Struct:{1}, NumBricks: ~{2}", moduleId, aaaa, brickNum);



        int brickId = 0;
        for (int layer = 0; layer < layerNum; layer++)      // iterate through layers, generate structure
        {
            List<Bounds> brickBounds = new List<Bounds>();
            int failsafe = 0;

            GameObject newLayer = new GameObject();
            newLayer.name = layer.ToString();
            newLayer.transform.parent = baseBricks.transform;


            if (layer == 0) { brickBounds.Add(screenBox[0].bounds); brickBounds.Add(screenBox[1].bounds); brickBounds.Add(screenBox[2].bounds); brickBounds.Add(buttonBox.bounds); }

            for (int local_brickId = 0; /*failsafe != 14 */local_brickId < layerCounts[layer]; local_brickId++)     // iterates through IntPar
            {
                bool Affirmative = false;
                failsafe = 0;

                while (Affirmative == false)
                {
                    // generate size & rotation of brick
                    int size = 0;
                    switch (layer) { // limits smaller bricks from spawning on lower layers
                        case 0:     size = (int)Math.Round((double)UnityEngine.Random.Range(3, 12)); break;
                        case 1:     size = (int)Math.Round((double)UnityEngine.Random.Range(2, 12)); break;
                        case 2:     size = (int)Math.Round((double)UnityEngine.Random.Range(1, 12)); break;
                        default:    size = (int)Math.Round((double)UnityEngine.Random.Range(0, 12)); break; }
                    int rot = rotations[(int)Math.Round((double)UnityEngine.Random.Range(0, 4))];

                    // generate spot to place brick on
                    int width = (int)Char.GetNumericValue(bricks[size].name[0]) - 1;
                    int length = (int)Char.GetNumericValue(bricks[size].name[2]) - 1;
                    int row = 0; int col = 0;                                             int lowI = 2; int upI = 14; // 3,13 = 10x10   2,14 = 12x12,  1,15 = 14x14,   0,16 = 16x16 
                    switch (rot) { //limit where bricks can be placed, based on rotation
                        case 0:     row = UnityEngine.Random.Range(lowI, upI - width);  col = UnityEngine.Random.Range(lowI, upI - length); break;
                        case 90:    row = UnityEngine.Random.Range(lowI, upI - length); col = UnityEngine.Random.Range(lowI + width, upI); break;
                        case 180:   row = UnityEngine.Random.Range(lowI + width, upI);  col = UnityEngine.Random.Range(lowI + length, upI); break;
                        case 270:   row = UnityEngine.Random.Range(lowI + length, upI); col = UnityEngine.Random.Range(lowI, upI - width); break; }


                    /*
                    Vector3 point; point = (grid[row].transform.Find(col.ToString())).transform.position;
                    point[1] += 0.0135f * layer;
                    GameObject newBrick; newBrick = Instantiate(brickParent[size], point, Quaternion.Euler(0, rot, 0), baseBricks.transform);
                    newBrick.transform.localScale = new Vector3(newBrick.transform.localScale[0] * 0.01125f, newBrick.transform.localScale[1] * 0.01125f, newBrick.transform.localScale[2] * 0.01125f);

                    var child = newBrick.transform.GetChild(0); child.transform.parent = baseBricks.transform; Destroy(newBrick);
                    Bounds brickCollider = child.GetComponent<BoxCollider>().bounds;
                    */

                    /*
                    Vector3 point; point = (grid[row].transform.Find(col.ToString())).transform.position;
                    point[1] += 0.01355f * layer;
                    GameObject newBrick; newBrick = Instantiate(brickParent[size], Vector3.zero, Quaternion.Euler(0, rot, 0));
                    newBrick.transform.SetParent(baseBricks.transform, false);
                    newBrick.transform.localPosition = new Vector3(point[0] * 88.1725f, (point[1] * 88.1725f) - 1.6f, point[2] * 88.1725f);

                    var child = newBrick.transform.GetChild(0); child.transform.parent = baseBricks.transform; Destroy(newBrick);
                    Bounds brickCollider = child.GetComponent<BoxCollider>().bounds;

                    Debug.Log(point[0] + " " + point[1] + " " + point[2]);
                    */

                    
                    // generate and place brick's box collider on module
                    Vector3 point; point = (grid[row].transform.Find(col.ToString())).transform.position; 
                    //point[1] += 0.0135f * layer; //point[1] += baseBricks.transform.up * 0.0135f ;
                    //GameObject newBrick; newBrick = Instantiate(brickParent[size], point, Quaternion.Euler(0, rot, 0), baseBricks.transform);
                    //GameObject newBrick; newBrick = Instantiate(brickParent[size], point[1] * baseBricks.transform.up, Quaternion.Euler(0, rot, 0), baseBricks.transform);
                    GameObject newBrick; newBrick = Instantiate(brickParent[size], baseBricks.transform.position, Quaternion.Euler(0, rot, 0));
                    //newBrick.transform.localScale = new Vector3(newBrick.transform.localScale[0] * 0.01125f, newBrick.transform.localScale[1] * 0.01125f, newBrick.transform.localScale[2] * 0.01125f);
                    newBrick.transform.SetParent(baseBricks.transform, false);
                    newBrick.transform.localPosition = baseBricks.transform.up * layer;


                    var child = newBrick.transform.GetChild(0); child.transform.parent = baseBricks.transform; Destroy(newBrick);
                    Bounds brickCollider = child.GetComponent<BoxCollider>().bounds;
                    




                    // checks if it's a valid placement, destroys if not
                    if (PlaceableHere(brickBounds, brickCollider, layer))
                    {

                        brickBounds.Add(brickCollider); child.name = brickId.ToString(); brickSelect[brickId] = child.GetComponent<KMSelectable>(); brickId++;
                        child.transform.parent = newLayer.transform;

                        int color = (int)Math.Round((double)UnityEngine.Random.Range(0, 58)); 
                        child.GetComponent<MeshRenderer>().material = colorMats[color];
                        child.GetComponent<MeshRenderer>().enabled = true;
                        if (color == 38) { glowyBricks.Add(child.gameObject); }

                        Affirmative = true;
                    }
                    else { Destroy(child.gameObject); Affirmative = false; }

                    // makes sure module doesn't crash
                    failsafe++;
                    if (failsafe == 10) { /*Debug.Log(moduleId + ": failed at " + layer);*/ Affirmative = true; }
                }
            }
            structureBounds.Add(brickBounds);
        } //iterate through layers, generate structure

       
       


        foreach (KMSelectable brick in brickSelect)
        {
            if (brick != null)
            {
                try
                {
                    GameObject curLay = brick.gameObject.transform.parent.gameObject; int b = 0;
                    foreach (Bounds aboveBrick in structureBounds[Int32.Parse(curLay.name) + 1])
                    {
                        if (brick.gameObject.GetComponent<BoxCollider>().bounds.Intersects(aboveBrick))
                            { b++; }
                    }

                    foreach (Bounds aboveBrick113 in structureBounds[Int32.Parse(curLay.name) + 3])
                    {
                        if (brick.gameObject.GetComponent<BoxCollider>().bounds.Intersects(aboveBrick113))
                            { b++; }
                    }

                    if (b == 0)
                    {
                        theModule.Children[Int32.Parse(brick.gameObject.name) + 1] = brick;
                        KMSelectable pressedBrick = brick; brick.OnInteract += delegate () { PressBrick(pressedBrick); return false; };
                        //Action OnHighlight = ColorblindHelp(brick.Highlight);
                        //Debug.Log(brick.name + " got nothin blocking");
                    }
                    //else { Debug.Log(brick.name + " is BLOCKED"); }
                }
                catch // top layer bricks
                {
                    theModule.Children[Int32.Parse(brick.gameObject.name) + 1] = brick;
                    KMSelectable pressedBrick = brick; brick.OnInteract += delegate () { PressBrick(pressedBrick); return false; };
                    //Debug.Log(brick.name + " is on top layer");
                }
            }
        }
        buttonSelect.OnInteract += delegate () { PressButton(); return false; };
        theModule.UpdateChildrenProperly();
    }

    void Start()
    {
        
    }

    //void ColorblindHelp(KMHighlightable highOver)
    //{

    //}

    void PressButton()
    {
        Debug.Log("PRESSED THE BUTTON");

        //foreach (KMSelectable sel in theModule.Children)
        //{

        //}

    }

    void PressBrick(KMSelectable theBrick)
    {
        Debug.Log("PRESSED A BRICK");
    }

    bool PlaceableHere(List<Bounds> CurLayBounds, Bounds brickB, int lay)
    {
        int a = 0;
        Bounds b113 = brickParent[3].transform.GetChild(0).GetComponent<BoxCollider>().bounds;

        foreach (Bounds item2 in CurLayBounds)
        { if (brickB.Intersects(item2)) { return false; } }         // is colliding with brick on current layer?

        if (lay > 0) {

            foreach (Bounds item3 in structureBounds[lay - 1]) {
                if (brickB.Intersects(item3)) {
                    if (Mathf.Approximately(item3.size.y,b113.size.y)) {               // is colliding with 1x1x3? (layer - 1)
                        return false;
                    } else { a++; } } }        // how many bricks is this one placed on?

            if (lay > 1) {
                foreach (Bounds item4 in structureBounds[lay - 2]) {
                    if (brickB.Intersects(item4)) {
                        if (Mathf.Approximately(item4.size.y, b113.size.y)) {       // is colliding with 1x1x3? (layer - 2)
                            return false; } } } }

            if (lay > 2) {
                foreach (Bounds item5 in structureBounds[lay - 3]) {
                    if (brickB.Intersects(item5)) {
                        if (Mathf.Approximately(item5.size.y, b113.size.y)) {       // is colliding with 1x1x3? (layer - 3)
                            a++; } } } }

            if (a > 0) { return true; } // is placed on another brick? (non-floating)
            else { return false; } }

        return true;
    }
    
    /*
    private void OnEnable()
    {
        EnvironmentEvents.OnLightsOn += OnLightsOn;
        EnvironmentEvents.OnLightsOff += OnLightsOff;
    }
    private void OnDisable()
    {
        EnvironmentEvents.OnLightsOn -= OnLightsOn;
        EnvironmentEvents.OnLightsOff -= OnLightsOff;

        
    }
    */

    public void glowBriChange(bool on)
    {
        if (on) {
            Debug.Log("LIGHTS ARE ON");
            foreach (GameObject glowy in glowyBricks)
            { glowy.GetComponent<MeshRenderer>().material = colorMats[38]; }
        }
        else
        {
            Debug.Log("THE LIGHTS ARE OFF");
            foreach (GameObject glowy in glowyBricks)
            { glowy.GetComponent<MeshRenderer>().material = glow; }
        }
    }

    

    void Update()
    {
        
    }
}
