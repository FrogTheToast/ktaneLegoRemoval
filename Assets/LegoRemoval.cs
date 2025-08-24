using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using KModkit;
using Events;
using System.Reflection;

public class LegoRemoval : MonoBehaviour
{

    public class ModSettingsJSON { public int countdownTime; public string note; }
    public KMAudio Audio; public KMBombInfo bombInf; public KMBombModule Module; public KMModSettings modSettings;
    

    public Material[] colorMats;
    public Material glow;

    public GameObject[] bricks;
    public GameObject[] grid;
    public GameObject baseBricks;

    public MeshRenderer[] baseplate;
    public MeshRenderer[] screenMesh;

    public KMSelectable theModule;
    public KMSelectable[] brickSelect;
    public KMSelectable buttonSelect;

    public TextMesh[] screenText;

    public BoxCollider[] modElemBoxes;
    public BoxCollider boxOfStupid;

    //UnityEngine.Object ceilingLight = FindObjectOfType(ReflectionHelper.FindGameType("CeilingLight"));
    

                                                // 3r 4b 5y 8g   40r 24b 27y 37g
    List<int> plateColors = new List<int> { 3, 4, 5, 8,     4, 5, 4, 5,     24, 27, 24, 27 };
    //List<int> plateColors = new List<int> { 40, 24, 27, 37,     4, 5, 4, 5,     24, 27, 24, 27 };

    List<int> rotations = new List<int> { 0, 90, 180, 270 };
    //List<int> rotations = new List<int> { 180, 270, 360, 450 };


    Dictionary<int, Bounds> structureBounds = new Dictionary<int, Bounds>();
    List<List<GameObject>> structure = new List<List<GameObject>>();
    public List<GameObject> glowyBricks = new List<GameObject>();
    List<KMSelectable> children = new List<KMSelectable>();
    List<GameObject> che = new List<GameObject>();

    //base and direction
    List<int> baseOrder; List<int> direction = new List<int>(); GameObject incBrick;

    //foundation
    List<GameObject> checker = new List<GameObject>();
    List<GameObject> transfer = new List<GameObject>();
    List<KMSelectable> foundList = new List<KMSelectable>();

    //supports
    int supCount = 0;
    List<List<List<int>>> coordList = new List<List<List<int>>>();
    List<KMSelectable> suppList = new List<KMSelectable>();

    //button
    int count = 0; float t = 0; int stupidDumb = 0; int inc = 0; int s = 0;
    Color norm = Color.clear;   Color norm1 = Color.clear;      
    Color high = Color.cyan;    Color high1 = Color.magenta;    Color bad = Color.red;

    private bool buttonPress = false;
    private bool foundHigh = false;
    private bool badBrick = false;
    private bool valid = false;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved = false;
    private bool isLightOn = false;
    
    

    void Awake()
    {
        moduleId = moduleIdCounter++;
        //bool isLightOn = ceilingLight && ceilingLight.GetValue<int>("currentState") == 0;

        //screenText[0].text = moduleId.ToString();

        bool onFrontFace = true;
        GameObject test; test = Instantiate(bricks[2], baseBricks.transform.position, Quaternion.Euler(0,0,0), baseBricks.transform);
        if (test.GetComponentInChildren<BoxCollider>().bounds.Intersects(boxOfStupid.bounds)) { onFrontFace = false; } Destroy(test);
        
        int brickNum = UnityEngine.Random.Range(57, 72);        // BRICK NUMBERS    49,71  62,72    42,70
        int layerNum = UnityEngine.Random.Range(7, 11);         // LAYERS
        //Debug.Log(brickNum + "  " + layerNum);

        //brickNum = 30;

        int plateColor = UnityEngine.Random.Range(0, 4);
        foreach (MeshRenderer elem in baseplate) { elem.material = colorMats[plateColors[plateColor]]; }
        foreach (MeshRenderer elem1 in screenMesh) { elem1.material = colorMats[plateColors[plateColor + 8]]; }
        buttonSelect.gameObject.GetComponent<MeshRenderer>().material = colorMats[plateColors[plateColor + 4]];

        int checker = brickNum;
        int[] layerCounts = new int[layerNum];
        int baseBriLayNum = (int)Math.Floor((double)brickNum / layerNum);
        for (int i7 = 0; i7 < layerNum; i7++)       { layerCounts[i7] = baseBriLayNum; checker -= baseBriLayNum; }
        for (int i8 = 0; i8 < checker; i8++)        { layerCounts[UnityEngine.Random.Range(0, layerNum)] += 1; }
        for (int i9 = 0; i9 < baseBriLayNum; i9++)  { layerCounts[UnityEngine.Random.Range(0, layerNum)] += 1; layerCounts[UnityEngine.Random.Range(0, layerNum)] -= 1; }
        Array.Sort(layerCounts); Array.Reverse(layerCounts);


        int brickId = 0;
        for (int layer = 0; layer < layerNum; layer++)      // iterate through layers, generate structure
        {
            List<GameObject> layBricks = new List<GameObject>();
            int failsafe = 0;

            for (int i20 = 0; /*failsafe != 14 */i20 < layerCounts[layer]; i20++)     // iterates through IntPar
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
                    int rotIndex = (int)Math.Round((double)UnityEngine.Random.Range(0, 4));
                    int rot = rotations[rotIndex];


                    // generate spot to place brick on
                    int width = (int)Char.GetNumericValue(bricks[size].name[0]) - 1;
                    int length = (int)Char.GetNumericValue(bricks[size].name[2]) - 1;
                    int row = 0; int col = 0;  int lowI = 1; int upI = 15; // 3,13 = 10x10   2,14 = 12x12,  1,15 = 14x14,   0,16 = 16x16 
                    switch (rot) { //limit where bricks can be placed based on rotation
                        case 0:                                         row = UnityEngine.Random.Range(lowI, upI - width);  col = UnityEngine.Random.Range(lowI, upI - length); break;
                        case 90:    if (!onFrontFace) { rot += 180; }   row = UnityEngine.Random.Range(lowI, upI - length); col = UnityEngine.Random.Range(lowI + width, upI); break;
                        case 180:                                       row = UnityEngine.Random.Range(lowI + width, upI);  col = UnityEngine.Random.Range(lowI + length, upI); break;
                        case 270:   if (!onFrontFace) { rot += 180; }   row = UnityEngine.Random.Range(lowI + length, upI); col = UnityEngine.Random.Range(lowI, upI - width); break; }

                    // 0, 0     90, 180     180, 0      270, 180

                    int alig = 0; if (!onFrontFace) { alig = 180; }
                    Vector3 point; point = grid[row].transform.Find(col.ToString()).transform.position;
                    GameObject newBrick;
                    newBrick = Instantiate(bricks[size], point, Quaternion.Euler(0, rot, alig), baseBricks.transform);

                    var position = newBrick.transform.localPosition; position.y = 1.2f * layer;
                    newBrick.transform.localPosition = position;

                    var child = newBrick.transform.GetChild(0);
                    child.transform.parent = grid[row].transform.Find(col.ToString()).transform;
                    Destroy(newBrick);

                    Bounds newBriBounds = child.GetComponent<BoxCollider>().bounds;
                    
                    if (PlaceableHere(layBricks, newBriBounds, layer))
                    {

                        
                        layBricks.Add(child.gameObject);
                        structureBounds.Add(brickId, newBriBounds);


                        if (layer < 10)
                        {
                            if (brickId < 10)
                            {
                                child.name += "_0" + brickId.ToString() + "_0" + layer.ToString() + "_" + rotIndex.ToString();
                            }

                            else
                            {
                                child.name += "_" + brickId.ToString() + "_0" + layer.ToString() + "_" + rotIndex.ToString();
                            }
                        }
                        else
                        {
                            if (brickId < 10)
                            {
                                child.name += "_0" + brickId.ToString() + "_" + layer.ToString() + "_" + rotIndex.ToString();
                            }

                            else
                            {
                                child.name += "_" + brickId.ToString() + "_" + layer.ToString() + "_" + rotIndex.ToString();
                            }
                        }
                        child.GetComponent<KMSelectable>().Parent = theModule;

                        brickId++;
                        

                        int color = (int)Math.Round((double)UnityEngine.Random.Range(0, 58)); 
                        child.GetComponent<MeshRenderer>().material = colorMats[color];
                        child.GetComponent<MeshRenderer>().enabled = true;
                        if (color == 38) { glowyBricks.Add(child.gameObject); }

                        Affirmative = true;
                    }
                    else { Destroy(child.gameObject); Affirmative = false; }

                    // makes sure module doesn't crash
                    failsafe++;
                    if (failsafe == 10) { /*Debug.Log(moduleId + ": failed at " + layer);*/
                    Affirmative = true; }
                }
            }
            structure.Add(layBricks);
            

        } //iterate through layers, generate structure

        
        for (int l = 0; l < structure.Count; l++)
        {
            foreach (GameObject briBounds in structure[l]) {
                try {
                    int b = 0;
                    foreach (GameObject aboveBrick in structure[l + 1]) {
                        if (Bo(briBounds).Intersects(Bo(aboveBrick))) { b++; }
                    }

                    
                    if (briBounds.name.Contains("1x1x3")) {
                        try {
                            foreach (GameObject aboveBrick in structure[l + 3]) {
                                if (Bo(briBounds).Intersects(Bo(aboveBrick))) { b++; }
                            }
                        }
                        catch {
                            children.Add(briBounds.GetComponent<KMSelectable>());
                            //Debug.Log(structure[l][brickId].name + " " + l + ": is 1x1x3" + "     " + briBounds.size.y);
                        }
                    }

                    if (b == 0) {
                        children.Add(briBounds.GetComponent<KMSelectable>());
                        //Debug.Log(structure[l][brickId].name + " " + l + ": is good :)" + "     " + briBounds.size.y);
                    }
                    //else { Debug.Log(structure[l][brickId].name + " " + l + ": is BLOCKED" + "     " + briBounds.size.y); }
                }
                catch {
                    children.Add(briBounds.GetComponent<KMSelectable>());
                    //Debug.Log(structure[l][brickId].name + " " + l + ": on top layer" + "     " + briBounds.size.y);
                }
            }
        }

        //gameInfo.OnLightsChange += glowBriChange;

        foreach (KMSelectable brick in children){
            DelegateBrick(brick); }

        List<KMSelectable> ch = new List<KMSelectable>(); ch.AddRange(children); ch.Add(buttonSelect);
        buttonSelect.Parent = theModule;
        buttonSelect.OnInteract += delegate () 
        {
            if (!buttonPress) { buttonPress = true; }
            else {
                foreach (KMSelectable sel in children) {
                    sel.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.clear); }
                foreach (KMSelectable sel in foundList) {
                    sel.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.clear); }
                t = 0;
                norm = Color.clear; norm1 = Color.clear;
                high = Color.cyan;  high1 = Color.magenta;
                foundList.Clear(); foundHigh = false; buttonPress = false; }
            return false;
        };

        theModule.Children = ch.ToArray();
        theModule.UpdateChildrenProperly();

    }

    bool PlaceableHere(List<GameObject> CurLayBounds, Bounds briB, int lay)
    {
        int a = 0;
        
        foreach (GameObject item in CurLayBounds) {
            if (briB.Intersects(Bo(item))) {                  // is colliding with brick on current layer?
                //Debug.Log("curLay?");
                return false; } }    
        foreach (BoxCollider item in modElemBoxes) {
            if (briB.Intersects(item.bounds)) {       // is colliding with screen/button?
                //Debug.Log("screen/button?");
                return false; } }

        //a += structureBounds[lay + 1].Count( aboveBrick => brickB.Intersects(item));
        
        if (lay > 0) {
            
            //a += structureBounds[lay + 1].Count(item => brickB.Intersects(item));
            foreach (GameObject item1 in structure[lay - 1]) {
                //Debug.Log(item.size);
                if (briB.Intersects(Bo(item1))) {
                    if (item1.name.Contains("1x1x3")) {          // is colliding with 1x1x3? (layer - 1)
                        //Debug.Log("1x1x3 -1?");
                        return false;
                    } else { a++; } } }                                         // how many bricks is this one placed on?

            if (lay > 1) {
                foreach (GameObject item2 in structure[lay - 2]) {
                    if (briB.Intersects(Bo(item2))) {
                        if (item2.name.Contains("1x1x3")) {      // is colliding with 1x1x3? (layer - 2)
                            //Debug.Log("1x1x3 -2?");
                            return false; } } } }

            if (lay > 2) {
                foreach (GameObject item3 in structure[lay - 3]) {
                    if (briB.Intersects(Bo(item3))) {
                        if (item3.name.Contains("1x1x3")) {      // is colliding with 1x1x3? (layer - 3)
                            a++; } } } }

            if (a > 0) { //Debug.Log("nonfloating!");
                return true; }          // is placed on another brick? (non-floating)
            else { return false; } }

        //Debug.Log("none!"); 
        return true;
    }

    void Start()
    {
        baseOrder = bombInf.GetSerialNumberNumbers().ToList();
        List<char> direc = bombInf.GetSerialNumberLetters().ToList();

        for (int ugh = 0; ugh < baseOrder.Count; ugh++) { // calculate Base Sequence
            baseOrder[ugh] = (baseOrder[ugh] + ConvertObj(structure).Where(bri => bri.name.Contains("1x1x1_peg")).Count()) % 5; }
        for (int hhh = 0; hhh < direc.Count; hhh++) { decimal oh = direc[hhh] - 55; // calculate Direction
            direction.Add((decimal.ToInt32(oh) + ConvertObj(structure).Where(bri => bri.name.Contains("1x1x1_stud")).Count()) % 2); }
        foreach (GameObject bri in structure.Last()) {  // calculate Increment Brick
            stupidDumb += SizeGet(bri.GetComponent<KMSelectable>()); }
            incBrick = bricks[stupidDumb % 12]; stupidDumb = 0;

        string b = ""; string d = ""; foreach (int num in baseOrder) { b += num; } foreach (int num in direction) { d += num;  }
        foreach (List<GameObject> dumb in structure) { count += dumb.Count; }

        Debug.LogFormat("[LEGO Removal #{0}] NumBricks: {1}, B: {2}, D: {3}, I: {4}", moduleId, count, b, d, incBrick.name);
    }

    List<List<int>> CoordsGet(GameObject orig)
    {
        List<List<int>> coords = new List<List<int>>();
        int col = Convert.ToInt32(Int32.Parse(orig.transform.parent.name));
        int row = Convert.ToInt32(Int32.Parse(orig.transform.parent.transform.parent.name));
        int width = Convert.ToInt32(Char.GetNumericValue(orig.name[0])) - 1;
        int length = Convert.ToInt32(Char.GetNumericValue(orig.name[2])) - 1;
        
        List<int> tl = new List<int>();
        List<int> br = new List<int>();
        
        switch (Convert.ToInt32(Char.GetNumericValue(orig.name[orig.name.Length - 1])) ) {
            case 0: // tl
                {
                    //opRow += width; opCol += length;

                    tl.AddRange(new List<int> { row, col });
                    br.AddRange(new List<int> { row + width, col + length});
                    break; }
            case 1: // tr
                {
                    //opRow += length; opCol -= width;

                    tl.AddRange(new List<int> { row, col - width });
                    br.AddRange(new List<int> { row + length, col });
                    break; }
            case 2: // br
                {
                    //opRow -= width; opCol -= length;

                    tl.AddRange(new List<int> { row - width, col - length });
                    br.AddRange(new List<int> { row, col });
                    break; }
            case 3: // bl
                {
                    //opRow -= length; opCol += width; int i = col;

                    tl.AddRange(new List<int> { row - length, col });
                    br.AddRange(new List<int> { row, col + width });
                    break; } }

        //Debug.Log(tl[0] + "," + tl[1] + "  " + br[0] + "," + br[1] + "            " + checker.Count + " " + row + "," + col);

        int r = tl[0];
        for (int c = tl[1]; c != (br[1] + 1); c++)
        {
            //Debug.Log(orig.name + ": " + r + "," + c);
            coords.Add(new List<int> { r, c });

            if (c == br[1]) { c = tl[1] - 1; r++;}
            if (r > br[0]) { c = br[1]; }
        }

        return coords;
    }
    List<GameObject> ConvertObj(List<List<GameObject>> stru)
    {
        List<GameObject> con = new List<GameObject>();
        foreach (List<GameObject> la in stru) { foreach (GameObject bri in la) { con.Add(bri.gameObject); } }
        return con;
    }
 
    Bounds Bo(GameObject ga)
    {
        return structureBounds[Convert.ToInt32(
            (Char.GetNumericValue(ga.name[ga.name.Length - 7]) * 10)
            + Char.GetNumericValue(ga.name[ga.name.Length - 6]))];
    }
    

    int ColorGet(KMSelectable b)
    {
        return Convert.ToInt32(
              (Char.GetNumericValue(b.GetComponent<MeshRenderer>().material.name[0]) * 100) 
            + (Char.GetNumericValue(b.GetComponent<MeshRenderer>().material.name[1]) * 10) 
            +  Char.GetNumericValue(b.GetComponent<MeshRenderer>().material.name[2]));
    }
    int SizeGet(KMSelectable b)
    {
        return Convert.ToInt32(
              Char.GetNumericValue(b.gameObject.name[0]) 
            * Char.GetNumericValue(b.gameObject.name[2]) 
            * Char.GetNumericValue(b.gameObject.name[4]));
    }
    int FoundationGet(KMSelectable b)
    {
        int lowLay = LayerGet(b);
        checker.Add( b.gameObject );
        
        while (lowLay > 0) {
            lowLay--;
            foreach (GameObject upperBrick in checker) {
                foreach (GameObject lowerBrick in structure[lowLay]) {
                    if (Bo(upperBrick).Intersects(Bo(lowerBrick))) {
                        if (!checker.Contains(lowerBrick)) {
                            //Debug.Log(lowerBrick.size.x + ", " + lowerBrick.size.y + ", " + lowerBrick.size.z);
                            transfer.Add(lowerBrick);
                        } } } }
            checker.AddRange(transfer); transfer.Clear(); }

        int found = checker.Count(); checker.Clear(); found--;  return found;
    }
    int SupportsGet(KMSelectable b)
    {
        int lowLay = LayerGet(b);
        if (Char.GetNumericValue(b.gameObject.name[0]) == 1 && Char.GetNumericValue(b.gameObject.name[2]) == 1) { return 1; }
        if (lowLay == 0) { return SizeGet(b); }

        checker.Add( b.gameObject );
        int b113 = lowLay - 3;

        while (lowLay > 0) {
            lowLay--;
            foreach (GameObject lowerBrick in structure[lowLay]) {
                if (Bo(lowerBrick).Intersects(Bo(checker[0]))) {
                    transfer.Add(lowerBrick); }
            } checker.AddRange(transfer); transfer.Clear();
            if (lowLay == b113) { break; } }

        foreach (GameObject uhhh in checker) { coordList.Add(CoordsGet(uhhh)); }
        supCount = 0;
        foreach (List<int> co in coordList[0]) {
            for (int i21 = 1; i21 < coordList.Count; i21++) {
                if (coordList[i21].Any(c => c[0] == co[0] && c[1] == co[1])) {
                    supCount++; } } }

        coordList.Clear(); checker.Clear();
        return supCount;
    }
    int LayerGet(KMSelectable b)
    {
        return Convert.ToInt32(
            (Char.GetNumericValue(b.name[b.name.Length - 4]) * 10)
            + Char.GetNumericValue(b.name[b.name.Length - 3]));
    }

    void PressBrick(KMSelectable theBrick)
    {
        if (!buttonPress)
        {
            valid = false;
            switch (baseOrder[inc % baseOrder.Count])
            {
                // color
                case 0: { 
                        var colorOrder = children
                                        .GroupBy(bri => ColorGet(bri)).ToArray()
                                        .OrderBy(grp => grp.Key).ToArray();
                        switch (direction[inc % direction.Count]) {
                            case 1: {
                                    if (colorOrder.ElementAtOrDefault(0).Key == ColorGet(theBrick))
                                    { valid = true; } else { valid = false; } break; }
                            case 0: {
                                    if (colorOrder.ElementAtOrDefault(colorOrder.Count() - 1).Key == ColorGet(theBrick))
                                    { valid = true; } else { valid = false; } break; } }
                        break; }
                // size
                case 1: { 
                        var sizeOrder = children
                                        .GroupBy(bri => SizeGet(bri)).ToArray()
                                        .OrderBy(grp => grp.Key).ToArray();
                        switch (direction[inc % direction.Count]) {
                            case 1: {
                                    if (sizeOrder.ElementAtOrDefault(0).Key == SizeGet(theBrick))
                                    { valid = true; } else { valid = false; } break; }
                            case 0: {
                                    if (sizeOrder.ElementAtOrDefault(sizeOrder.Count() - 1).Key == SizeGet(theBrick))
                                    { valid = true; } else { valid = false; } break; } }
                        break; }
                // foundation
                case 2: {
                        var foundOrder = children
                                        .GroupBy(bri => FoundationGet(bri)).ToArray()
                                        .OrderBy(grp => grp.Key).ToArray();
                        switch (direction[inc % direction.Count]) {
                            case 1: {
                                    if (foundOrder.ElementAtOrDefault(0).Key == FoundationGet(theBrick))
                                    { valid = true; } else { valid = false; } break; }
                            case 0: {
                                    if (foundOrder.ElementAtOrDefault(foundOrder.Count() - 1).Key == FoundationGet(theBrick))
                                    { valid = true; } else { valid = false; } break; } }
                        break; }
                // supports
                case 3: {
                        var suppOrder = children
                                        .GroupBy(bri => SupportsGet(bri)).ToArray()
                                        .OrderBy(grp => grp.Key).ToArray();
                        switch (direction[inc % direction.Count]) {
                            case 1: {
                                    if (suppOrder.ElementAtOrDefault(0).Key == SupportsGet(theBrick))
                                    { valid = true; } else { valid = false; } break; }
                            case 0: {
                                    if (suppOrder.ElementAtOrDefault(suppOrder.Count() - 1).Key == SupportsGet(theBrick))
                                    { valid = true; } else { valid = false; } break; } }
                        break; }
                // layers
                case 4: {
                        //Debug.Log(theBrick.name + ": " + LayerGet(theBrick));
                        var layerOrder = children
                                        .GroupBy(bri => LayerGet(bri)).ToArray()
                                        .OrderBy(grp => grp.Key).ToArray();
                        switch (direction[inc % direction.Count]) {
                            case 1: {
                                    if (layerOrder.ElementAtOrDefault(0).Key == LayerGet(theBrick))
                                    { valid = true; } else { valid = false; } break; }
                            case 0: {
                                    if (layerOrder.ElementAtOrDefault(layerOrder.Count() - 1).Key == LayerGet(theBrick))
                                    { valid = true; } else { valid = false; } break; } }
                        break; }
            }
            switch (valid)
            {
                case true: {
                        badBrick = false;
                        count--; children.Remove(theBrick);
                        if (theBrick.name.Contains(incBrick.name))
                        { inc++; Debug.LogFormat("[LEGO Removal #{0}] IncBrick! B: {1}, D: {2}", moduleId, baseOrder[inc % baseOrder.Count()], direction[inc % direction.Count()]); }

                        int lay = LayerGet(theBrick);
                        Bounds the = Bo(theBrick.gameObject);
                        List<GameObject> theLayer = structure[lay]; theLayer.Remove(theBrick.gameObject);
                        //Debug.Log("Selected Brick: " + theBrick.name);


                        if (lay > 0) {

                            //find foundation
                            int lowLay = lay; int b113 = lowLay - 3;
                            while (lowLay > 0) {
                                lowLay--;
                                foreach (GameObject lowerBrick in structure[lowLay]) {
                                    if (Bo(lowerBrick).Intersects(the)) {
                                        if (!checker.Contains(lowerBrick))
                                        { transfer.Add(lowerBrick); } } }
                                checker.AddRange(transfer); transfer.Clear();
                                if (lowLay == b113) { break; } }

                            //filter foundation
                            if (theLayer.Count > 0) {
                                int b = 0;
                                foreach (GameObject foBri in checker) {
                                    foreach (GameObject aboveBrick in theLayer) {
                                        if (Bo(foBri).Intersects(Bo(aboveBrick))) {
                                            //Debug.Log(foBri.name + " is under " + aboveBrick.name + "...");
                                            b++; } }
                                    if (b == 0) { che.Add(foBri); }
                                    b = 0; } }
                            else { che.AddRange(checker); } checker.Clear();


                            // delegate
                            if (che.Count > 0) {
                                foreach (GameObject brick in che) { DelegateBrick(brick.GetComponent<KMSelectable>()); children.Add(brick.GetComponent<KMSelectable>()); }
                            } che.Clear();
                        }

                        Destroy(theBrick.gameObject);
                        List<KMSelectable> ch = new List<KMSelectable>(); ch.AddRange(children); ch.Add(buttonSelect);

                        theModule.Children = ch.ToArray();
                        theModule.UpdateChildrenProperly();

                        break;
                    }
                case false:
                    {
                        foreach (GameObject bri in structure.Last()) {
                            stupidDumb += SizeGet(bri.GetComponent<KMSelectable>()); }
                        incBrick = bricks[stupidDumb % 12]; stupidDumb = 0; inc = 0;

                        Debug.LogFormat("[LEGO Removal #{0}] HEY! IncBrick: {1}, B: {2}, D: {3}", moduleId, incBrick.name, baseOrder[inc % baseOrder.Count()], direction[inc % direction.Count()]);
                        badBrick = true; GetComponent<KMBombModule>().HandleStrike(); break;
                    }
            }
          
        }
        
        else
        {
            if (!foundHigh) {
                foreach (KMSelectable sel in children) {
                    sel.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.clear); }

                int lowLay = structure.FindIndex(l => l.Contains(theBrick.gameObject));
                checker.Add(theBrick.gameObject);

                while (lowLay > 0) {
                    lowLay--;
                    foreach (GameObject upperBrick in checker) {
                        foreach (GameObject lowerBrick in structure[lowLay]) {
                            if (Bo(upperBrick).Intersects(Bo(lowerBrick))) {
                                if (!checker.Contains(lowerBrick)) {
                                    //Debug.Log(lowerBrick.size.x + ", " + lowerBrick.size.y + ", " + lowerBrick.size.z);
                                    transfer.Add(lowerBrick);
                                } } } }
                    checker.AddRange(transfer); transfer.Clear();
                }

                foreach (GameObject bo in checker) {
                    foundList.Add(bo.GetComponent<KMSelectable>());
                } checker.Clear(); foundHigh = true;
            }  
        }
        
    }

    void DelegateBrick(KMSelectable brick)
    {
        brick.OnHighlight += delegate {
            if (!moduleSolved)
            {
                string txt = brick.GetComponent<MeshRenderer>().material.name;
                screenText[0].text = txt[0].ToString();
                screenText[1].text = txt[1].ToString();
                screenText[2].text = txt[2].ToString();
            }
        };
        brick.OnDefocus += delegate {
            //brick.OnHighlightEnded += delegate {
            if (!moduleSolved)
            {
                screenText[0].text = "";
                screenText[1].text = "";
                screenText[2].text = "";
            }
        };


        KMSelectable pressedBrick = brick;
        brick.OnInteract += delegate () { PressBrick(pressedBrick); return false; };
    }

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
    private void OnLightsOn(bool _)
    {
        //isLightsOn = true;
        foreach (GameObject glowy in glowyBricks)
        { glowy.GetComponent<MeshRenderer>().material = colorMats[38]; }
        glowBriChange();
    }
    private void OnLightsOff(bool _)
    {
        //isLightsOn = false;
        foreach (GameObject glowy in glowyBricks)
        { glowy.GetComponent<MeshRenderer>().material = glow; }
        glowBriChange();
    }
    void glowBriChange()
    {
        if (isLightOn)
        {
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

    //IEnumerator PassIndicator() { }

    IEnumerator StrikeIndicator()
    {
        foreach (List<GameObject> lis in structure) {
            foreach (GameObject bri in lis) {
                bri.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.Lerp(norm1, bad, t)); } }

        if (t > .99f) {
            t = 0f; s++; Color hugh = norm1; norm1 = bad; bad = hugh;
            if (s == 2) { s = 0; badBrick = false; } }
        else { t += .05f; }

        yield return new WaitForSeconds(1f);
    }
    IEnumerator PressButton() {
        if (foundHigh) {
            foreach (KMSelectable sel in foundList) {
                sel.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.Lerp(norm1, high1, t)); } }
        else {
            foreach (KMSelectable sel in children) {
                sel.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", Color.Lerp(norm, high, t)); } }

        //Debug.Log(t);
        if (t > .99f) {
            t = 0f; Color idiot = norm; norm = high; high = idiot;
                    Color dummy = norm1; norm1 = high1; high1 = dummy; }
        else { t += .025f; }

        yield return new WaitForSeconds(1f);
    }

    void Update()
    {
        if (buttonPress) { StartCoroutine(PressButton()); }
        if (badBrick) { StartCoroutine(StrikeIndicator()); }
    }
}
