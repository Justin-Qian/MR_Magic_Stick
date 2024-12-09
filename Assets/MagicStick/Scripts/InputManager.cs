using System;
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Unity.VisualScripting;
using UnityEngine;

public class Stick
{
    public GameObject stickObject;
    public Transform stickTransform;
    public Renderer stickRenderer;
    public Collider stickCollider;

    /// <summary>
    /// Constructor to create a Stick instance using a prefab.
    /// </summary>
    /// <param name="stickPrefab">The prefab to instantiate the stick from.</param>
    public Stick(GameObject stickPrefab, List<Collider> stickColliders)
    {
        // Create a stick object
        stickObject = GameObject.Instantiate(stickPrefab);
        stickTransform = stickObject.transform;
        stickRenderer = stickObject.GetComponent<Renderer>();
        // Set Colliders
        stickCollider = stickObject.GetComponent<Collider>();
        stickColliders.Add(stickCollider);
    }

    public void SetStickPosition(Vector3 position)
    {
        stickTransform.position = position;
    }

    public void SetStickRotation(Quaternion rotation)
    {
        stickTransform.rotation = rotation;
    }

    public void SetStickScale(Vector3 scale)
    {
        stickTransform.localScale = scale;
    }

    /// <summary>
    /// Sets the color of the stick.
    /// </summary>
    /// <param name="color">The color to apply to the stick.</param>
    public void SetStickColor(Color color)
    {
        if (stickRenderer != null && stickRenderer.material != null)
        {
            stickRenderer.material.color = color;
        }
        else
        {
            Debug.LogWarning("StickRenderer or material is missing. Cannot set color.");
        }
    }

    /// <summary>
    /// Enables or disables the collider of the stick.
    /// </summary>
    /// <param name="enable">Whether to enable or disable the collider.</param>
    public void SetColliderEnabled(bool enable)
    {
        if (stickCollider != null)
        {
            stickCollider.enabled = enable;
        }
        else
        {
            Debug.LogWarning("StickCollider is missing. Cannot change its state.");
        }
    }
}

// This class aims to manage hand-tracking and head-tracking, with functions related to them
public class InputManager : MonoBehaviour
{
    public static InputManager Instance {get; private set;}

    public Camera sceneCamera; // Head's Data
    public Vector3 lefthandPosition {get; private set;}
    public Vector3 righthandPosition {get; private set;}
    public Vector3 linkCenter {get; private set;}
    public Vector3 linkDirection {get; private set;}
    public float linkLength {get; private set;}

    public GameObject stickPrefab;

    public List<Collider> stickColliders { get; private set; }
    public string type;
    private string state;
    private Color workingColor = Color.white;
    private Color notWorkingColor = new Color(1f, 0f, 0f, 0.2f);

    // Rigid
    private Stick rigidStick;

    // Spring
    private List<Stick> springSticks;

    // Bamboo
    private List<Stick> bambooSticks;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        stickColliders = new List<Collider>();

        CreateRigid();
        CreateSpring();
        CreateBamboo(25);
        SetType(type);
    }
    // Update is called once per frame
    void Update()
    {
        // Update hand data
        lefthandPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        righthandPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);

        // Update link data
        linkCenter = (lefthandPosition + righthandPosition) / 2;
        linkDirection = righthandPosition - lefthandPosition;
        linkLength = linkDirection.magnitude;

        if (type == "Rigid")
        {
            Rigid(linkLength);
        }
        else if (type == "Spring")
        {
            Spring(linkLength);
        }
        else if (type == "Bamboo")
        {
            Bamboo(linkLength);
        }
        else
        {
            Debug.LogWarning("Type not valid.");
        }
    }

    public Vector3 GetHeadPosition()
    {
        return sceneCamera.transform.position;
    }

    public void SetType(string newType)
    {
        type = newType;
        if (type == "Rigid")
        {
            StopSpring();
            StopBamboo();
        }
        else if (type == "Spring")
        {
            StopRigid();
            StopBamboo();
        }
        else if (type == "Bamboo")
        {
            StopRigid();
            StopSpring();
        }
        else
        {
            Debug.LogWarning("Type not valid.");
        }
    }

    private void CreateRigid()
    {
        rigidStick = new Stick(stickPrefab, stickColliders);
    }
    private void Rigid(float linkLength, float initialDistance = 0.6f, float tolerance = 0.1f)
    {
        // position and direction
        rigidStick.SetStickPosition(linkCenter); // center
        rigidStick.SetStickRotation(Quaternion.FromToRotation(Vector3.up, linkDirection)); // direction

        // length
        rigidStick.SetStickScale(new Vector3(0.1f, linkLength/2, 0.1f));

        // Check if stick is in rigid state
        if (Mathf.Abs(linkLength - initialDistance) <= tolerance)
        {
            // normal
            rigidStick.SetStickColor(workingColor);
            rigidStick.SetColliderEnabled(true);
            state = "Normal";
        }
        else
        {
            // not working
            rigidStick.SetStickColor(notWorkingColor);// transparent red
            rigidStick.SetColliderEnabled(false);
            state = "Not Working";
        }

        UIManager.Instance.UpdateState(type, state, linkLength);
    }
    private void StopStick(Stick stick)
    {
        stick.SetStickPosition(Vector3.zero);
        stick.SetStickRotation(Quaternion.identity);
        stick.SetStickScale(new Vector3(0.05f, 0.0f, 0.05f));
        // not working
        stick.SetStickColor(notWorkingColor);
        stick.SetColliderEnabled(false);
    }
    private void StopRigid()
    {
        StopStick(rigidStick);
    }

    private void CreateSpring()
    {
        springSticks = new List<Stick>();
        for (int i = 0; i < 3; i++)
        {
            springSticks.Add(new Stick(stickPrefab, stickColliders));
        }
    }

    private void Spring(float linkLength, float totalDistance = 1.6f, float initialDistance = 0.8f, float tolerance = 0.05f)
    {

        if (linkLength > totalDistance)
        {
            // sticks
            StopSpring();
            state = "Too Long";
        }
        else
        {

            // position and direction
            // middle stick
            springSticks[0].SetStickPosition(linkCenter);
            springSticks[0].SetStickRotation(Quaternion.FromToRotation(Vector3.up, linkDirection));
            springSticks[0].SetStickScale(new Vector3(0.1f, linkLength/2, 0.1f));

            // ends
            float endlength = (totalDistance - linkLength) / 2;
            Vector3 leftCenter = lefthandPosition - linkDirection.normalized * (endlength/2);
            Vector3 rightCenter = righthandPosition + linkDirection.normalized * (endlength/2);

            springSticks[1].SetStickPosition(leftCenter);
            springSticks[1].SetStickRotation(Quaternion.FromToRotation(Vector3.up, linkDirection));
            springSticks[1].SetStickScale(new Vector3(0.1f, endlength/2, 0.1f));

            springSticks[2].SetStickPosition(rightCenter);
            springSticks[2].SetStickRotation(Quaternion.FromToRotation(Vector3.up, linkDirection));
            springSticks[2].SetStickScale(new Vector3(0.1f, endlength/2, 0.1f));

            if (linkLength > initialDistance + tolerance)
            {
                // Middle Working
                springSticks[0].SetStickColor(workingColor);
                springSticks[0].SetColliderEnabled(true);

                // ends not working
                springSticks[1].SetStickColor(notWorkingColor);
                springSticks[1].SetColliderEnabled(false);
                springSticks[2].SetStickColor(notWorkingColor);
                springSticks[2].SetColliderEnabled(false);

                state = "Middle Working";
            }
            else if (linkLength < initialDistance - tolerance)
            {
                // middle not working
                springSticks[0].SetStickColor(notWorkingColor);
                springSticks[0].SetColliderEnabled(false);

                // Ends Working
                springSticks[1].SetStickColor(workingColor);
                springSticks[1].SetColliderEnabled(true);
                springSticks[2].SetStickColor(workingColor);
                springSticks[2].SetColliderEnabled(true);

                state = "Ends Working";
            }
            else
            {
                // all not working
                for (int i = 0; i < 3; i++)
                {
                    springSticks[i].SetStickColor(notWorkingColor);
                    springSticks[i].SetColliderEnabled(false);
                }
                state = "Not Working";
            }
        }

        // Update UI
        UIManager.Instance.UpdateState(type, state, linkLength);

    }
    private void StopSpring()
    {
        for (int i = 0; i < 3; i++)
        {
            StopStick(springSticks[i]);
        }
    }

    private void CreateBamboo(int numSticks)
    {
        bambooSticks = new List<Stick>();
        for (int i = 0; i < numSticks; i++)
        {
            bambooSticks.Add(new Stick(stickPrefab, stickColliders));
        }
    }
    private void Bamboo(float linkLength, float initialDistance = 1.0f, float tolerance = 0.05f, float minlinkLength = 0.1f, float maxDeformation = 0.8f)
    {
        // not working
        if (linkLength > initialDistance + tolerance)
        {
            StopBamboo();
            // first stick
            bambooSticks[0].SetStickPosition(linkCenter);
            bambooSticks[0].SetStickRotation(Quaternion.FromToRotation(Vector3.up, linkDirection));
            bambooSticks[0].SetStickScale(new Vector3(0.05f, linkLength/2, 0.05f));
            bambooSticks[0].SetStickColor(notWorkingColor);
            bambooSticks[0].SetColliderEnabled(false);
            state = "Not Working";
        }
        // Deformation
        else if (linkLength < initialDistance && linkLength > minlinkLength)
        {
            // total deformation factor
            // deformation factor, Quadratic function
            float deformation = maxDeformation / (minlinkLength - initialDistance) * (linkLength - initialDistance);
            // deformation direction, always Face forward
            Vector3 deformDirection = Vector3.Cross(linkDirection, Vector3.Cross(linkDirection, Vector3.forward)).normalized;
            deformDirection.z = Mathf.Abs(deformDirection.z);

            for (int i = 0; i < bambooSticks.Count; i++)
            {
                float unitLength = linkLength / bambooSticks.Count;
                Vector3 unitDirection = linkDirection.normalized * (unitLength * (0.5f + i));
                Vector3 unitCenter = lefthandPosition + unitDirection;

                // for each unit calculate deformation factor
                float t = unitDirection.magnitude / linkLength;
                float offset = -4 * deformation * t * (t - 1);
                Vector3 offsetDirection = offset * deformDirection;

                // for each unit calculate position and direction
                unitCenter += offsetDirection;

                bambooSticks[i].SetStickPosition(unitCenter);
                bambooSticks[i].SetStickRotation(Quaternion.FromToRotation(Vector3.up, unitDirection));
                bambooSticks[i].SetStickScale(new Vector3(0.05f, unitLength/2, 0.05f));
                // working
                bambooSticks[i].SetStickColor(workingColor);
                bambooSticks[i].SetColliderEnabled(true);
            }
            state = "Deforming";
        }
        // Normal
        else
        {
            StopBamboo();
            // first stick
            bambooSticks[0].SetStickPosition(linkCenter);
            bambooSticks[0].SetStickRotation(Quaternion.FromToRotation(Vector3.up, linkDirection));
            bambooSticks[0].SetStickScale(new Vector3(0.05f, linkLength/2, 0.05f));
            bambooSticks[0].SetStickColor(workingColor);
            bambooSticks[0].SetColliderEnabled(true);
            state = "Normal";
        }

        // text
        UIManager.Instance.UpdateState(type, state, linkLength);
    }

    private void StopBamboo()
    {
        for (int i = 0; i < bambooSticks.Count; i++)
        {
            StopStick(bambooSticks[i]);
        }
    }
}
