using System.Runtime.Serialization;
using Newtonsoft.Json;
using Supabase.Core.Attributes;

namespace Supabase.Storage
{
    public class TransformOptions
    {
        /// <summary>
        /// The resize mode can be cover, contain or fill. Defaults to cover.
        /// - Cover resizes the image to maintain it's aspect ratio while filling the entire width and height.
        /// - Contain resizes the image to maintain it's aspect ratio while fitting the entire image within the width and height.
        /// - Fill resizes the image to fill the entire width and height.If the object's aspect ratio does not match the width and height, the image will be stretched to fit.
        /// </summary>
        public enum ResizeType
        {
            [MapTo("cover"), EnumMember(Value = "cover")]
            Cover,
            [MapTo("contain"), EnumMember(Value = "contain")]
            Contain,
            [MapTo("fill"), EnumMember(Value = "fill")]
            Fill
        }

        /// <summary>
        /// The width of the image in pixels.
        /// </summary>
        [JsonProperty("width")]
        public int? Width { get; set; }

        /// <summary>
        /// The height of the image in pixels.
        /// </summary>
        [JsonProperty("height")]
        public int? Height { get; set; }

        /// <summary>
        /// The resize mode can be cover, contain or fill. Defaults to cover.
        /// - Cover resizes the image to maintain it's aspect ratio while filling the entire width and height.
        /// - Contain resizes the image to maintain it's aspect ratio while fitting the entire image within the width and height.
        /// - Fill resizes the image to fill the entire width and height.If the object's aspect ratio does not match the width and height, the image will be stretched to fit.
        /// </summary>
        [JsonProperty("resize")]
        public ResizeType Resize { get; set; } = ResizeType.Cover;

        /// <summary>
        /// Set the quality of the returned image, this is percentage based, default 80
        /// </summary>
        [JsonProperty("quality")]
        public int Quality { get; set; } = 80;

        /// <summary>
        /// Specify the format of the image requested.
        ///
        /// When using 'origin' we force the format to be the same as the original image,
        /// bypassing automatic browser optimisation such as webp conversion
        /// </summary>
        [JsonProperty("format")]
        public string Format { get; set; } = "origin";
    }
}
