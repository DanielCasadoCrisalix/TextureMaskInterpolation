using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crisalix.Visualization;
using UnityEngine;

public class TextureAlphaAdder : MonoBehaviour
{
    [SerializeField] private Material _testMat;
    [SerializeField] private Texture2D _smallMask;
    [SerializeField] private Texture2D _bigMask;
    [SerializeField] private Texture2D _bodyTexture;

    [SerializeField] float _maximumFactor = 1f; //one its maximum exterior mask

    [SerializeField] private float _interpolationFactor = 1f;

    [SerializeField] private Texture2D _result;
    private int _height;
    private int _width;
    private Vector2 _initialMaskPos;
    private Vector2 _endMaskPos;

    private struct IntVector2
    {
        public int X;
        public int Y;

        public IntVector2(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    private List<IntVector2> _smallMaskSurfacePoints;

    private void Start()
    {
        _width = _smallMask.width;
        _height = _smallMask.height;
        Debug.Log(Application.dataPath + " _width " + _width + "  _height " + _height);
        _result = new Texture2D(_width, _height, TextureFormat.RGBA32, true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log(Time.realtimeSinceStartup);

            GetMaskedTexture(_maximumFactor);

            Debug.Log(Time.realtimeSinceStartup);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            ExportTextureToFile();
        }
    }

    private void ExportTextureToFile()
    {
        byte[] bytes = _result.EncodeToPNG();
        string path = Application.dataPath + "Mask_" + _maximumFactor + "_" + _interpolationFactor + ".png";
        File.WriteAllBytes(path, bytes);
    }

    private void GetMaskedTexture(float maximumFactor)
    {
        if (_bodyTexture.width != _width && _bodyTexture.height != _height)
        {
            Debug.LogError("Check mask, and bodytexture sizes. They must to be the same");
            return;
        }

        _smallMaskSurfacePoints = new List<IntVector2>();

        List<Color> textureColors = _bodyTexture.GetPixels(0, 0, _width, _height).ToList();
        Vector2 minPoint;
        Vector2 maxPoint;

        List<Vector2> smallBorderMaskPositions =
            GetMaskBorder(_smallMask, out minPoint, out maxPoint, _smallMaskSurfacePoints);
        Vector2 smallBorderMaskCenter = Vector2.Lerp(minPoint, maxPoint, 0.5f);

        List<Vector2> bigBorderMaskPositions = GetMaskBorder(_bigMask, out minPoint, out maxPoint, null);

        List<Vector2> minimumInterpolatedMask;
        List<Vector2> interpolatedMask = GetInterpolateMask(bigBorderMaskPositions, smallBorderMaskPositions,
            maximumFactor,
            smallBorderMaskCenter, out minimumInterpolatedMask);
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
                Color color = textureColors[cntr];//Color.white;
                float alpha = 0;

                if (_initialMaskPos.x < j && j < _endMaskPos.x && _initialMaskPos.y < i && i < _endMaskPos.y)
                {
                    IntVector2 point = new IntVector2(j, i);
                    alpha = GetPixelTranparency(point, interpolatedMask, minimumInterpolatedMask);
                }

                color.a = alpha;
                textureColors[cntr] = color;
                cntr++;
            }
        }

        return textureColors;
    }

    private float GetPixelTranparency(IntVector2 point, List<Vector2> interpolatedMask,
        List<Vector2> minimumInterpolatedMask)
    {
        // if (CheckIfPointInsideMinimumMask(point, _smallMaskSurfacePoints)) return 1;

        Vector2 pointFloatV2 = new Vector2(point.X, point.Y);
        int closestLineIndex = int.MaxValue;
        float minDifference = int.MaxValue;
        Vector2 pointOnLineProjection = Vector2.zero;

        for (int i = 0; i < interpolatedMask.Count; i++)
        {
            Vector2 pointOverLine;
            float difference =
                DistancePointToLine(pointFloatV2, minimumInterpolatedMask[i], interpolatedMask[i], out pointOverLine);

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
        alpha = 1-Mathf.Pow(1-alpha, _interpolationFactor);

        return alpha;
    }

    private bool CheckIfPointInsideMinimumMask(IntVector2 point, List<IntVector2> surface)
    {
        for (int i = 0; i < surface.Count; i++)
        {
            IntVector2 surfacePoint = surface[i];

            if (surfacePoint.X == point.X && surfacePoint.Y == point.Y)
            {
                return true;
            }
        }

        return false;
    }

    private List<Vector2> GetInterpolateMask(List<Vector2> bigBorderMaskPositions,
        List<Vector2> smallBorderMaskPositions,
        float interpolationFactor, Vector2 smallBorderMaskCenter, out List<Vector2> minimumInterpolatedMask)
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

            Vector2 minimumPoint = nearesPointInSmallMask;
            minimumPoint = new Vector2(Mathf.Round(minimumPoint.x), Mathf.Round(minimumPoint.y));
            minimumInterpolatedMask.Add(minimumPoint);

            Vector2 interpolatedPoint = Vector2.Lerp(nearesPointInSmallMask, bigPoint, interpolationFactor);
            interpolatedPoint = new Vector2(Mathf.Round(interpolatedPoint.x), Mathf.Round(interpolatedPoint.y));
            interpolatedMask.Add(interpolatedPoint);

            Debug.DrawLine(interpolatedPoint, minimumPoint, Color.green, 20);

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

    private bool DistancePointToLineSegmentCheck(Vector2 point, Vector2 linepoint, Vector2 linepoint2,
        out Vector2 overLine)
    {
        return Math3D.ProjectPointOnLineSegmentCheck(linepoint, linepoint2, point, out overLine);
    }

    private List<Vector2> GetMaskBorder(Texture2D mask, out Vector2 minPoint, out Vector2 maxPoint,
        List<IntVector2> surfacePoints)
    {
        List<Vector2> borderPositions = new List<Vector2>();
        List<IntVector2> surfaceXy = new List<IntVector2>();

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
                        borderPositions.Add(pos);
                    }

                    minX = GetMin(x, minX);
                    minY = GetMin(y, minY);
                    maxX = GetMax(x, maxX);
                    maxY = GetMax(y, maxY);
                    surfaceXy.Add(new IntVector2(x, y));
                }

                cntr++;
            }
        }

        if (surfacePoints != null)
        {
            surfacePoints.AddRange(surfaceXy);
        }

        minPoint = new Vector3(minX, minY);
        maxPoint = new Vector3(maxX, maxY);

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