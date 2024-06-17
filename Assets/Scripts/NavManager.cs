using System;
using System.Collections;
using System.Collections.Generic;
using PolySpatial.Samples;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.AI;

// #if UNITY_INCLUDE_ARFOUNDATION
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// #endif


public class NavManager : MonoBehaviour
{
    // Dictionary of the GameObject meshes and their NavMeshSurfaces that are attached as components
    public Dictionary<GameObject, NavMeshSurface> navigableObjects = new();
    private bool isUpdating = false;
    private string DEBUG_TAG = "[NAV MANAGER]: ";
    public ApplicationReferences appRef;
    [SerializeField] private Transform groundDetectorOrigin;
    [SerializeField] public NavMeshAgent navMeshAgent;
    [SerializeField] private Animator agentAnimator;
    public GameObject navNode;
    public GameObject debugNode;
    public GameObject classifiedDebugNode;
    private static readonly int IsWalking = Animator.StringToHash("isRunning");
    private static readonly int DoJump = Animator.StringToHash("doJump");
    private bool isSeating;
    private bool isMoving;

    public void FixedUpdate()
    {
        // // find all game objects that use the Navigable tag
        //     // mesh will be generated from prefab with "Navigable" tag
        // navigableObjects = GameObject.FindGameObjectsWithTag("Navigable");

        // perform a raycast from groundDectorOrigin to the ground and add navigable
        RaycastHit hit;
        if (Physics.Raycast(groundDetectorOrigin.position, Vector3.down, out hit, 10f))
        {
            if (hit.collider.gameObject.CompareTag("Navigable"))
            {
                Debug.Log(DEBUG_TAG + "Navigable object found: " + hit.collider.gameObject.name);
                AddNavigable(hit.collider.gameObject);
            }
            else
            {
                Debug.Log(DEBUG_TAG + "Collider hit, but no navigable object tag: " + hit.collider.gameObject.tag);
            }
        }
        else
        {
            Debug.Log(DEBUG_TAG + "No navigable object found");
        }

        if (navigableObjects.Count <= 0)
        {
            Debug.Log(DEBUG_TAG + "No navigable objects have been added.");
        }
        
        foreach (KeyValuePair<GameObject, NavMeshSurface> pair in navigableObjects)
        {
            if (isUpdating == false)
            {
                StartCoroutine(UpdateNavMesh(pair.Value));
            }
        }

        // if (navNode != null) {
        //     // if the char has reached the destination node
        //     if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.1f) {
        //         if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f) {
        //             if (navMeshAgent.gameObject.GetComponent<Animation>() != null) {
        //                 navMeshAgent.gameObject.GetComponent<Animation>().Play("Idle");
        //             }
        //         }
        //         agentAnimator.SetBool(IsWalking, false);
        //     }
        //     else
        //     {
        //         agentAnimator.SetBool(IsWalking, true);
        //     }
        // }

        if(isMoving && Vector3.Distance(navMeshAgent.gameObject.transform.position, navMeshAgent.destination) <= 0.2f)
        {
            Debug.Log(DEBUG_TAG + "Reached destination");
            agentAnimator.SetBool(IsWalking, false);
            isMoving = false;
            navMeshAgent.ResetPath();
            Debug.Log(DEBUG_TAG + "isseating:" + isSeating);
            if (isSeating)
            {
                Debug.Log(DEBUG_TAG + "Seating animation");
                agentAnimator.SetTrigger(DoJump);

                // move agent up z axis
                MoveAgentUpToSeat(navMeshAgent.destination);
                isSeating = false;
            }
        }



    }

    /**
     * Add a navigable object to the list of navigable objects
     * Used in InputDebug.cs for adding meshes that are tapped (user selects the floor meshes)
     */
    public void AddNavigable(GameObject n)
    {
        if (navigableObjects.ContainsKey(n) == false)
        {
            Debug.Log(DEBUG_TAG + "Adding navigable object: " + n.ToString());
            NavMeshSurface navMeshSurface = n.AddComponent<NavMeshSurface>();
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navigableObjects.Add(n, navMeshSurface);
        } else {
            Debug.Log(DEBUG_TAG + "Navigable object already exists: " + n.ToString());
        }
    }

    private IEnumerator UpdateNavMesh(NavMeshSurface navMeshSurface)
    {
        if (navMeshSurface != null)
        {
            isUpdating = true;
            Debug.Log(DEBUG_TAG + "Updating NavMeshSurface");
            navMeshSurface.BuildNavMesh();
            yield return new WaitForSeconds(1f);
            isUpdating = false;
        } else {
            Debug.Log(DEBUG_TAG + "NavMeshSurface is null");
        }
    }

    public void MakeNavigate(GameObject destNode) {

        if (destNode != null)
        {
            Vector3 destPos = destNode.transform.position;
            Debug.Log(DEBUG_TAG + "Navigating to: " + destPos);
            // move the user to the selected position
            navMeshAgent.SetDestination(destPos);
            navNode = destNode;

            // animations
            agentAnimator.SetBool(IsWalking, true);

            isMoving = true;

            if (navMeshAgent.gameObject.GetComponent<Animation>() != null) {
                navMeshAgent.gameObject.GetComponent<Animation>().Play("Walking");
            }
        }
        else
        {
            Debug.Log(DEBUG_TAG + "No navigation position found");
        }
    }

    public void MakeNavigate(Vector3 destCoords) {
        navMeshAgent.gameObject.GetComponent<NavMeshAgent>().enabled = true;

        Vector3 destPos = new Vector3(destCoords.x, 0, destCoords.z);
        Debug.Log(DEBUG_TAG + "Navigating to: " + destPos);
        // move the user to the selected position
        navMeshAgent.SetDestination(destPos);
        if(debugNode) Instantiate(debugNode, destPos, Quaternion.identity);

        // animations
        agentAnimator.SetBool(IsWalking, true);

        isMoving = true;

        if (navMeshAgent.gameObject.GetComponent<Animation>() != null) {
            navMeshAgent.gameObject.GetComponent<Animation>().Play("Walking");
        }
    }

    public void CallPuppy() {
        Debug.Log(DEBUG_TAG + "Calling the puppy to you");
        GameObject yourPos = new GameObject();
        yourPos.transform.position = appRef.camTrans.position;
        Debug.Log(DEBUG_TAG + "Your position: " + yourPos.transform.position);
        MakeNavigate(yourPos);
        Debug.Log(DEBUG_TAG + "Puppy is on the way!");
    }
    public void MoveAgentToNearestSeatMesh() {
        Debug.Log(DEBUG_TAG + "Calling the puppy to seat mesh");
        var seats = GameObject.FindGameObjectsWithTag("Seat");
        Debug.Log(DEBUG_TAG + "Number of seats: " + seats.Length);
        // find the nearest seat in seats
        GameObject nearestSeat = null;
        float minDistance = Mathf.Infinity;
        Vector3 targetPos = Vector3.zero;
        foreach (GameObject seat in seats)
        {
            Vector3 seatPos = seat.transform.GetComponent<MeshRenderer>().bounds.center;
            Debug.Log(DEBUG_TAG + "Seat position: " + seatPos);
            float distance = Vector3.Distance(seatPos, appRef.camTrans.position);
            if (distance < minDistance)
            {
                nearestSeat = seat;
                targetPos = seatPos;
                minDistance = distance;
            }
        }
        if(nearestSeat) Debug.Log(DEBUG_TAG + "Navigating to seat at position " + targetPos);
        Debug.Log(DEBUG_TAG + "Your position: " + appRef.camTrans.position);
        MakeNavigate(targetPos);
        Debug.Log(DEBUG_TAG + "Puppy is on the way!");
        isSeating = true;
    }

    public void MoveAgentToNearestSeatPlane() {
        Debug.Log(DEBUG_TAG + "Calling the puppy to seat plane");
        var planes = GameObject.FindGameObjectsWithTag("Plane");
        Debug.Log(DEBUG_TAG + "Number of planes: " + planes.Length);
        // find the nearest seat in seats
        GameObject nearestSeat = null;
        float minDistance = Mathf.Infinity;
        Vector3 targetPos = Vector3.zero;
        foreach (GameObject seat in planes)
        {
            if (seat.GetComponent<ARPlane>().classification == PlaneClassification.Seat)
            {
                Vector3 seatPos = seat.transform.position;
                Debug.Log(DEBUG_TAG + "Seat plane position: " + seatPos);
                float distance = Vector3.Distance(seatPos, appRef.camTrans.position);
                if (distance < minDistance)
                {
                    nearestSeat = seat;
                    targetPos = seatPos;
                    minDistance = distance;
                }
            }
        }
        if(nearestSeat) Debug.Log(DEBUG_TAG + "Navigating to seat plane at position " + targetPos);
        Debug.Log(DEBUG_TAG + "Your position: " + appRef.camTrans.position);
        MakeNavigate(targetPos);
        Debug.Log(DEBUG_TAG + "Puppy is on the way!");
        isSeating = true;
    }

    public void MoveAgentUpToSeat(Vector3 seatPos)
    {
        seatPos = new Vector3(0, 0.6f, 0);

        // disable navmeshagent component
        navMeshAgent.gameObject.GetComponent<NavMeshAgent>().enabled = false;

        // over time, move the agent up to the seat position
        while (Vector3.Distance(navMeshAgent.gameObject.transform.position, seatPos) >= 0.08f)
        {
            navMeshAgent.gameObject.transform.position = Vector3.MoveTowards(navMeshAgent.gameObject.transform.position,
                seatPos, 0.1f*Time.deltaTime);
            Debug.Log(DEBUG_TAG + "Moving agent up to seat position: " + seatPos);
        }

    }
}
