using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VRVision.Toolkit.TransformAnimator
{
    public partial class TransformAnimator : AnimatorBase
    {
        #region Publics

        public bool CurrentlyAnimating { get; private set; } = false;

        public bool PlayForwardOnAwake = false;
        public bool PlayReverseOnAwake = false;
        [Tooltip("Delay in seconds applied before the start of the animation loop")]
        public float StartDelay = 0f;
        [Tooltip("Delay in seconds applied before the object bounces off the end of the path")]
        public float PingPongDelay = 0f;
        [Tooltip("Smoothing applied when the object hits an anchor. Effect only applied in PlayMode")]
        public EasingFunction.Ease EaseMode = EasingFunction.Ease.EaseInQuad;
        public bool EaseAtEveryPoint = false;

        [Tooltip("Uses the actual distance in between the anchors to average the speed and display accurate seperations in the editor timeline")]
        public bool UseTrueDistances = false;

        [SerializeReference]
        public TAConstraints Constraints = new TAConstraints()
        {
            TranslateX = true,
            TranslateY = true,
            TranslateZ = true,
            RotateX = true,
            RotateY = true,
            RotateZ = true,
            ScaleX = true,
            ScaleY = true,
            ScaleZ = true,

            UseQuaternions = false
        };

        #endregion

        #region Privates

        [SerializeField] protected TransformAnimatorVisualizer visualizer;

        [Tooltip("Start and End points of the path")]
        [SerializeField] protected Transform startTransform, endTransform;
        [Tooltip("This object is set to the position of StartingAnchor on Awake()")]
        [SerializeField] protected Transform startingAnchor;
        [Tooltip("List of points existing in between the Start and End")]
        [SerializeField] protected List<Transform> midpoints = new List<Transform>();

        [Tooltip("List of splines in the path")]
        [SerializeField] protected List<SplineSegment> splineSegments = new List<SplineSegment>();

        [Tooltip("The distance in between anchors, assuming even spacing")]
        [SerializeField] protected float evenSegmentSize = 0f;
        [Tooltip("Details on the path")]
        [SerializeField] protected Segments segments;

        #endregion

        #region Properties

        public int NumOfAnchors { get { return midpoints.Count + 2; } }
        public int NumOfMidpointAnchors { get { return midpoints.Count; } }

        #endregion

        #region Callback Functions

        protected virtual void Awake()
        {
            this.transform.localPosition = startingAnchor.transform.localPosition;
            SetTimeBasedOnAnchor(startingAnchor);

            startTransform.GetComponent<MeshRenderer>().material = visualizer.playModeMat;
            endTransform.GetComponent<MeshRenderer>().material = visualizer.playModeMat;

            foreach (Transform midPoint in midpoints)
                midPoint.GetComponent<MeshRenderer>().material = visualizer.playModeMat;

            if (PlayForwardOnAwake)
            {
                PlayForward();
            }
            else if (PlayReverseOnAwake)
            {
                PlayReverse();
            }
        }

        #endregion

        #region Animation Functions

        /// <summary>
        /// Plays forward from 0
        /// </summary>
        public void PlayForwardFromStart()
        {
            Play(0f, 1f);
        }
        /// <summary>
        /// Plays reverse from 1
        /// </summary>
        public void PlayReverseFromEnd()
        {
            Play(1f, 0f);
        }
        /// <summary>
        /// If TimeValue is smaller than 0.5f the animation will play forwards, otherwise it plays reverse
        /// </summary>
        public new void PlayToggle()
        {
            if (timeValue > 0.499999f)
            {
                PlayReverse();
            }
            else
            {
                PlayForward();
            }
        }
        protected override IEnumerator AnimateRoutine(float start, float end)
        {
            yield return new WaitForSeconds(StartDelay);
            InvokeOnPlayStarted(start);

            start = Mathf.Clamp01(start);
            end = Mathf.Clamp01(end);

            float speedMod = Mathf.Abs(end - start);

            timeValue = start;
            float t = 0f;

            Transform[] anchors = GetAllAnchors().ToArray();
            var ease = EasingFunction.GetEasingFunction(EaseMode);

            CurrentlyAnimating = true;
            if (EaseAtEveryPoint)
            {
                while (CurrentlyAnimating)
                {
                    t += Time.deltaTime / animationTime / speedMod;
                    timeValue = Mathf.Lerp(start, end, t);

                    SetPositionBasedOnTime(timeValue, anchors, ease);

                    if (t >= 1f)
                    {
                        switch (repeatMode)
                        {
                            case RepeatMode.Loop:
                                t = 0;
                                break;
                            case RepeatMode.PingPong:

                                float temp = start;
                                start = end;
                                end = temp;
                                t = 0;

                                yield return new WaitForSeconds(PingPongDelay);

                                break;
                            default:
                                CurrentlyAnimating = false;
                                break;
                        }
                    }

                    yield return null;
                }
            }
            else
            {
                while (CurrentlyAnimating)
                {
                    t += Time.deltaTime / animationTime / speedMod;
                    timeValue = ease(start, end, t);

                    SetPositionBasedOnTime(timeValue, anchors);

                    if (t >= 1f)
                    {
                        switch (repeatMode)
                        {
                            case RepeatMode.Loop:
                                t = 0;
                                break;
                            case RepeatMode.PingPong:

                                float temp = start;
                                start = end;
                                end = temp;
                                t = 0;

                                yield return new WaitForSeconds(PingPongDelay);

                                break;
                            default:
                                CurrentlyAnimating = false;
                                break;
                        }
                    }

                    yield return null;
                }
            }

            InvokeOnPlayFinished(end);
        }

        #endregion

        #region Private Functions

        // Checks if the parameter anchor starts a spline segment
        protected virtual bool CheckIfAnchorHasASpline(Transform anchor, out SplineSegment spline, out int index)
        {
            spline = new SplineSegment();
            index = 0;
            foreach (SplineSegment splineSegment in splineSegments)
            {
                if (splineSegment.Start == anchor)
                {
                    spline = splineSegment;
                    return true;
                }
                index++;
            }

            index = -1;
            return false;
        }
        // Checks if the parameter anchor starts a spline segment
        protected virtual bool CheckIfAnchorHasASpline(Transform anchor)
        {
            SplineSegment s;
            return CheckIfAnchorHasASpline(anchor, out s, out int temp);
        }

        protected virtual float CalculatePathLength()
        {
            segments = new Segments(GetAllAnchors().ToArray());

            evenSegmentSize = 1.0f / (NumOfMidpointAnchors + 1.0f);

            return segments.PathLength;
        }
        protected virtual float SetTimeBasedOnAnchor(Transform anchor)
        {
            if (anchor == startTransform)
            {
                timeValue = 0f;
                return timeValue;
            }
            else if (anchor == endTransform)
            {
                timeValue = 1f;
                return timeValue;
            }
            else
            {
                float distance = Vector3.Distance(startTransform.localPosition, midpoints[0].localPosition);
                for (int k = 0; k < midpoints.Count; k++)
                {
                    if (k > 0)
                    {
                        distance += Vector3.Distance(midpoints[k - 1].localPosition, midpoints[k].localPosition);
                    }

                    if (midpoints[k] == anchor)
                    {
                        timeValue = UseTrueDistances ?
                            distance / CalculatePathLength()
                            :
                            (k + 1f) / (midpoints.Count + 1f);
                        return timeValue;
                    }
                }
            }
            return timeValue;
        }
        protected virtual void SetPositionBasedOnTime(float time, Transform[] anchors, EasingFunction.Function easeFunction)
        {
            Vector3 position, scale, euler = startingAnchor.localEulerAngles;
            Quaternion rotation = startingAnchor.localRotation;

            Transform anchor1, anchor2;
            float lerpVal;

            if (anchors.Length == 2)
            {
                lerpVal = time;
                anchor1 = anchors[0];
                anchor2 = anchors[1];
            }
            else
            {
                if (UseTrueDistances)
                {
                    int a1 = 0;
                    float sSize = 0f;

                    if (time < segments.MidpointNormalizedPositions[0])
                    {
                        a1 = 0;
                        sSize = 0f;
                    }
                    else if (time > segments.MidpointNormalizedPositions[segments.MidpointNormalizedPositions.Length - 1])
                    {
                        a1 = anchors.Length - 2;
                        sSize = segments.MidpointNormalizedPositions[segments.MidpointNormalizedPositions.Length - 1];
                    }
                    else
                    {
                        for (int k = segments.MidpointNormalizedPositions.Length - 1; k >= 0; k--)
                        {
                            if (time > segments.MidpointNormalizedPositions[k])
                            {
                                a1 = k + 1;
                                sSize = segments.MidpointNormalizedPositions[k];
                                break;
                            }
                        }
                    }

                    int a2 = a1 + 1;
                    lerpVal = (time - sSize) / segments.SegmentNormalizedSizes[a1];

                    anchor1 = anchors[a1];
                    anchor2 = anchors[a2];
                }
                else
                {
                    float diff = time / evenSegmentSize;

                    int a1 = Mathf.FloorToInt(diff);
                    int a2 = Mathf.CeilToInt(diff);

                    if (a1 == a2) { return; }
                    if (a1 >= anchors.Length || a2 >= anchors.Length) { return; }
                    if (a1 < 0 || a2 < 0) { return; }

                    lerpVal = (time - (a1 * evenSegmentSize)) / evenSegmentSize;

                    anchor1 = anchors[a1];
                    anchor2 = anchors[a2];
                }
            }

            #region LERP

            SplineSegment spline;
            if (CheckIfAnchorHasASpline(anchor1, out spline, out int temp))
            {
                if (this.transform.parent)
                {
                    position = this.transform.parent.InverseTransformPoint(spline.GetPositionFromTime(lerpVal));
                }
                else
                {
                    position = spline.GetPositionFromTime(lerpVal);
                }
            }
            else
            {
                position.x = easeFunction(anchor1.localPosition.x, anchor2.localPosition.x, lerpVal);
                position.y = easeFunction(anchor1.localPosition.y, anchor2.localPosition.y, lerpVal);
                position.z = easeFunction(anchor1.localPosition.z, anchor2.localPosition.z, lerpVal);
            }

            if (Constraints.UseQuaternions) rotation = Quaternion.Lerp(anchor1.localRotation, anchor2.localRotation, lerpVal);
            else
            {
                euler.x = easeFunction(anchor1.localEulerAngles.x, anchor2.localEulerAngles.x, lerpVal);
                euler.y = easeFunction(anchor1.localEulerAngles.y, anchor2.localEulerAngles.y, lerpVal);
                euler.z = easeFunction(anchor1.localEulerAngles.z, anchor2.localEulerAngles.z, lerpVal);
            }

            scale.x = easeFunction(anchor1.localScale.x, anchor2.localScale.x, lerpVal);
            scale.y = easeFunction(anchor1.localScale.y, anchor2.localScale.y, lerpVal);
            scale.z = easeFunction(anchor1.localScale.z, anchor2.localScale.z, lerpVal);

            #endregion

            #region CONSTRAINTS

            if (!Constraints.UseQuaternions)
            {
                euler.x = Constraints.RotateX ? euler.x : startingAnchor.localEulerAngles.x;
                euler.y = Constraints.RotateX ? euler.y : startingAnchor.localEulerAngles.y;
                euler.z = Constraints.RotateX ? euler.z : startingAnchor.localEulerAngles.z;
            }

            position.x = Constraints.TranslateX ? position.x : startingAnchor.localPosition.x;
            position.y = Constraints.TranslateY ? position.y : startingAnchor.localPosition.y;
            position.z = Constraints.TranslateZ ? position.z : startingAnchor.localPosition.z;

            scale.x = Constraints.ScaleX ? scale.x : startingAnchor.localScale.x;
            scale.y = Constraints.ScaleY ? scale.y : startingAnchor.localScale.y;
            scale.z = Constraints.ScaleZ ? scale.z : startingAnchor.localScale.z;

            #endregion

            this.transform.localPosition = position;
            if (Constraints.UseQuaternions) this.transform.localRotation = rotation;
            else this.transform.localEulerAngles = euler;
            this.transform.localScale = scale;
        }
        protected virtual void SetPositionBasedOnTime(float time, Transform[] anchors)
        {
            Vector3 position, scale, euler = startingAnchor.localEulerAngles;
            Quaternion rotation = startingAnchor.localRotation;

            Transform anchor1, anchor2;
            float lerpVal;

            if (anchors.Length == 2)
            {
                lerpVal = time;
                anchor1 = anchors[0];
                anchor2 = anchors[1];
            }
            else
            {
                if (UseTrueDistances)
                {
                    int a1 = 0;
                    float sSize = 0f;

                    if (time < segments.MidpointNormalizedPositions[0])
                    {
                        a1 = 0;
                        sSize = 0f;
                    }
                    else if (time > segments.MidpointNormalizedPositions[segments.MidpointNormalizedPositions.Length - 1])
                    {
                        a1 = anchors.Length - 2;
                        sSize = segments.MidpointNormalizedPositions[segments.MidpointNormalizedPositions.Length - 1];
                    }
                    else
                    {
                        for (int k = segments.MidpointNormalizedPositions.Length - 1; k >= 0; k--)
                        {
                            if (time > segments.MidpointNormalizedPositions[k])
                            {
                                a1 = k + 1;
                                sSize = segments.MidpointNormalizedPositions[k];
                                break;
                            }
                        }
                    }

                    int a2 = a1 + 1;
                    lerpVal = (time - sSize) / segments.SegmentNormalizedSizes[a1];

                    anchor1 = anchors[a1];
                    anchor2 = anchors[a2];
                }
                else
                {
                    float diff = time / evenSegmentSize;

                    int a1 = Mathf.FloorToInt(diff);
                    int a2 = Mathf.CeilToInt(diff);

                    if (a1 == a2) { return; }
                    if (a1 >= anchors.Length || a2 >= anchors.Length) { return; }
                    if (a1 < 0 || a2 < 0) { return; }

                    lerpVal = (time - (a1 * evenSegmentSize)) / evenSegmentSize;

                    anchor1 = anchors[a1];
                    anchor2 = anchors[a2];
                }
            }

            #region LERP

            SplineSegment spline;
            if (CheckIfAnchorHasASpline(anchor1, out spline, out int temp))
            {
                if (this.transform.parent)
                {
                    position = this.transform.parent.InverseTransformPoint(spline.GetPositionFromTime(lerpVal));
                }
                else
                {
                    position = spline.GetPositionFromTime(lerpVal);
                }
            }
            else
            {
                position = Vector3.Lerp(anchor1.localPosition, anchor2.localPosition, lerpVal);
            }

            if (Constraints.UseQuaternions) rotation = Quaternion.Lerp(anchor1.localRotation, anchor2.localRotation, lerpVal);
            else
            {
                euler = Vector3.Lerp(anchor1.localEulerAngles, anchor2.localEulerAngles, lerpVal);
            }

            scale = Vector3.Lerp(anchor1.localScale, anchor2.localScale, lerpVal);

            #endregion

            #region CONSTRAINTS

            if (!Constraints.UseQuaternions)
            {
                euler.x = Constraints.RotateX ? euler.x : startingAnchor.localEulerAngles.x;
                euler.y = Constraints.RotateX ? euler.y : startingAnchor.localEulerAngles.y;
                euler.z = Constraints.RotateX ? euler.z : startingAnchor.localEulerAngles.z;
            }

            position.x = Constraints.TranslateX ? position.x : startingAnchor.localPosition.x;
            position.y = Constraints.TranslateY ? position.y : startingAnchor.localPosition.y;
            position.z = Constraints.TranslateZ ? position.z : startingAnchor.localPosition.z;

            scale.x = Constraints.ScaleX ? scale.x : startingAnchor.localScale.x;
            scale.y = Constraints.ScaleY ? scale.y : startingAnchor.localScale.y;
            scale.z = Constraints.ScaleZ ? scale.z : startingAnchor.localScale.z;

            #endregion

            this.transform.localPosition = position;
            if (Constraints.UseQuaternions) this.transform.localRotation = rotation;
            else this.transform.localEulerAngles = euler;
            this.transform.localScale = scale;
        }
        #endregion

        #region Public Functions

        public override void SetValue(float t)
        {
            base.SetValue(t);
            SetPositionBasedOnTime(t, GetAllAnchors().ToArray(), EasingFunction.GetEasingFunction(EaseMode));
        }
        public List<Transform> GetAllAnchors()
        {
            List<Transform> all = new List<Transform>();
            all.Add(startTransform);
            all.AddRange(midpoints);
            all.Add(endTransform);

            return all;
        }
        public void SetToState(int state)
        {
            if (state == 0)
            {
                this.transform.localPosition = startTransform.localPosition;
                SetTimeBasedOnAnchor(startTransform);
            }
            else if (state > midpoints.Count)
            {
                this.transform.localPosition = endTransform.localPosition;
                SetTimeBasedOnAnchor(endTransform);
            }
            else
            {
                this.transform.localPosition = midpoints[state - 1].localPosition;
                SetTimeBasedOnAnchor(midpoints[state - 1]);
            }
        }
        public void SetToStart()
        {
            this.transform.localPosition = startTransform.localPosition;
            SetTimeBasedOnAnchor(startTransform);
        }
        public void SetToEnd()
        {
            this.transform.localPosition = endTransform.localPosition;
            SetTimeBasedOnAnchor(endTransform);
        }

        #endregion

        #region Supporting Objects

        [System.Serializable]
        public class TAConstraints
        {
            public bool TranslateX, TranslateY, TranslateZ;
            public bool RotateX, RotateY, RotateZ;
            public bool ScaleX, ScaleY, ScaleZ;

            public bool UseQuaternions;
        }

        [System.Serializable]
        public struct Segments
        {
            public float[] Sizes;
            public float[] MidpointNormalizedPositions;
            public float[] SegmentNormalizedSizes;
            public float PathLength;

            public Segments(Transform[] anchors)
            {
                Sizes = new float[anchors.Length - 1];
                MidpointNormalizedPositions = new float[anchors.Length - 2];
                SegmentNormalizedSizes = new float[anchors.Length - 1];

                PathLength = 0f;

                for (int k = 0; k < Sizes.Length; k++)
                {
                    PathLength += Sizes[k] = Vector3.Distance(anchors[k].localPosition, anchors[k + 1].localPosition);
                }

                float lastRS = 0f;
                for (int k = 0; k < Sizes.Length; k++)
                {
                    SegmentNormalizedSizes[k] = Sizes[k] / PathLength;
                    if (k < Sizes.Length - 1)
                    {
                        MidpointNormalizedPositions[k] = lastRS + SegmentNormalizedSizes[k];
                        lastRS = MidpointNormalizedPositions[k];
                    }
                }
            }
        }

        [System.Serializable]
        public struct SplineSegment
        {
            public Transform Start, End;
            [SerializeField] public Vector3 Handle1, Handle2;
            [SerializeField] public bool SingleHandle;

            public SplineSegment(Transform start, Transform end, Vector3 handle1, Vector3 handle2)
            {
                Start = start; End = end;
                Handle1 = handle1; Handle2 = handle2;
                SingleHandle = false;
            }
            public SplineSegment(Transform start, Transform end, Vector3 handle1)
            {
                Start = start; End = end;
                Handle1 = handle1; Handle2 = Vector3.zero;
                SingleHandle = true;
            }
            public Vector3 GetPositionFromTime(float t)
            {
                List<Vector3> points = new List<Vector3>();
                points.Add(Start.position);
                points.Add(End.position);
                points.Add(Handle1);
                if (!SingleHandle) points.Add(Handle2);

                return Bezier.Curve(points, t);
            }
        }

        #endregion
    }
}
