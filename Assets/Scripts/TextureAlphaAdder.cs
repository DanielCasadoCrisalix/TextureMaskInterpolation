using System.Collections.Generic;
using System.Linq;
using Crisalix.Visualization;
using UnityEngine;

public class TextureAlphaAdder : MonoBehaviour
{
    [SerializeField] private Texture2D _smallMask;
    [SerializeField] private Texture2D _bigMask;
    [SerializeField] private Texture2D _bodyTexture;

    [SerializeField] private Texture2D _testMask;
    
    [SerializeField] float _minimumFactor = 0.2f;
    [SerializeField] float _maximumFactor = 0.6f;

    
    [SerializeField] private Texture2D _result;

    private void Start()
    {
        
        GetSimpleMaskedTexture(_bodyTexture);

        // GetMaskedTexture(minimumFactor, maximumFactor);
    }

    private Texture2D GetSimpleMaskedTexture(Texture2D bodyTexture)
    {
      /*  if (bodyTexture.width != _smallMask.width && bodyTexture.height != _smallMask.height)
        {
            return null;
        }*/
        
        _result = new Texture2D(bodyTexture.width,bodyTexture.height,TextureFormat.RGBA32,true);

        int width = bodyTexture.width;
        int height = bodyTexture.height;
        List<Color> maskColors = _testMask.GetPixels(0, 0, width, height).ToList();
        List<Color> textureColors = bodyTexture.GetPixels(0, 0, width, height).ToList();
        List<Color> resultTexture = new List<Color>();

        for (int i = 0; i < textureColors.Count; i++)
        {
            Color resultColor = textureColors[i];
            Color maskColor = maskColors[i];
            resultColor.a = maskColor.a;
            resultTexture.Add(resultColor);
        }

        _result.SetPixels(resultTexture.ToArray());
        _result.Apply();

        return _result;
    }

    private void GetMaskedTexture(float minimumFactor, float maximumFactor)
    {
        List<Vector2> smallBorderMaskPositions = GetMaskBorder(_smallMask);
        Vector2 smallBorderMaskCenter = GetMaskCenter(smallBorderMaskPositions);

        List<Vector2> bigBorderMaskPositions = GetMaskBorder(_bigMask);

        List<Vector2> minimumInterpolatedMask;
        List<Vector2> interpolatedMask = GetNewInterpolateMask(bigBorderMaskPositions, smallBorderMaskPositions,
            maximumFactor,
            smallBorderMaskCenter, minimumFactor, out minimumInterpolatedMask);

        CreateMaskTexture(_bodyTexture, interpolatedMask, smallBorderMaskPositions, smallBorderMaskCenter);
    }

    private void CreateMaskTexture(Texture2D bodyTexture, List<Vector2> interpolatedMask,
        List<Vector2> smallBorderMaskPositions, Vector2 smallBorderMaskCenter)
    {
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

            Vector2 nearesPointInSmallMask = smallBorderMaskPositions
                .Select(vect => new {distance = DistancePointToLine(vect, bigPoint, smallBorderMaskCenter), vect})
                .OrderBy(x => x.distance)
                .First().vect;

            Vector2 interpolatedPoint = Vector2.Lerp(bigPoint, nearesPointInSmallMask, interpolationFactor);
            interpolatedPoint = new Vector2(Mathf.Round(interpolatedPoint.x), Mathf.Round(interpolatedPoint.y));
            interpolatedMask.Add(interpolatedPoint);

            Vector2 minimumPoint = Vector2.Lerp(interpolatedMask[i], smallBorderMaskCenter, minimumFactor);
            minimumPoint = new Vector2(Mathf.Round(minimumPoint.x), Mathf.Round(minimumPoint.y));
            minimumInterpolatedMask.Add(minimumPoint);
        }

        interpolatedMask = interpolatedMask.OrderBy(
            x => Vector2.Distance(Vector2.zero, x)
        ).ToList();

        for (int i = 0; i < interpolatedMask.Count; i++)
        {
            Vector3DebuggerPoints.CreateSphereInPos(interpolatedMask[i], Color.red);
        }

        for (int i = 0; i < minimumInterpolatedMask.Count; i++)
        {
            Vector3DebuggerPoints.CreateSphereInPos(minimumInterpolatedMask[i], Color.black);
        }

        return interpolatedMask;
    }

    private float DistancePointToLine(Vector2 vect, Vector2 bigPoint, Vector2 centerMask)
    {
        Vector2 pointOverLine = Math3D.ProjectPointOnLineSegment(centerMask, bigPoint, vect);
        return Vector2.Distance(pointOverLine, vect);
    }

    private List<Vector2> GetMaskBorder(Texture2D mask)
    {
        List<Vector2> borderPositions = new List<Vector2>();
        List<Color> border = new List<Color>();

        int width = mask.width;
        int height = mask.height;
        List<Color> arrayTexture = mask.GetPixels(0, 0, width, height).ToList();

        int cntr = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int indexUp = y == 0 ? (height - 1) * width + x : cntr - width;
                int indexDown = y == height - 1 ? x : cntr + width;
                int indexRight = x == width - 1 ? cntr - width : cntr + 1;
                int indexLeft = x == 0 ? cntr + width : cntr - 1;

                if (!arrayTexture[cntr].Equals(Color.black))
                {
                    if (!AllAroundPixelColorsAreBlack(arrayTexture, indexUp, indexDown, indexRight, indexLeft))
                    {
                        Vector3 pos = new Vector3(x, y, 0);
                        borderPositions.Add(pos);
                        // Vector3DebuggerPoints.CreateSphereInPos(pos, Color.black).transform.parent = SphereCont.transform;

                        border.Add(Color.black);
                    }
                    else
                    {
                        border.Add(Color.white);
                    }
                }
                else
                {
                    border.Add(Color.white);
                }

                cntr++;
            }
        }

        Debug.Log(border.Count);

        return borderPositions;
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