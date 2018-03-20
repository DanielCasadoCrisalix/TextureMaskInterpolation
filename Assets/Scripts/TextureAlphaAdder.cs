using System;
using System.Collections.Generic;
using System.Linq;
using Crisalix.Visualization;
using UnityEngine;

public class TextureAlphaAdder : MonoBehaviour
{
    [SerializeField] private Material _testMat;
    [SerializeField] private Texture2D _smallMask;
    [SerializeField] private Texture2D _bigMask;
    [SerializeField] private Texture2D _bodyTexture;

    [SerializeField] float _minimumFactor = .3f;//one its maximum interior mask. Zero its center point
    [SerializeField]float _maximumFactor = 1f;//one its maximum exterior mask

    [SerializeField]  private float _interpolationFactor = 1.5f;

    [SerializeField] private Texture2D _result;
    private int _height;
    private int _width;
    private int _maxMaskSizeY;
    private int _maxMaskSizeX;
    private Vector2 _initialMaskPos;
    private Vector2 _endMaskPos;

    private void Start()
    {
        _width = _smallMask.width;
        _height = _smallMask.height;
        Debug.Log("_width " + _width + "  _height " + _height);
        _result = new Texture2D(_width, _height, TextureFormat.RGBA32, true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log(Time.realtimeSinceStartup);

            GetMaskedTexture(_minimumFactor, _maximumFactor);

            Debug.Log(Time.realtimeSinceStartup);
        }
    }

    private void GetMaskedTexture(float minimumFactor, float maximumFactor)
    {
        if (_bodyTexture.width != _width && _bodyTexture.height != _height)
        {
            Debug.LogError("Check mask, and bodytexture sizes. They must to be the same");
            return;
        }

        List<Color> textureColors = _bodyTexture.GetPixels(0, 0, _width, _height).ToList();
        Vector2 smallBorderMaskCenter;
        Vector2 bigBorderMaskCenter;
        List<Vector2> smallBorderMaskPositions = GetMaskBorder(_smallMask, out smallBorderMaskCenter);
        List<Vector2> bigBorderMaskPositions = GetMaskBorder(_bigMask, out bigBorderMaskCenter);

        List<Vector2> minimumInterpolatedMask;
        List<Vector2> interpolatedMask = GetInterpolateMask(bigBorderMaskPositions, smallBorderMaskPositions,
            maximumFactor,
            smallBorderMaskCenter, minimumFactor, out minimumInterpolatedMask);

        textureColors = SetTranparencies(textureColors, interpolatedMask, minimumInterpolatedMask);
        CreateMaskTexture(textureColors);
    }

    private void CreateMaskTexture(List<Color> textureColors)
    {
        _result.SetPixels(textureColors.ToArray());
        _result.Apply();
        _testMat.mainTexture = _result;
    }

    private List<Color> SetTranparencies(List<Color> textureColors, List<Vector2> interpolatedMask,
        List<Vector2> minimumInterpolatedMask)
    {
        int cntr = 0;

        for (int i = 0; i < _height; i++)
        {
            for (int j = 0; j < _width; j++)
            {
                Color color = textureColors[cntr];
                float alpha = 0;
                if (_initialMaskPos.x < j && j < _endMaskPos.x && _initialMaskPos.y < i && i < _endMaskPos.y)
                {
                    Vector2 point = new Vector2(j, i);
                    alpha = GetPixelTranparency(point, interpolatedMask, minimumInterpolatedMask);
                }

                color.a = alpha;
                textureColors[cntr] = color;
                cntr++;
            }
        }

        return textureColors;
    }

    private float GetPixelTranparency(Vector2 point, List<Vector2> interpolatedMask,
        List<Vector2> minimumInterpolatedMask)
    {
        int closestLineIndex = int.MaxValue;
        float minDifference = int.MaxValue;
        Vector2 pointOnLineProjection = Vector2.zero;

        for (int i = 0; i < interpolatedMask.Count; i++)
        {
            Vector2 pointOverLine;
            float difference =
                DistancePointToLine(point, minimumInterpolatedMask[i], interpolatedMask[i], out pointOverLine);

            if (minDifference > difference)
            {
                pointOnLineProjection = pointOverLine;
                minDifference = difference;
                closestLineIndex = i;
            }
        }

        float segmentMagnitude = (interpolatedMask[closestLineIndex] - minimumInterpolatedMask[closestLineIndex])
            .magnitude;
        float projectedToInterpolatedPoint =
            (pointOnLineProjection - interpolatedMask[closestLineIndex]).magnitude;

        float alpha = (projectedToInterpolatedPoint / segmentMagnitude);
        alpha =  Mathf.Pow(alpha, _interpolationFactor);

        return alpha;
    }

    private List<Vector2> GetInterpolateMask(List<Vector2> bigBorderMaskPositions,
        List<Vector2> smallBorderMaskPositions,
        float interpolationFactor, Vector2 smallBorderMaskCenter,
        float minimumFactor, out List<Vector2> minimumInterpolatedMask)
    {
        List<Vector2> interpolatedMask = new List<Vector2>();
        minimumInterpolatedMask = new List<Vector2>();

        float minX = int.MaxValue;
        float minY = int.MaxValue;
        float maxX = int.MinValue;
        float maxY = int.MinValue;

        for (int i = 0; i < bigBorderMaskPositions.Count; i++)
        {
            Vector2 bigPoint = bigBorderMaskPositions[i];
            float minDifference = int.MaxValue;
            Vector2 nearesPointInSmallMask = Vector2.zero;

            for (int j = 0; j < smallBorderMaskPositions.Count; j++)
            {
                Vector2 pointOverLine;

                float difference = DistancePointToLine(smallBorderMaskPositions[j], bigPoint, smallBorderMaskCenter,
                    out pointOverLine);

                if (!(minDifference > difference)) continue;
                nearesPointInSmallMask = pointOverLine;
                minDifference = difference;
            }

            Vector2 minimumPoint = Vector2.Lerp( smallBorderMaskCenter,nearesPointInSmallMask, minimumFactor);
            minimumPoint = new Vector2(Mathf.Round(minimumPoint.x), Mathf.Round(minimumPoint.y));
            minimumInterpolatedMask.Add(minimumPoint);

            Vector2 interpolatedPoint = Vector2.Lerp(nearesPointInSmallMask,bigPoint, interpolationFactor);
            interpolatedPoint = new Vector2(Mathf.Round(interpolatedPoint.x), Mathf.Round(interpolatedPoint.y));
            interpolatedMask.Add(interpolatedPoint);

            Vector3DebuggerPoints.CreateSphereInPos(minimumPoint, Color.blue);
            Vector3DebuggerPoints.CreateSphereInPos(interpolatedPoint, Color.red);

            minX = GetMin(interpolatedPoint.x, minX);
            minY = GetMin(interpolatedPoint.y, minY);
            maxX = GetMax(interpolatedPoint.x, maxX);
            maxY = GetMax(interpolatedPoint.y, maxY);
        }

        Vector2 maxim = new Vector3(maxX, maxY);
        Vector2 minim = new Vector3(minX, minY);

        _initialMaskPos = new Vector2(Mathf.Round(minim.x), Mathf.Round(minim.y));
        _endMaskPos = new Vector2(Mathf.Round(maxim.x), Mathf.Round(maxim.y));

        return interpolatedMask;
    }

    private float DistancePointToLine(Vector2 point, Vector2 linepoint, Vector2 linepoint2, out Vector2 overLine)
    {
        Vector2 pointOverLine = Math3D.ProjectPointOnLineSegment(linepoint, linepoint2, point);
        overLine = pointOverLine;
        return (pointOverLine - point).sqrMagnitude;
    }

    private List<Vector2> GetMaskBorder(Texture2D mask, out Vector2 borderMaskCenter)
    {
        List<Vector2> borderPositions = new List<Vector2>();

        List<Color> arrayTexture = mask.GetPixels(0, 0, _width, _height).ToList();

        int cntr = 0;

        float minX = int.MaxValue;
        float minY = int.MaxValue;
        float maxX = int.MinValue;
        float maxY = int.MinValue;

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (arrayTexture[cntr].Equals(Color.white))
                {
                    int indexUp = y == 0 ? (_height - 1) * _width + x : cntr - _width;
                    int indexDown = y == _height - 1 ? x : cntr + _width;
                    int indexRight = x == _width - 1 ? cntr - _width : cntr + 1;
                    int indexLeft = x == 0 ? cntr + _width : cntr - 1;

                    if (!AllAroundPixelColorsAreWhite(arrayTexture, indexUp, indexDown, indexRight, indexLeft))
                    {
                        Vector3 pos = new Vector3(x, y, 5);
                        // Vector3DebuggerPoints.CreateSphereInPos(pos, Color.yellow);
                        borderPositions.Add(pos);
                    }

                    minX = GetMin(x, minX);
                    minY = GetMin(y, minY);
                    maxX = GetMax(x, maxX);
                    maxY = GetMax(y, maxY);
                }

                cntr++;
            }
        }

        Vector2 maxim = new Vector3(maxX, maxY);
        Vector2 minim = new Vector3(minX, minY);

        borderMaskCenter = Vector2.Lerp(maxim, minim, 0.5f);
        borderMaskCenter.x = Mathf.Round(borderMaskCenter.x);
        borderMaskCenter.y = Mathf.Round(borderMaskCenter.y);
        Vector3DebuggerPoints.CreateSphereInPos(borderMaskCenter, Color.blue, "borderMaskCenter");

        return borderPositions;
    }

    private float GetMin(float diffVal, float minVal)
    {
        if (minVal > diffVal)
        {
            return diffVal;
        }

        return minVal;
    }

    private float GetMax(float diffVal, float maxVal)
    {
        if (maxVal < diffVal)
        {
            return diffVal;
        }

        return maxVal;
    }

    private bool AllAroundPixelColorsAreWhite(List<Color> arrayTexture, int indexUp, int indexDown, int indexRight,
        int indexLeft)
    {
        return IsWhite(arrayTexture[indexUp]) && IsWhite(arrayTexture[indexDown]) &&
               IsWhite(arrayTexture[indexRight]) && IsWhite(arrayTexture[indexLeft]);
    }

    private bool IsWhite(Color color)
    {
        return color.Equals(Color.white);
    }
}