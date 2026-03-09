using System.IO;
using ImageMagick;

namespace RayCarrot.RCP.Metro.Imaging;

public abstract class ImageMagickImageFormat : ImageFormat
{
    public override string Id => Format.ToString();

    public override bool CanDecode => true;
    public override bool CanEncode => true;

    public abstract MagickFormat Format { get; }

    private ImageMetadata GetMetadata(MagickImage img)
    {
        return new ImageMetadata((int)img.Width, (int)img.Height);
    }

    protected virtual void OnEncode(MagickImage img) { }

    public override ImageMetadata GetMetadata(Stream inputStream)
    {
        using MagickImage img = new(inputStream, Format);
        return GetMetadata(img);
    }

    public override RawImageData Decode(Stream inputStream)
    {
        using MagickImage img = new(inputStream, Format);
        using IUnsafePixelCollection<byte> pixels = img.GetPixelsUnsafe();

        if (img.HasAlpha)
        {
            byte[] rawData = pixels.ToByteArray("BGRA") ?? throw new Exception("Unable to get raw pixel bytes from image");
            return new RawImageData(rawData, RawImageDataPixelFormat.Bgra32, GetMetadata(img));
        }
        else
        {
            byte[] rawData = pixels.ToByteArray("BGR") ?? throw new Exception("Unable to get raw pixel bytes from image");
            return new RawImageData(rawData, RawImageDataPixelFormat.Bgr24, GetMetadata(img));
        }
    }

    public override ImageMetadata Encode(RawImageData data, Stream outputStream)
    {
        using MagickImage img = new(data.GetImageData(0), new MagickReadSettings
        {
            Format = data.PixelFormat switch
            {
                RawImageDataPixelFormat.Bgr24 => MagickFormat.Bgr,
                RawImageDataPixelFormat.Bgra32 => MagickFormat.Bgra,
                _ => throw new ArgumentOutOfRangeException()
            },
            Width = (uint)data.Metadata.Width,
            Height = (uint)data.Metadata.Height,
        });

        OnEncode(img);

        img.Format = Format;
        img.Strip();
        img.Write(outputStream);
        
        return GetMetadata(img);
    }

    public override ImageMetadata Convert(Stream inputStream, Stream outputStream, ImageFormat outputFormat)
    {
        if (outputFormat is ImageMagickImageFormat outputMagickFormat)
        {
            using MagickImage img = new(inputStream, Format);

            OnEncode(img);

            img.Format = outputMagickFormat.Format;
            img.Strip();
            img.Write(outputStream);

            return GetMetadata(img);
        }
        else
        {
            return base.Convert(inputStream, outputStream, outputFormat);
        }
    }
}