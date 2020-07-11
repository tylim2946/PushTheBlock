using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;
using System.Linq;
using UnityEngine.Rendering.Universal.Internal;

public class PlayerController : MonoBehaviour
{
    public Camera cam;
    public NavMeshAgent agent;
    public NavMeshSurface surf;

    public Material idle;
    public Material pushing;

    private Vector3 currPos;

    public GameObject corner1;
    public GameObject corner2;

    private Vector3 cornerPos1;
    private Vector3 cornerPos2;

    private Tile[,,] mTiles;
    private List<Vector3> mPath;

    private int xLength;
    private int yLength;
    private int zLength;

    private bool isGoingToPush = false;
    private bool isMoving = false;

    private int mState;
    private const int STATE_IDLE = 0;
    private const int STATE_MOVING_START = -1; // implement through functions such as OnDestinationReached()
    private const int STATE_MOVING = 1;
    private const int STATE_MOVING_STOPPED = -1; // implement through functions such as OnDestinationReached()
    private const int STATE_PUSHING = 2;
    private const int STATE_MOVING_PUSHING = 3;



    private void Start()
    {
        GenerateGrid(out mTiles);
    }

    private void GenerateGrid(out Tile[,,] tiles)
    {
        // calculate x, y, z length
        xLength = Convert.ToInt32(Math.Abs(Convert.ToInt32(corner1.transform.position.x) - Convert.ToInt32(corner2.transform.position.x))) + 1;
        yLength = Convert.ToInt32(Math.Abs(Convert.ToInt32(corner1.transform.position.y) - Convert.ToInt32(corner2.transform.position.y))) + 1;
        zLength = Convert.ToInt32(Math.Abs(Convert.ToInt32(corner1.transform.position.z) - Convert.ToInt32(corner2.transform.position.z))) + 1;

        // adjust corners
        cornerPos1.x = Math.Min(corner1.transform.position.x, corner2.transform.position.x);
        cornerPos1.y = Math.Min(corner1.transform.position.y, corner2.transform.position.y);
        cornerPos1.z = Math.Min(corner1.transform.position.z, corner2.transform.position.z);
        cornerPos2.x = Math.Max(corner1.transform.position.x, corner2.transform.position.x);
        cornerPos2.y = Math.Max(corner1.transform.position.y, corner2.transform.position.y);
        cornerPos2.z = Math.Max(corner1.transform.position.z, corner2.transform.position.z);

        // initialize tiles
        tiles = new Tile[xLength, yLength, zLength];

        // initialize path
        mPath = new List<Vector3>();

        // initialize currPos
        List<Collider> nearbyObj = Physics.OverlapSphere(transform.position + Vector3.down * 1.1f, 0.01f).ToList();
        
        for (int i = nearbyObj.Count - 1; i >= 0; i --)
        {
            if (nearbyObj[i].tag != "Walkables" && nearbyObj[i].tag != "Walkable Ramps" && nearbyObj[i].tag != "Pushables")
            {
                nearbyObj.Remove(nearbyObj[i]);
            }
        }
        nearbyObj.OrderByDescending(o => o.ClosestPointOnBounds(transform.position));
        currPos = nearbyObj[0].transform.position;

        // generate tile grid
        for (int x =  0; x < xLength; x++)
        {
            for (int y = 0; y < yLength; y++)
            {
                for (int z = 0; z < zLength; z++)
                {
                    Collider[] targetTile = Physics.OverlapSphere(new Vector3(x, y, z) + cornerPos1, 0.4f);

                    foreach (Collider t in targetTile)
                    {
                        if (t.tag == "Walkables" || t.tag == "Walkable Ramps" || t.tag == "Pushables")
                        {
                            Collider[] aboveTile = Physics.OverlapSphere(t.transform.position + Vector3.up, 0.4f);
                            
                            if (aboveTile.Length == 0)
                            {
                                tiles[x, y, z] = new Tile();
                                tiles[x, y, z].SetObject(t.gameObject);
                            }
                            else
                            {
                                int count = 0;

                                foreach (Collider col in aboveTile)
                                {
                                    if (col.gameObject.tag != "Walkables" && col.gameObject.tag != "Walkable Ramps" && col.gameObject.tag != "Pushables")
                                    {
                                        count++;
                                    }
                                }

                                if (count == aboveTile.Length)
                                {
                                    tiles[x, y, z] = new Tile();
                                    tiles[x, y, z].SetObject(t.gameObject);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void InitTiles()
    {
        foreach(Tile t in mTiles)
        {
            if (t != null)
            {
                if (t.IsInitialized())
                {
                    t.Init();
                }
            }
        }
    }

    private bool FindOptPath(Vector3 start, Vector3 dest, out List<Vector3> path)
    {
        // variable declaration
        List<Tile> candidates = new List<Tile>();
        List<Tile> sortedCandidates;

        Vector3 worldPos = start;
        bool isPathFound = false;

        Tile parent = null;
        Tile destination = null;

        // initialize path variable
        path = new List<Vector3>();

        // Explore neighbors to find candidates
        do  
        {
            if (IsWithinWorld(worldPos))
            {
                // Expand node at targetPos and add them to candidates
                TileAtArrayPos(worldPos).SetVisited();

                if (candidates.Contains(TileAtArrayPos(worldPos)))
                {
                    candidates.Remove(TileAtArrayPos(worldPos));
                }

                List<Tile> neighbors = InitNeighbors(TileAtArrayPos(worldPos), dest);

                foreach (Tile t in neighbors)
                {
                    if (!candidates.Contains(t) && !t.HasVisited())
                    {
                        candidates.Add(t); // add these to candidates
                    }
                    
                    if (t.GetH() == 0)
                    {
                        isPathFound = true;
                        destination = t; //@@@@@@@ 138
                    }
                }
            }

            if (candidates.Count != 0)
            {
                // set up sorted list
                sortedCandidates = candidates.OrderBy(o => o.GetF()).ToList();

                // set new targetPos
                worldPos = sortedCandidates[0].GetPosition();
            }
            

        } while (!(candidates.Count == 0 || isPathFound));

        // Convert explored nodes into path
        if (isPathFound)
        {
            //destination = sortedCandidates[0];
            do
            {
                if (parent == null)
                {
                    parent = destination;
                    path.Insert(0, destination.GetPosition());
                }
                else
                {
                    parent = parent.GetParent();

                    if (parent.GetPosition() != start)
                    {
                        path.Insert(0, parent.GetPosition());
                    }
                }
            } while (parent.GetPosition() != start);
        }

        InitTiles();

        return isPathFound;
    }

    private List<Tile> InitNeighbors(Tile tile, Vector3 dest)
    {
        List<Tile> results = new List<Tile>();
        
        for (int i = 1; i <= 12; i ++) // but then you can only go up if it is ramp && ramp direction matters
        {
            Tile test = InitNeighbor(tile, dest, i);

            if (test != null)
            {
                results.Add(test);
            }
        }

        return results;
    }

    private Vector3 PosToTilesIndexes(Vector3 pos)
    {
        return pos - cornerPos1;
    }

    private Tile TileAtArrayPos(Vector3 worldPos)
    {
        Vector3 arrayPos = PosToTilesIndexes(worldPos);
        return mTiles[Convert.ToInt32(arrayPos.x), Convert.ToInt32(arrayPos.y), Convert.ToInt32(arrayPos.z)];
    }

    private Tile InitNeighbor(Tile tile, Vector3 dest, int side)
    {
        Tile result = null;
        Vector3 worldPos = new Vector3();

        // set targetPos
        switch(side)
        {
            case 1: // N
                worldPos = tile.GetPosition() + Vector3.forward;
                break;
            case 2: // E
                worldPos = tile.GetPosition() + Vector3.right;
                break;
            case 3: // S
                worldPos = tile.GetPosition() + Vector3.back;
                break;
            case 4: // W
                worldPos = tile.GetPosition() + Vector3.left;
                break;
            case 5: // N
                worldPos = tile.GetPosition() + Vector3.up + Vector3.forward;
                break;
            case 6: // E
                worldPos = tile.GetPosition() + Vector3.up + Vector3.right;
                break;
            case 7: // S
                worldPos = tile.GetPosition() + Vector3.up + Vector3.back;
                break;
            case 8: // W
                worldPos = tile.GetPosition() + Vector3.up + Vector3.left;
                break;
            case 9: // N
                worldPos = tile.GetPosition() + Vector3.down + Vector3.forward;
                break;
            case 10: // E
                worldPos = tile.GetPosition() + Vector3.down + Vector3.right;
                break;
            case 11: // S
                worldPos = tile.GetPosition() + Vector3.down + Vector3.back;
                break;
            case 12: // W
                worldPos = tile.GetPosition() + Vector3.down + Vector3.left;
                break;
        }

        // check if targetPos is valid
        if (IsWithinWorld(worldPos))
        {
            // if a node exists at targetPos
            if (TileAtArrayPos(worldPos) != null)
            {
                if (side >= 1 && side <= 4 && CheckRampOri(tile, worldPos, side))
                {
                    // result is set to that tile
                    result = TileAtArrayPos(worldPos);

                    if (!result.IsInitialized())
                    {
                        result.SetParent(tile);
                    }

                    // initialize tile values
                    result.InitValues(dest, tile);
                }
                else if (side >= 5 && side <= 12 && CheckRampToTile(tile, worldPos) && CheckRampOri(tile, worldPos, side))
                {
                    // result is set to that tile
                    result = TileAtArrayPos(worldPos);

                    if (!result.IsInitialized())
                    {
                        result.SetParent(tile);
                    }

                    // initialize tile values
                    result.InitValues(dest, tile);
                }
            }
        }

        return result;
    }

    private bool CheckRampToTile(Tile t1, Vector3 t2)
    {
        bool check1 = false; // ramp to tile or tile to ramp // assume t1 and t2 have different y
        Tile tile = TileAtArrayPos(t2);

        // Check 1
        // when tile is ramp
        if ((t1.GetObject().CompareTag("Walkables") || t1.GetObject().CompareTag("Pushables")) && tile.GetObject().CompareTag("Walkable Ramps"))
        {
            check1 = true;
        }

        // when t1 is ramp
        if ((tile.GetObject().CompareTag("Walkables") || tile.GetObject().CompareTag("Pushables")) && t1.GetObject().CompareTag("Walkable Ramps"))
        {
            check1 = true;
        }

        return check1;
    }

    private bool CheckRampOri(Tile t1, Vector3 t2, int side)
    {
        bool check2 = false; // ramp orientation // compare rotation and side value?
        Tile tile = TileAtArrayPos(t2);
        float ori1 = t1.GetObject().transform.rotation.y;
        float ori2 = tile.GetObject().transform.rotation.y;

        if (t1.GetObject().CompareTag("Walkable Ramps"))
        {
            ori1 = t1.GetObject().transform.parent.parent.rotation.eulerAngles.y;
        }

        if (tile.GetObject().CompareTag("Walkable Ramps"))
        {
            ori2 = tile.GetObject().transform.parent.parent.rotation.eulerAngles.y;
        }             

        if (side >= 1 && side <= 4)
        {
            // t1 is lamp
            if (t1.GetObject().CompareTag("Walkable Ramps") && !tile.GetObject().CompareTag("Walkable Ramps"))
            {
                switch (side)
                {
                    case 1:
                        if (ori1 == 180)
                        {
                            check2 = true;
                        }
                        break;
                    case 2:
                        if (ori1 == 270)
                        {
                            check2 = true;
                        }
                        break;
                    case 3:
                        if (ori1 == 0)
                        {
                            check2 = true;
                        }
                        break;
                    case 4:
                        if (ori1 == 90)
                        {
                            check2 = true;
                        }
                        break;
                }
            }

            // t2 is lamp
            else if (!t1.GetObject().CompareTag("Walkable Ramps") && tile.GetObject().CompareTag("Walkable Ramps"))
            {
                switch (side)
                {
                    case 1:
                        if (ori2 == 0)
                        {
                            check2 = true;
                        }
                        break;
                    case 2:
                        if (ori2 == 90)
                        {
                            check2 = true;
                        }
                        break;
                    case 3:
                        if (ori2 == 180)
                        {
                            check2 = true;
                        }
                        break;
                    case 4:
                        if (ori2 == 270)
                        {
                            check2 = true;
                        }
                        break;
                }
            }

            // both t1 and t2 is lamp
            else if (t1.GetObject().CompareTag("Walkable Ramps") && tile.GetObject().CompareTag("Walkable Ramps"))
            {
                if (side == 1 || side == 3)
                {
                    if (ori1 == ori2 && (ori1 == 90|| ori1 == 270))
                    {
                        check2 = true;
                    }
                }
                else if (side == 2|| side == 4 )
                {
                    if (ori1 == ori2 && (ori1 == 0 || ori1 == 180))
                    {
                        check2 = true;
                    }
                }
            }

            // neither is lamp
            else
            {
                check2 = true;
            }
        }
        else if (side >= 5 && side <= 8)
        {
            // t2 is lamp
            if (!t1.GetObject().CompareTag("Walkable Ramps") && tile.GetObject().CompareTag("Walkable Ramps"))
            {
                switch (side)
                {
                    case 5:
                        if (ori2 == 180)
                        {
                            check2 = true;
                        }
                        break;
                    case 6:
                        if (ori2 == 270)
                        {
                            check2 = true;
                        }
                        break;
                    case 7:
                        if (ori2 == 0)
                        {
                            check2 = true;
                        }
                        break;
                    case 8:
                        if (ori2 == 90)
                        {
                            check2 = true;
                        }
                        break;
                }
            }

            // both t1 and t2 is lamp
            else if (t1.GetObject().CompareTag("Walkable Ramps") && tile.GetObject().CompareTag("Walkable Ramps"))
            {
                if (ori1 == ori2)
                {
                    switch(side)
                    {
                        case 5:
                            if (ori1 == 180)
                            {
                                check2 = true;
                            }
                            break;
                        case 6:
                            if (ori1 == 270)
                            {
                                check2 = true;
                            }
                            break;
                        case 7:
                            if (ori1 == 0)
                            {
                                check2 = true;
                            }
                            break;
                        case 8:
                            if (ori1 == 90)
                            {
                                check2 = true;
                            }
                            break;
                    }
                }
            }
        }
        else if (side >= 9 && side <= 12)
        {
            // t1 is lamp
            if (t1.GetObject().CompareTag("Walkable Ramps") && !tile.GetObject().CompareTag("Walkable Ramps"))
            {
                switch (side)
                {
                    case 9:
                        if (ori1 == 0)
                        {
                            check2 = true;
                        }
                        break;
                    case 10:
                        if (ori1 == 90)
                        {
                            check2 = true;
                        }
                        break;
                    case 11:
                        if (ori1 == 180)
                        {
                            check2 = true;
                        }
                        break;
                    case 12:
                        if (ori1 == 270)
                        {
                            check2 = true;
                        }
                        break;
                }
            }

            // both t1 and t2 is lamp
            else if (t1.GetObject().CompareTag("Walkable Ramps") && tile.GetObject().CompareTag("Walkable Ramps"))
            {
                if (ori1 == ori2)
                {
                    switch (side)
                    {
                        case 9:
                            if (ori1 == 0)
                            {
                                check2 = true;
                            }
                            break;
                        case 10:
                            if (ori1 == 90)
                            {
                                check2 = true;
                            }
                            break;
                        case 11:
                            if (ori1 == 180)
                            {
                                check2 = true;
                            }
                            break;
                        case 12:
                            if (ori1 == 270)
                            {
                                check2 = true;
                            }
                            break;
                    }
                }
            }
        }

        return check2;
    }

    private bool IsWithinWorld(Vector3 worldPos)
    {
        return worldPos.x >= cornerPos1.x && worldPos.x <= cornerPos2.x && worldPos.y >= cornerPos1.y && worldPos.y <= cornerPos2.y && worldPos.z >= cornerPos1.z && worldPos.z <= cornerPos2.z;
    }

    private bool IsWithinArray(Vector3 arrayPos)
    {
        return arrayPos.x >= 0 && arrayPos.x <= xLength && arrayPos.y >= 0 && arrayPos.y <= yLength && arrayPos.z >= 0 && arrayPos.z <= zLength;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if(Physics.Raycast(ray, out hit))
            {
                Debug.Log("[0] " + hit.collider.tag);
                List<Vector3> path = new List<Vector3>();

                if (mState != STATE_PUSHING)
                {
                    if (hit.collider.CompareTag("Walkables") || hit.collider.CompareTag("Walkable Ramps"))
                    {
                        Debug.Log("[1] Walkables");
                        // walkables
                        FindOptPath(currPos, hit.collider.transform.position, out path);
                    }
                    else if (hit.collider.CompareTag("Pushables"))
                    {
                        Debug.Log("[1] Pushables");
                        List<Vector3> testWalkable = new List<Vector3>();
                        List<Vector3> testPushable = new List<Vector3>();

                        // test Walkables
                        FindOptPath(currPos, hit.collider.transform.position, out testWalkable);

                        // test Pushables
                        List<Vector3>[] testPushables = new List<Vector3>[4];
                        FindOptPath(currPos, hit.collider.transform.position + Vector3.down + Vector3.forward, out testPushables[0]);
                        FindOptPath(currPos, hit.collider.transform.position + Vector3.down + Vector3.right, out testPushables[1]);
                        FindOptPath(currPos, hit.collider.transform.position + Vector3.down + Vector3.back, out testPushables[2]);
                        FindOptPath(currPos, hit.collider.transform.position + Vector3.down + Vector3.left, out testPushables[3]);

                        for (int i = 0; i < 4; i++)
                        {
                            if (testPushables[i].Count > 0 && testPushable.Count == 0)
                            {
                                testPushable = testPushables[i];
                            }

                            if (testPushables[i].Count > 0 && testPushable.Count > 0 && testPushable.Count > testPushables[i].Count)
                            {
                                testPushable = testPushables[i];
                            }

                            // how can we consider the "least turn"
                        }

                        if (testWalkable.Count > 0 && testPushable.Count > 0) // both
                        {
                            Debug.Log("[2] Both");
                            if (testPushable.Count <= testWalkable.Count)
                            {
                                // pushable
                                path = testPushable;
                            }
                            else
                            {
                                // walkable
                                path = testWalkable;
                            }
                        }
                        else if (testWalkable.Count > 0) // walkable only
                        {
                            Debug.Log("[2] Walkable Only");
                            path = testWalkable;
                        }
                        else if (testPushable.Count > 0) // pushable only
                        {
                            Debug.Log("[2] Pushable Only");
                            path = testPushable;
                        }
                    }

                    Debug.Log("# of nodes: " + path.Count);

                    if (path.Count != 0)
                    {
                        mPath = path;
                    }
                }
                else
                {
                    // pushing state
                }
            }
        }
        else
        {
            Debug.Log("[0] Raycast missed");
        }

        if (mPath.Count != 0 && agent.remainingDistance == 0.0f)
        {
            agent.SetDestination(mPath[0]);
            currPos = mPath[0];
            mPath.RemoveAt(0);
        }

        // Latch isMoving
        if (isMoving && agent.remainingDistance == 0.0f)
        {
            OnDestinationReached();
        }

        if (!isMoving && agent.remainingDistance > 0)
        {
            OnStartMoving();
        }
    }
    
    public void OnStartMoving()
    {
        // to be used with animation controller
        isMoving = true;

    }

    private void OnDestinationReached()
    {
        isMoving = false;

        if (isGoingToPush)
        {
            mState = STATE_PUSHING;
            isGoingToPush = false;
            GetComponent<MeshRenderer>().material = pushing;
        }
        else
        {
            GetComponent<MeshRenderer>().material = idle;
        }
    }

    public void OnTerrainUpdated()
    {
        /*
        instead of generating new grid, move tiles
        1. make mTile[x1, y1, z1] = null
        2. set mTile[x2, y2, z2] = GameObject
        */
        GenerateGrid(out mTiles);
        surf.BuildNavMesh();
    }

    private void FindAvailTiles(out List<Vector3> tiles)
    {
        // initialize tiles
        tiles = null;

        // get all Walkables
        GameObject[] walkables = GameObject.FindGameObjectsWithTag("Walkables");
        GameObject[] walkableRamps = GameObject.FindGameObjectsWithTag("Walkable Ramps");

        // for each walkable, if there is no Pushable on top of Walkable, add it to tiles
        foreach (GameObject obj in walkables)
        {
            Collider[] hitColliders = Physics.OverlapSphere(obj.transform.position + Vector3.up, 0.9f);

            foreach (Collider col in hitColliders)
            {
                if (col.gameObject.tag != "Walkables" && col.gameObject.tag != "Walkable Ramps" && col.gameObject.tag != "Pushables")
                {
                    tiles.Add(obj.transform.position);
                }
            }
        }
        foreach (GameObject obj in walkableRamps)
        {
            Collider[] hitColliders = Physics.OverlapSphere(obj.transform.position + Vector3.up, 0.9f);

            foreach (Collider col in hitColliders)
            {
                if (col.gameObject.tag != "Walkables" && col.gameObject.tag != "Walkable Ramps" && col.gameObject.tag != "Pushables")
                {
                    tiles.Add(obj.transform.position);
                }
            }
        }
    }
}

class Tile
{
    private GameObject mObj;

    private Tile mParent;

    private Vector3 mDest;

    private bool visited = false;
    private bool initialized = false;

    // values
    private float mF;
    private float mG;
    private float mH;
    private float mT;

    // weight
    private const float mWeightG = 1.0f;
    private const float mWeightH = 1.0f;
    private const float mWeightT = 0.00001f;

    private bool isHorizontal = false;


    public Tile()
    {
        
    }

    public void Init()
    {
        mParent = null;
        initialized = false;
        visited = false;
    }

    public void SetObject(GameObject obj)
    {
        mObj = obj;
    }

    public GameObject GetObject()
    {
        return mObj;
    }

    public void SetParent(Tile parent)
    {
        mParent = parent;
    }

    public Tile GetParent()
    {
        return mParent;
    }

    public Vector3 GetPosition()
    {
        return mObj.transform.position;
    }

    public void SetValues(float f, float g, float h, float t)
    {
        mF = f;
        mG = g;
        mH = h;
        mT = t;
    }

    public float GetF()
    {
        return mF;
    }

    public float GetH()
    {
        return mH;
    }

    public void InitValues(Vector3 dest, Tile parentCandidate)
    {
        //  (mParent, mDest, isInitialized, mF, mG, mH, mT, isHorizontal)
        float testF = float.MaxValue;
        float testG = float.MaxValue;
        float testH = float.MaxValue;
        float testT = float.MaxValue;

        if (mObj != null)
        {
            if (mDest != dest)
            {
                testH = (float) Math.Sqrt(Math.Pow(Math.Abs(mObj.transform.position.x - dest.x), 2)
                + Math.Pow(Math.Abs(mObj.transform.position.y - dest.y), 2)
                + Math.Pow(Math.Abs(mObj.transform.position.z - dest.z), 2));

                mDest = dest;
            }

            testG = 0f;
            testT = 0f;

            if (mParent != null)
            {
                testG = mParent.mG + 1f;

                isHorizontal = mObj.transform.position.z == mParent.mObj.transform.position.z;

                if (isHorizontal == mParent.isHorizontal)
                {
                    testT = mParent.mT;
                }
                else
                {
                    testT = mParent.mT + 1f;
                }

                // calculate F value
                testF = mWeightG * testG + mWeightH * testH + mWeightT * testT;
            }
        }

        // update values only if mF > testF
        if (initialized)
        {
            if (mF > testF)
            {
                SetValues(testF, testG, testH, testT);
                SetParent(parentCandidate);
            }
        }
        else
        {
            SetValues(testF, testG, testH, testT);

            // mark initialized
            initialized = true;
        }
    }

    public bool IsInitialized()
    {
        return initialized;
    }


    public bool HasVisited()
    {
        return visited;
    }

    public void SetVisited()
    {
        visited = true;
    }
}