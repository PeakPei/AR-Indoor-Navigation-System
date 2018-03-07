﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vuforia;

public class MarkerConstructor : MonoBehaviour
{
    //public GameObject arrowPrefab, checkTruePrefab, descriptionBoardPrefab;
    public GameObject[] arObjectList = new GameObject[3];
    public List<DraftMarkerData> draftMarkerList;
    private GameObject[] allNodeList;
    private GameObject choosenMarker, oldMarker;
    private ARObject.Type lastObjectType = ARObject.Type.Board;
    private float mostPriority = 0;
    private int markerCount = 0;
    public enum ObjectType
    {
        Arrow,
        Check,
        Board
    }

    /* Check detected marker every loop and choose one of marker.
    Attract AR Object to that marker
     and Send result to MainController to update and display */

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        markerCount = 0;
        mostPriority = 0;
        IEnumerable<TrackableBehaviour> trackableBehaviours = TrackerManager.Instance.GetStateManager().GetActiveTrackableBehaviours();

        // Loop over all TrackableBehaviours. add data and 
        foreach (TrackableBehaviour trackableBehaviour in trackableBehaviours)
        {
            AddMarkerData(trackableBehaviour);
            GameObject markerObj = trackableBehaviour.gameObject;
            //Select marker to Choosen marker  ... now use most priority that found
            if (markerObj.GetComponent<MarkerData>().priority > mostPriority)
            {
                choosenMarker = markerObj;
                mostPriority = markerObj.GetComponent<MarkerData>().priority;
            }
            markerCount = +1;
        }

        /* take action with choosen marker */
        if (choosenMarker != null && oldMarker != null)
        {
            //Debug.Log(choosenMarker.name + " " + oldMarker.name);
            if (choosenMarker.name != oldMarker.name) //triger once when have new marker
            {

                CreateArObject(choosenMarker);
                //send node
                MainController.instance.SetBeginMarker(choosenMarker);
                //MainController.instance.SetBeginPoint(choosenMarker.GetComponent<MarkerData>().GetParentNodeObject());
                //update begin point
                //run dijkstra
                // send to main controller to choose ar shown
                // point arrrow
                oldMarker = choosenMarker;
            }
        }
        else if (choosenMarker != null && oldMarker == null)
        {
            CreateArObject(choosenMarker);
            MainController.instance.SetBeginMarker(choosenMarker);
            //MainController.instance.SetBeginPoint(choosenMarker.GetComponent<MarkerData>().GetParentNodeObject());
            //instantiate like below
            oldMarker = choosenMarker;
        }

        //will move to ARControlScript
        if (markerCount == 0) //target Lost
        {
            foreach (GameObject arobj in arObjectList)
            {
                arobj.SetActive(false);
                choosenMarker = null;
            }
        }
        else
        {
            foreach (GameObject arobj in arObjectList)
            {
                switch (this.lastObjectType)
                {
                    case ARObject.Type.Arrow:
                        if (arobj.GetComponent<ArrowScript>() != null)
                        {
                            arobj.SetActive(true);
                        }
                        break;
                    case ARObject.Type.Check:
                        if (arobj.GetComponent<CheckTrueScript>() != null)
                        {
                            arobj.SetActive(true);
                        }
                        break;
                    case ARObject.Type.Board:
                        if (arobj.GetComponent<DescriptionBoardScript>() != null)
                        {
                            arobj.SetActive(true);
                        }
                        break;
                    default:
                        if (arobj.GetComponent<DescriptionBoardScript>() != null)
                        {
                            arobj.SetActive(true);
                        }
                        break;
                }
            }
        }
    }

    public void SetLastObjectType(ARObject.Type objtype)
    {
        this.lastObjectType = objtype;
    }

    public void AddDraftMarker(DraftMarkerData draftmarker)
    /* add draft data from jsonReader */
    {
        if (draftMarkerList == null)
        {
            draftMarkerList = new List<DraftMarkerData>();
        }
        Debug.Log("Add marker " + draftmarker.markerID);
        draftMarkerList.Add(draftmarker);
    }

    private void AddMarkerData(TrackableBehaviour trackableBehaviour)
    /* Add marker to dtected marker */
    {
        //check that marker not has markerdata. 
        if (trackableBehaviour.gameObject.GetComponent<MarkerData>() == null)
        {
            foreach (DraftMarkerData dm in draftMarkerList)
            {
                //check name that match draftmarkerlist and add markerdata.
                if (dm.markerImageName == trackableBehaviour.TrackableName)
                {
                    trackableBehaviour.gameObject.AddComponent<MarkerData>();
                    MarkerData tmd = trackableBehaviour.gameObject.GetComponent<MarkerData>();
                    tmd.markerID = dm.markerID;
                    tmd.markerImageName = dm.markerImageName;
                    tmd.priority = dm.priority;
                    tmd.markerOrientation = dm.markerOrientation;
                    tmd.fkNodeID = dm.fkNodeID;
                    trackableBehaviour.gameObject.name = "Marker" + tmd.markerID;

                    //attract marker object to node
                    string nodeStructureTag = "Node";
                    if (allNodeList == null)
                    {
                        allNodeList = GameObject.FindGameObjectsWithTag(nodeStructureTag);
                    }
                    foreach (GameObject tObj in allNodeList)
                    {
                        if (nodeStructureTag + tmd.fkNodeID == tObj.name)
                        {
                            trackableBehaviour.gameObject.transform.SetParent(tObj.transform);
                            trackableBehaviour.gameObject.transform.localPosition = Vector3.zero;
                            trackableBehaviour.gameObject.transform.localEulerAngles = new Vector3(0, 0, tmd.markerOrientation); //<<<<<< to Vector3
                            break;
                        }
                    }
                    draftMarkerList.Remove(dm); //remove for best performance in next loop
                    break;
                }
            }
        }
    }

    private void CreateArObject(GameObject markerObject)
    /* factory pattern to create/activate AR object and put data attrach to parent */
    {
        Debug.Log("Create AR Object to " + markerObject.name);
        foreach (GameObject arobj in arObjectList)
        {
            Debug.Log(markerObject.name);
            arobj.transform.SetParent(markerObject.transform);
            arobj.GetComponent<IARObject>().InitAR();
            arobj.transform.localPosition = Vector3.zero;
            arobj.transform.localEulerAngles = new Vector3(0, 0, markerObject.GetComponent<MarkerData>().markerOrientation);
            arobj.transform.localScale = Vector3.one;
        }
    }

    // private void CreateArObject(GameObject markerObject, ARObject.Type otype)
    // /* unused */
    // {
    //     GameObject arObj;
    //     // find all child and check that only one have arobjectScript and currenly activated

    //     if (otype == ARObject.Type.Arrow)
    //     {
    //         List<ArrowScript> arrowList = new List<ArrowScript>();
    //         markerObject.GetComponentsInChildren<ArrowScript>(true, arrowList);
    //         if (arrowList.Count < 1) //no objeect
    //         {
    //             arObj = Instantiate(arrowPrefab);
    //             arObj.name = "Arrow";
    //         }
    //         else //at least 1
    //         {
    //             arObj = arrowList[0].gameObject;
    //             arObj.SetActive(true);
    //             if (arrowList.Count > 1)
    //             {
    //                 for (int a = 1; a < arrowList.Count; a++)
    //                 {
    //                     Destroy(arrowList[a].gameObject);
    //                 }
    //             }
    //         }
    //     }
    //     else if (otype == ARObject.Type.Check)
    //     {
    //         List<CheckTrueScript> checkList = new List<CheckTrueScript>();
    //         markerObject.GetComponentsInChildren<CheckTrueScript>(true, checkList);
    //         if (checkList.Count < 1) //no objeect
    //         {
    //             arObj = Instantiate(arrowPrefab);
    //             arObj.name = "Check";
    //         }
    //         else //at least 1
    //         {
    //             arObj = checkList[0].gameObject;
    //             arObj.SetActive(true);
    //             if (checkList.Count > 1)
    //             {
    //                 for (int c = 1; c < checkList.Count; c++)
    //                 {
    //                     Destroy(checkList[c].gameObject);
    //                 }
    //             }
    //         }
    //     }
    //     else
    //     {
    //         List<DescriptionBoardScript> boardList = new List<DescriptionBoardScript>();
    //         markerObject.GetComponentsInChildren<DescriptionBoardScript>(true, boardList);
    //         if (boardList.Count < 1) //no objeect
    //         {
    //             arObj = Instantiate(arrowPrefab);
    //             arObj.name = "Board";
    //         }
    //         else //at least 1
    //         {
    //             arObj = boardList[0].gameObject;
    //             arObj.SetActive(true);
    //             if (boardList.Count > 1)
    //             {
    //                 for (int c = 1; c < boardList.Count; c++)
    //                 {
    //                     Destroy(boardList[c].gameObject);
    //                 }
    //             }
    //         }
    //         DescriptionBoardScript desBoard = arObj.GetComponent<DescriptionBoardScript>();
    //         desBoard.SetRoomName(markerObject.GetComponent<MarkerData>().GetParentObjectData().GetParentObjectData().roomName);
    //         desBoard.SetRoomDest(markerObject.GetComponent<MarkerData>().GetParentObjectData().GetParentObjectData().roomDescription);
    //     }
    //     arObj.transform.SetParent(markerObject.transform);
    //     arObj.transform.localPosition = Vector3.zero;
    //     arObj.transform.localEulerAngles = Vector3.zero;
    //     arObj.transform.localScale = Vector3.one;
    // }
}
