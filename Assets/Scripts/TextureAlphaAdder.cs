using System.Collections.Generic;
using System.Linq;
using Crisalix.Visualization;
using UnityEngine;

public class TextureAlphaAdder : MonoBehaviour
{
    [SerializeField] private Material testMat;
    [SerializeField] private Texture2D _smallMask;
    [SerializeField] private Texture2D _bigMask;
    [SerializeField] private Texture2D _bodyTexture;

    float _minimumFactor = 0.5f;
    float _maximumFactor = 0.3f;


    [SerializeField] private Texture2D _result;
    private int _height;
    private int _width;
    private List<float> _maskTransparencies;
    private List<Vector2> _initialMaskPoints;
    private List<Vector2> _fixedAlphaPoints;

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
        _initialMaskPoints = new List<Vector2>();

        List<Color> textureColors = _bodyTexture.GetPixels(0, 0, _width, _height).ToList();

        List<Vector2> smallBorderMaskPositions = GetMaskBorder(_smallMask);
        List<Vector2> bigBorderMaskPositions = GetMaskBorder(_bigMask);

        Vector2 smallBorderMaskCenter = GetMaskCenter(smallBorderMaskPositions);

        List<Vector2> minimumInterpolatedMask;

        List<Vector2> interpolatedMask = GetNewInterpolateMask(bigBorderMaskPositions, smallBorderMaskPositions,
            maximumFactor,
            smallBorderMaskCenter, minimumFactor, out minimumInterpolatedMask);

        textureColors = SetTranparencies(textureColors, interpolatedMask, minimumInterpolatedMask);
        CreateMaskTexture(textureColors);
    }

    private void CreateMaskTexture(List<Color> textureColors)
    {
        _result.SetPixels(textureColors.ToArray());
        _result.Apply();
        testMat.mainTexture = _result;
    }

    private List<Color> SetTranparencies(List<Color> textureColors, List<Vector2> interpolatedMask,
        List<Vector2> minimumInterpolatedMask)
    {
        int cntr = 0;
        for (int i = 0; i < _height; i++)
        {
            for (int j = 0; j < _width; j++)
            {
                Vector2 point = new Vector2(j, i);
                Color color = Color.black;
                ; //textureColors[cntr];
                float alpha = GetPixelTranparency(point, interpolatedMask, minimumInterpolatedMask);
                color.b = alpha;
                Vector3DebuggerPoints.CreateCubeInPos(point, color);
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

        float alpha;
        int overSegmenteCheck = Math3D.PointOnWhichSideOfLineSegment(interpolatedMask[closestLineIndex],
            minimumInterpolatedMask[closestLineIndex], point);
        if (overSegmenteCheck == 0)
        {
            float segmentMagnitude = (interpolatedMask[closestLineIndex] - minimumInterpolatedMask[closestLineIndex])
                .magnitude;
            float projectedToMinimumMagnitude =
                (pointOnLineProjection - minimumInterpolatedMask[closestLineIndex]).magnitude;
            alpha = 1 - projectedToMinimumMagnitude / segmentMagnitude;
        }
        else if (overSegmenteCheck == 2)
        {
            alpha = 1;
        }
        else
        {
            alpha = 0;
        }

        return alpha;
    }

    private List<Vector2> GetNewInterpolateMask(List<Vector2> bigBorderMaskPositions,
        List<Vector2> smallBorderMaskPositions,
        float interpolationFactor, Vector2 smallBorderMaskCenter,
        float minimumFactor, out List<Vector2> minimumInterpolatedMask)
    {
        List<Vector2> interpolatedMask = new List<Vector2>();
        minimumInterpolatedMask = new List<Vector2>();

        for (int i = 0; i < bigBorderMaskPositions.Count; i++)
        {
            Vector2 bigPoint = bigBorderMaskPositions[i];
            Vector2 pointOverLine;
            Vector2 nearesPointInSmallMask = smallBorderMaskPositions
                .Select(vect => new
                {
                    distance = DistancePointToLine(vect, bigPoint, smallBorderMaskCenter, out pointOverLine),
                    vect
                })
                .OrderBy(x => x.distance)
                .First().vect;

            Vector2 minimumPoint = Vector2.Lerp(nearesPointInSmallMask, smallBorderMaskCenter, minimumFactor);
            minimumPoint = new Vector2(Mathf.Round(minimumPoint.x), Mathf.Round(minimumPoint.y));
            minimumInterpolatedMask.Add(minimumPoint);

            Vector2 interpolatedPoint = Vector2.Lerp(bigPoint, nearesPointInSmallMask, interpolationFactor);
            interpolatedPoint = new Vector2(Mathf.Round(interpolatedPoint.x), Mathf.Round(interpolatedPoint.y));
            interpolatedMask.Add(interpolatedPoint);
        }

        return interpolatedMask;
    }

    private float DistancePointToLine(Vector2 point, Vector2 linepoint, Vector2 linepoint2, out Vector2 overLine)
    {
        Vector2 pointOverLine = Math3D.ProjectPointOnLineSegment(linepoint, linepoint2, point);
        overLine = pointOverLine;
        return Vector2.Distance(pointOverLine, point);
    }

    private List<Vector2> GetMaskBorder(Texture2D mask)
    {
        List<Vector2> borderPositions = new List<Vector2>();

        List<Color> arrayTexture = mask.GetPixels(0, 0, _width, _height).ToList();

        int cntr = 0;

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int indexUp = y == 0 ? (_height - 1) * _width + x : cntr - _width;
                int indexDown = y == _height - 1 ? x : cntr + _width;
                int indexRight = x == _width - 1 ? cntr - _width : cntr + 1;
                int indexLeft = x == 0 ? cntr + _width : cntr - 1;

                if (!arrayTexture[cntr].Equals(Color.black))
                {
                    Vector2 pos = new Vector3(x, y);
                    _initialMaskPoints.Add(pos);

                    if (!AllAroundPixelColorsAreBlack(arrayTexture, indexUp, indexDown, indexRight, indexLeft))
                    {
                        borderPositions.Add(pos);
                    }
                }

                cntr++;
            }
        }

        return borderPositions;
    }

    private static Vector2 GetMaskCenter(List<Vector2> maskPositions)
    {
        Vector2 center = Vector2.zero;
        for (int i = 0; i < maskPositions.Count; i++)
        {
            center += maskPositions[i];
        }

        center /= maskPositions.Count;
        return center;
    }

    private bool AllAroundPixelColorsAreBlack(List<Color> arrayTexture, int indexUp, int indexDown, int indexRight,
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