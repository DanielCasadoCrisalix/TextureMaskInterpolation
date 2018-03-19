using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Crisalix.Visualization
{
    public class AlphaMaskToTexture
    {
        [SerializeField] private Texture2D _testMask;
        [SerializeField] private Texture2D _bodyTexture;
        private Texture2D _result;

        private void Start()
        {
            _result = new Texture2D(_bodyTexture.width, _bodyTexture.height, TextureFormat.RGBA32, false);
            GetSimpleMaskedTexture(_bodyTexture);
        }

        private Texture2D GetSimpleMaskedTexture(Texture2D bodyTexture)
        {
            if (bodyTexture.width != _testMask.width && bodyTexture.height != _testMask.height)
            {
                return null;
            }

            int width = bodyTexture.width;
            int height = bodyTexture.height;
            List<Color> maskColors = _testMask.GetPixels(0, 0, width, height).ToList();
            List<Color> textureColors = bodyTexture.GetPixels(0, 0, width, height).ToList();
            List<Color> resultColors = new List<Color>();

            for (int i = 0; i < textureColors.Count; i++)
            {
                Color resultColor = textureColors[i];
                Color maskColor = maskColors[i];
                resultColor.a = maskColor.a;
                resultColors.Add(resultColor);
            }

            _result.SetPixels(resultColors.ToArray());
            _result.Apply();

            return _result;
        }
    }
}