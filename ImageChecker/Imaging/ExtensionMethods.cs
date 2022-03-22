using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ImageChecker.Imaging;

public static class ExtensionMethods
{
    /// <summary>
    /// The Bhattacharyya difference (the difference between normalized versions of the histograms of both images)
    /// This tells something about the differences in the brightness of the images as a whole, not so much about where they differ.
    /// </summary>
    /// <param name="img1">The first image to compare</param>
    /// <param name="img2">The second image to compare</param>
    /// <returns>The difference between the images' normalized histograms</returns>
    public static float BhattacharyyaDifference(this byte[,] img1GrayscaleValues, byte[,] img2GrayscaleValues)
    {
        var normalizedHistogram1 = new double[img1GrayscaleValues.GetLength(0), img1GrayscaleValues.GetLength(1)];
        var normalizedHistogram2 = new double[img2GrayscaleValues.GetLength(0), img2GrayscaleValues.GetLength(1)];

        double histSum1 = 0.0;
        double histSum2 = 0.0;

        foreach (var value in img1GrayscaleValues) { histSum1 += value; }
        foreach (var value in img2GrayscaleValues) { histSum2 += value; }


        for (int x = 0; x < img1GrayscaleValues.GetLength(0); x++)
        {
            for (int y = 0; y < img1GrayscaleValues.GetLength(1); y++)
            {
                normalizedHistogram1[x, y] = (double)img1GrayscaleValues[x, y] / histSum1;
            }
        }
        for (int x = 0; x < img2GrayscaleValues.GetLength(0); x++)
        {
            for (int y = 0; y < img2GrayscaleValues.GetLength(1); y++)
            {
                normalizedHistogram2[x, y] = (double)img2GrayscaleValues[x, y] / histSum2;
            }
        }

        double bCoefficient = 0.0;
        for (int x = 0; x < img2GrayscaleValues.GetLength(0); x++)
        {
            for (int y = 0; y < img2GrayscaleValues.GetLength(1); y++)
            {
                double histSquared = normalizedHistogram1[x, y] * normalizedHistogram2[x, y];
                bCoefficient += Math.Sqrt(histSquared);
            }
        }

        double dist1 = 1.0 - bCoefficient;
        dist1 = Math.Round(dist1, 8);
        double distance = Math.Sqrt(dist1);
        distance = Math.Round(distance, 8);
        return (float)distance;

    }

    /// <summary>
    /// The Bhattacharyya difference (the difference between normalized versions of the histograms of both images)
    /// This tells something about the differences in the brightness of the images as a whole, not so much about where they differ.
    /// </summary>
    /// <param name="img1">The first image to compare</param>
    /// <param name="img2">The second image to compare</param>
    /// <returns>The difference between the images' normalized histograms</returns>
    public static float BhattacharyyaDifference(this Image img1, Image img2, int newWidth, int newHeight)
    {
        byte[,] img1GrayscaleValues = img1.GetGrayScaleValues(newWidth, newHeight);
        byte[,] img2GrayscaleValues = img2.GetGrayScaleValues(newWidth, newHeight);

        var normalizedHistogram1 = new double[img1GrayscaleValues.GetLength(0), img1GrayscaleValues.GetLength(1)];
        var normalizedHistogram2 = new double[img2GrayscaleValues.GetLength(0), img2GrayscaleValues.GetLength(1)];

        double histSum1 = 0.0;
        double histSum2 = 0.0;

        foreach (var value in img1GrayscaleValues) { histSum1 += value; }
        foreach (var value in img2GrayscaleValues) { histSum2 += value; }


        for (int x = 0; x < img1GrayscaleValues.GetLength(0); x++)
        {
            for (int y = 0; y < img1GrayscaleValues.GetLength(1); y++)
            {
                normalizedHistogram1[x, y] = (double)img1GrayscaleValues[x, y] / histSum1;
            }
        }
        for (int x = 0; x < img2GrayscaleValues.GetLength(0); x++)
        {
            for (int y = 0; y < img2GrayscaleValues.GetLength(1); y++)
            {
                normalizedHistogram2[x, y] = (double)img2GrayscaleValues[x, y] / histSum2;
            }
        }

        double bCoefficient = 0.0;
        for (int x = 0; x < img2GrayscaleValues.GetLength(0); x++)
        {
            for (int y = 0; y < img2GrayscaleValues.GetLength(1); y++)
            {
                double histSquared = normalizedHistogram1[x, y] * normalizedHistogram2[x, y];
                bCoefficient += Math.Sqrt(histSquared);
            }
        }

        double dist1 = 1.0 - bCoefficient;
        dist1 = Math.Round(dist1, 8);
        double distance = Math.Sqrt(dist1);
        distance = Math.Round(distance, 8);
        return (float)distance;

    }

    /// <summary>
    /// Gets the lightness of the image in 256 sections (16x16)
    /// </summary>
    /// <param name="img">The image to get the lightness for</param>
    /// <returns>A doublearray (16x16) containing the lightness of the 256 sections</returns>
    public static byte[,] GetGrayScaleValues(this Image img, int newWidth, int newHeight)
    {
        using Bitmap thisOne = (Bitmap)img.Resize(newWidth, newHeight).GetGrayScaleVersion();
        byte[,] grayScale = new byte[newWidth, newHeight];


        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                grayScale[x, y] = (byte)Math.Abs(thisOne.GetPixel(x, y).R);
            }
        }
        return grayScale;
    }

    /// <summary>
    /// Resizes an image
    /// </summary>
    /// <param name="originalImage">The image to resize</param>
    /// <param name="newWidth">The new width in pixels</param>
    /// <param name="newHeight">The new height in pixels</param>
    /// <returns>A resized version of the original image</returns>
    public static Image Resize(this Image originalImage, int newWidth, int newHeight)
    {


        Image smallVersion = new Bitmap(newWidth, newHeight);
        using (Graphics g = Graphics.FromImage(smallVersion))
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            lock (originalImage)
            {
                g.DrawImage(originalImage, 0, 0, newWidth, newHeight);
            }
        }

        return smallVersion;
    }

    /// <summary>
    /// Converts an image to grayscale
    /// </summary>
    /// <param name="original">The image to grayscale</param>
    /// <returns>A grayscale version of the image</returns>
    public static Image GetGrayScaleVersion(this Image original)
    {
        //http://www.switchonthecode.com/tutorials/csharp-tutorial-convert-a-color-image-to-grayscale
        //create a blank bitmap the same size as original
        Bitmap newBitmap = new Bitmap(original.Width, original.Height);

        //get a graphics object from the new image
        using (Graphics g = Graphics.FromImage(newBitmap))
        {
            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(_colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
        }
        return newBitmap;

    }

    //the colormatrix needed to grayscale an image
    //http://www.switchonthecode.com/tutorials/csharp-tutorial-convert-a-color-image-to-grayscale
    private static readonly ColorMatrix _colorMatrix = new(new float[][]
    {
        new float[] {.3f, .3f, .3f, 0, 0},
        new float[] {.59f, .59f, .59f, 0, 0},
        new float[] {.11f, .11f, .11f, 0, 0},
        new float[] {0, 0, 0, 1, 0},
        new float[] {0, 0, 0, 0, 1}
    });

    public static Bitmap To24bppRgbFormat(this Bitmap img)
    {
        return img.Clone(new Rectangle(0, 0, img.Width, img.Height),
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
    }
}
