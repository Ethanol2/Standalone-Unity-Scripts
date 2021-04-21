// Script written by EthanASC
// https://www.fiverr.com/ethanasc

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Serializable]
    class BGLayer
    {
        public Transform spriteObj;         // Reference to the transform of the background layer
        float spriteWidth;                  // Width of the sprite
        float spriteHeight;                 // Height of the sprite
        public Vector2 parallaxStrength;    // Strength of the parallax for this layer
        public Vector3 position             // Reference to the localPosition value of the layer
        {
            get { return spriteObj.localPosition; }
            set { spriteObj.localPosition = value; }
        }
        public Vector3 scale                // Reference to the localScale value of the layer
        {
            get { return spriteObj.localScale; }
            set { spriteObj.localScale = value; }
        }
        Vector3 referencePos;               // The position the parallax effect references
        public bool verticalParallax;              // Bool for whether vertical parallax is enabled
        public bool horizontalParallax;            // Bool for whether horizontal parallax is enabled
        public BGLayer(Transform a_spriteObject, Vector2 a_parallaxStrength,
                        bool a_horiParallax, bool a_extendHori,
                        bool a_vertParallax, bool a_extendVert)
        {
            // Set the layer object reference and get the sprite width
            spriteObj = a_spriteObject;
            spriteWidth = spriteObj.GetComponent<SpriteRenderer>().bounds.size.x;
            spriteHeight = spriteObj.GetComponent<SpriteRenderer>().bounds.size.y;

            // If extend sides is enabled, extend the width of the sprite to 3 times the original width
            if (a_extendHori || a_extendVert)
            {
                spriteObj.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Tiled;
                spriteObj.GetComponent<SpriteRenderer>().size = new Vector2(
                    a_extendHori ? (spriteWidth * 3f) / scale.x : spriteWidth / scale.x,
                    a_extendVert ? (spriteHeight * 3f) / scale.y : spriteHeight / scale.y
                    );
            }

            // Sets the parallax strength and reference position
            parallaxStrength = a_parallaxStrength;
            referencePos = spriteObj.localPosition;

            // Sets the vertical and horizontal parallax toggles
            verticalParallax = a_vertParallax;
            horizontalParallax = a_horiParallax;
        }
        // Calculates the new position with parallax
        public void updatePosition(Vector3 distance, bool horizontalRepeat, bool verticalRepeat)
        {
            // The new position is calculated using the camera distance times the parallax strength, added to the reference position
            Vector3 move = new Vector3(
                horizontalParallax ? distance.x * parallaxStrength.x : 0f,
                verticalParallax ? distance.y * parallaxStrength.y : 0f,
                0f);

            position = move + referencePos;

            // If the layer needs to catch up, the sprite width will be added to, or removed from, the reference position
            if (horizontalRepeat)
            {
                if (distance.x * (1f - parallaxStrength.x) > referencePos.x + spriteWidth)
                {
                    referencePos.x += spriteWidth;
                }
                else if (distance.x * (1f - parallaxStrength.x) < referencePos.x - spriteWidth)
                {
                    referencePos.x -= spriteWidth;
                }
            }
            if (verticalRepeat)
            {
                if (distance.y * (1f - parallaxStrength.y) > referencePos.y + spriteHeight)
                {
                    referencePos.y += spriteHeight;
                }
                else if (distance.y * (1f - parallaxStrength.y) < referencePos.y - spriteHeight)
                {
                    referencePos.y -= spriteHeight;
                }
            }
            return;
        }
        // Calculates the new position with parallax using linear interpolation smoothing
        public void updatePositionSmooth(Vector3 distance, float smoothing, bool horizontalRepeat, bool verticalRepeat)
        {
            // Calculates the new position by running the camera distance times the parallax strengh, plus the reference position
            // through the linear interpolation function
            Vector3 move = new Vector3(
                horizontalParallax ?
                Mathf.Lerp(position.x, (distance.x * parallaxStrength.x) + referencePos.x, smoothing * Time.deltaTime)
                : referencePos.x,

                verticalParallax ?
                Mathf.Lerp(position.y, (distance.y * parallaxStrength.y) + referencePos.y, smoothing * Time.deltaTime)
                : referencePos.y,

                referencePos.z);

            position = move;

            // If the layer needs to catch up, the sprite width will be added to, or removed from, the reference position
            // and the current position
            // The current position is modified to avoid smoothing issues
            if (horizontalRepeat)
            {
                if (distance.x * (1f - parallaxStrength.x) > referencePos.x + spriteWidth)
                {
                    referencePos.x += spriteWidth;
                    position += new Vector3(spriteWidth, 0f, 0f);
                }
                else if (distance.x * (1f - parallaxStrength.x) < referencePos.x - spriteWidth)
                {
                    referencePos.x -= spriteWidth;
                    position += new Vector3(-spriteWidth, 0f, 0f);
                }
            }
            if (verticalRepeat)
            {
                if (distance.y * (1f - parallaxStrength.y) > referencePos.y + spriteHeight)
                {
                    referencePos.y += spriteHeight;
                    position += new Vector3(0f, spriteHeight, 0f);
                }
                else if (distance.y * (1f - parallaxStrength.y) < referencePos.y - spriteHeight)
                {
                    referencePos.y -= spriteHeight;
                    position -= new Vector3(0f, spriteHeight, 0f);
                }
            }
            return;
        }
        // Seter for the position reference
        public void setPosition(Vector3 newPos)
        {
            position = newPos;
            return;
        }
    }

    [Header("Setup")]
    [Tooltip("Fill field with the camera that follows the player")]
    public Transform mainCamera;
    [Space]
    [Tooltip("Use linear interpolation smoothing on background movement. Used to hide small movements made by the camera")]
    public bool useSmoothing = true;
    [Tooltip("Smoothing strength")]
    public float smoothing = 7f;
    [Space]
    [Tooltip("If toggled on, the script will apply parallax on the x axis")]
    public bool useSideParallax = true;
    [Tooltip("Extend the background layers automatically")]
    public bool applyWideBackgrounds = true;
    [Tooltip("If toggled on, the script will auto assign the paralax strenghs to the background layers")]
    public bool autoApplySideParallax = true;
    [Tooltip("If toggled on, the background will repeat to keep up with the camera horizontally. This parameter can be toggled at runtime")]
    public bool repeatBackgroundOnSide = true;
    [Range(0f, 1f)]
    [Tooltip("The minimum parallax strength. Closer to 0 means the closest background layer will move less")]
    public float minSideParallax = 0f;
    [Range(0f, 1f)]
    [Tooltip("The maximum parallax strength. Closer to 1 means the farthest background layer will move more")]
    public float maxSideParallax = 0.8f;
    [Tooltip("If Auto Apply Side Parallax is toggled off, set the manual values here")]
    public float[] manualSideParallax;
    [Space]
    [Tooltip("If toggled on, the script will apply parallax to the background on the Y axis")]
    public bool useUpParallax = true;
    [Tooltip("If toggled on, the script will extend the background layers vertically")]
    public bool applyTallBackgrounds = false;

    [Tooltip("If toggled on, the script will auto assign the paralax strenghs to the background layers")]
    public bool autoApplyUpParallax = true;
    [Tooltip("If toggled on, the background will repeat to keep up with the camera vertically. This parameter can be toggled at runtime")]
    public bool repeatBackgroundOnUp = false;
    [Range(0f, 1f)]
    [Tooltip("The minimum parallax strength. Closer to 0 means the closest background layer will move less")]
    public float minUpParallax = 0f;
    [Range(0f, 1f)]
    [Tooltip("The maximum parallax strength. Closer to 1 means the farthest background layer will move more")]
    public float maxUpParallax = 0.8f;
    [Tooltip("If Auto Apply Up Parallax is toggled off, set the manual values here")]
    public float[] manualUpParallax;
    [Header("Debug")]
    [SerializeField]
    [Tooltip("List of the background layers and paralax strengths")]
    List<BGLayer> backgroundLayers = new List<BGLayer>();
    Vector3 startPosition;  // Camera start position

    // Start is called before the first frame update
    void Start()
    {
        // Array for holding the vertical parallax strengths
        float[] vertParallax = new float[this.transform.childCount];
        if (autoApplyUpParallax)
        {
            // If auto applying the parallax, divide the parallax range by the amount of background layers and apply them to the layers
            float parallaxDecrement = (maxUpParallax - minUpParallax) / this.transform.childCount;
            for (int k = 0; k < vertParallax.Length; k++)
            {
                vertParallax[k] = maxUpParallax - (parallaxDecrement * k);
            }
        }
        else
        {
            // If using manual parallax, copy the manual array to the vertical parallax array
            vertParallax = manualUpParallax;
        }
        if (autoApplySideParallax)
        {
            // If the horizontal parallax is set to auto, divide the range by the number 
            // of background layers and increment the strength using that value
            float parallaxDecrement = (maxSideParallax - minSideParallax) / this.transform.childCount;
            float parallax = maxSideParallax - parallaxDecrement;
            int i = 0;
            foreach (Transform child in this.transform)
            {
                backgroundLayers.Add(new BGLayer(child, new Vector2(parallax, vertParallax[i]),
                                                useSideParallax, applyWideBackgrounds, useUpParallax, applyTallBackgrounds));
                parallax -= parallaxDecrement;
                i++;
            }
        }
        else
        {
            // If horizontal parallax is set to manual, apply the manual array
            int i = 0;
            foreach (Transform child in this.transform)
            {
                backgroundLayers.Add(new BGLayer(child, new Vector2(manualSideParallax[i], vertParallax[i]),
                                                useSideParallax, applyWideBackgrounds, useUpParallax, applyTallBackgrounds));
                i++;
            }
        }

        // Set the start position
        startPosition = mainCamera.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // If smoothing is on, use the smoothing position function, else use the regular position function
        if (useSmoothing)
        {
            foreach (BGLayer layer in backgroundLayers)
            {
                layer.updatePositionSmooth(mainCamera.position - startPosition, smoothing, repeatBackgroundOnSide, repeatBackgroundOnUp);
            }
        }
        else
        {
            foreach (BGLayer layer in backgroundLayers)
            {
                layer.updatePosition(mainCamera.position - startPosition, repeatBackgroundOnSide, repeatBackgroundOnUp);
            }
        }
    }
}