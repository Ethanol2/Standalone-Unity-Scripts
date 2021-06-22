﻿// Programmed by Ethan Colucci
// This script abstracts gameobject transforms

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UXEffects : MonoBehaviour
{
    struct UI_Transform
    {
        public GameObject element;      // Ref to Gameobject being transformed
        public Vector3 target;          // Target vector
        public float timeScale;         // Speed of transformation
        public float tolerance;         // Distance at which the transformation is considered complete
    }

    List<UI_Transform> moveList = new List<UI_Transform>();     // Queue for elements translating
    List<UI_Transform> scaleList = new List<UI_Transform>();    // Queue for elements scaling
    List<UI_Transform> rotList = new List<UI_Transform>();      // Queue for elements rotating
    public float defaultTime = 5f;
    public float defaultTolerance = 0.01f;
    public int listCount;                                       // Total count for all lists
    public float lastDeltaTime;                                 // If the timescale is set to 0, use the last delta time

    // Use this for initialization
    void Start()
    {
        listCount = moveList.Count + scaleList.Count + rotList.Count;
        lastDeltaTime = Time.deltaTime;
    }

    void Update()
    {
        // Check timescale
        lastDeltaTime = Time.timeScale > 0 ? Time.deltaTime : lastDeltaTime;

        // Execute scaling
        for (int k = 0; k < scaleList.Count; k++)
        {
            scaleList[k].element.transform.localScale =
                Vector3.Lerp(scaleList[k].element.transform.localScale, scaleList[k].target, scaleList[k].timeScale * lastDeltaTime);

            if (Mathf.Abs(scaleList[k].element.transform.localScale.x - scaleList[k].target.x) < moveList[k].tolerance &&
                Mathf.Abs(scaleList[k].element.transform.localScale.y - scaleList[k].target.y) < moveList[k].tolerance &&
                Mathf.Abs(scaleList[k].element.transform.localScale.z - scaleList[k].target.z) < moveList[k].tolerance)
            {
                scaleList[k].element.transform.localScale = scaleList[k].target;
                scaleList.RemoveAt(k);
                k--;
            }
        }

        // Execute translations
        for (int k = 0; k < moveList.Count; k++)
        {
            moveList[k].element.transform.localPosition =
                Vector3.Lerp(moveList[k].element.transform.localPosition, moveList[k].target, moveList[k].timeScale * lastDeltaTime);

            if (Mathf.Abs(moveList[k].element.transform.localPosition.x - moveList[k].target.x) < moveList[k].tolerance &&
                Mathf.Abs(moveList[k].element.transform.localPosition.y - moveList[k].target.y) < moveList[k].tolerance &&
                Mathf.Abs(moveList[k].element.transform.localPosition.z - moveList[k].target.z) < moveList[k].tolerance)
            {
                moveList[k].element.transform.localPosition = moveList[k].target;
                moveList.RemoveAt(k);
                k--;
            }
        }

        // Execute rotations
        for (int k = 0; k < rotList.Count; k++)
        {
            rotList[k].element.transform.localRotation =
                Quaternion.Lerp(rotList[k].element.transform.localRotation, Quaternion.Euler(rotList[k].target), rotList[k].timeScale * lastDeltaTime);

            if (Mathf.Abs(rotList[k].element.transform.localEulerAngles.x - rotList[k].target.x) < rotList[k].tolerance &&
                Mathf.Abs(rotList[k].element.transform.localEulerAngles.y - rotList[k].target.y) < rotList[k].tolerance &&
                Mathf.Abs(rotList[k].element.transform.localEulerAngles.z - rotList[k].target.z) < rotList[k].tolerance)
            {
                rotList[k].element.transform.localRotation = Quaternion.Euler(rotList[k].target);
                rotList.RemoveAt(k);
                k--;
            }
        }

        listCount = moveList.Count + scaleList.Count + rotList.Count;

    }

    public void scaleElement(GameObject a_element, Vector3 a_scale)
    {
        for (int k = 0; k < scaleList.Count; k++)
        {
            if (scaleList[k].element == a_element)
            {
                UI_Transform replace;
                replace.element = a_element;
                replace.target = a_scale;
                replace.timeScale = defaultTime;
                replace.tolerance = defaultTolerance;
                scaleList[k] = replace;
                return;
            }
        }

        UI_Transform temp;
        temp.element = a_element;
        temp.target = a_scale;
        temp.timeScale = defaultTime;
        temp.tolerance = defaultTolerance;
        scaleList.Add(temp);
        return;
    }

    public void moveElement(GameObject a_element, Vector3 a_position)
    {
        if (moveList.Count > 0)
        {
            for (int k = 0; k < moveList.Count; k++)
            {
                if (moveList[k].element == a_element)
                {
                    UI_Transform replace;
                    replace.element = a_element;
                    replace.target = a_position;
                    replace.timeScale = defaultTime;
                    replace.tolerance = defaultTolerance;
                    moveList[k] = replace;
                    return;
                }
            }
        }

        UI_Transform temp;
        temp.element = a_element;
        temp.target = a_position;
        temp.timeScale = defaultTime;
        temp.tolerance = defaultTolerance;
        moveList.Add(temp);
        return;
    }

    public void rotateElement(GameObject a_element, Vector3 a_rot)
    {
        for (int k = 0; k < rotList.Count; k++)
        {
            if (rotList[k].element == a_element)
            {
                UI_Transform replace;
                replace.element = a_element;
                replace.target = a_rot;
                replace.timeScale = defaultTime;
                replace.tolerance = defaultTolerance;
                rotList[k] = replace;
                return;
            }
        }

        UI_Transform temp;
        temp.element = a_element;
        temp.target = a_rot;
        temp.timeScale = defaultTime;
        temp.tolerance = defaultTolerance;
        rotList.Add(temp);
        return;
    }

    public void scaleElement(GameObject a_element, Vector3 a_scale, float a_time, float a_tolerance)
    {
        for (int k = 0; k < scaleList.Count; k++)
        {
            if (scaleList[k].element == a_element)
            {
                UI_Transform replace;
                replace.element = a_element;
                replace.target = a_scale;
                replace.timeScale = a_time;
                replace.tolerance = a_tolerance;
                scaleList[k] = replace;
                return;
            }
        }

        UI_Transform temp;
        temp.element = a_element;
        temp.target = a_scale;
        temp.timeScale = a_time;
        temp.tolerance = a_tolerance;
        scaleList.Add(temp);
        return;
    }

    public void moveElement(GameObject a_element, Vector3 a_position, float a_time, float a_tolerance)
    {
        if (moveList.Count > 0)
        {
            for (int k = 0; k < moveList.Count; k++)
            {
                if (moveList[k].element == a_element)
                {
                    UI_Transform replace;
                    replace.element = a_element;
                    replace.target = a_position;
                    replace.timeScale = a_time;
                    replace.tolerance = a_tolerance;
                    moveList[k] = replace;
                    return;
                }
            }
        }

        UI_Transform temp;
        temp.element = a_element;
        temp.target = a_position;
        temp.timeScale = a_time;
        temp.tolerance = a_tolerance;
        moveList.Add(temp);
        return;
    }

    public void rotateElement(GameObject a_element, Vector3 a_rot, float a_time, float a_tolerance)
    {
        for (int k = 0; k < rotList.Count; k++)
        {
            if (rotList[k].element == a_element)
            {
                UI_Transform replace;
                replace.element = a_element;
                replace.target = a_rot;
                replace.timeScale = a_time;
                replace.tolerance = a_tolerance;
                rotList[k] = replace;
                return;
            }
        }

        UI_Transform temp;
        temp.element = a_element;
        temp.target = a_rot;
        temp.timeScale = a_time;
        temp.tolerance = a_tolerance;
        rotList.Add(temp);
        return;
    }

}
