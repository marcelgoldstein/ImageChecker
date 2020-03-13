using ImageChecker.Concurrent;
using ImageChecker.DataClass;
using ImageChecker.Helper;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.Flann;
using OpenCvSharp.XFeatures2D;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ImageChecker.Imaging
{
    public class CustomFLANN : IDisposable
    {
        private const double surfHessianThresh = 400;
        private const int Knn = 2;

        private SURF surfDetector = SURF.Create(surfHessianThresh);

        /// <summary>
        /// Computes image descriptors.
        /// </summary>
        /// <param name="fileName">Image filename.</param>
        /// <returns>The descriptors for the given image.</returns>
        public async Task<Mat<float>> ComputeSingleDescriptorsAsync(string fileName, ConcurrentBag<FileImage> preLoadedFileImagesSource, ConcurrentBag<string> errorFiles, bool preResizeImages, int preResizeScale)
        {
            var descriptors = new Mat<float>();

            await Task.Run(() =>
            {
                try
                {
                    using (var fs = File.OpenRead(@"\\?\" + fileName))
                    {
                        var bmi = new BitmapImage();
                        bmi.BeginInit();
                        bmi.StreamSource = fs;
                        bmi.CacheOption = BitmapCacheOption.OnLoad;
                        bmi.EndInit();
                        bmi.Freeze();

                        using (var bm = bmi.ToDrawingBitmap())
                        {
                            using (var origImage = bm.ToMat())
                            using (var scaledImage = new Mat())
                            using (var grayedImage = new Mat())
                            {
                                if (preResizeImages)
                                {
                                    Cv2.Resize(origImage, scaledImage, new Size(preResizeScale, preResizeScale), 0D, 0D, InterpolationFlags.Cubic);
                                }
                                else
                                {
                                    origImage.CopyTo(scaledImage);
                                }

                                Cv2.CvtColor(scaledImage, grayedImage, ColorConversionCodes.BGR2GRAY);
                                origImage.Dispose();
                                scaledImage.Dispose();

                                this.surfDetector.DetectAndCompute(grayedImage, null, out _, descriptors);
                            }
                        }
                    }

                    preLoadedFileImagesSource.Add(new FileImage(fileName, descriptors));
                }
                catch (Exception) { errorFiles.Add(fileName); } // when anything goes wrong with reading of the image/file
            });

            return descriptors;
        }



        /// <summary>
        /// Computes 'similarity' value (IndecesMapping.Similarity) for each image in the collection against our query image.
        /// </summary>
        /// <param name="dbDescriptors">Query image descriptor.</param>
        /// <param name="source">Consolidated db images descriptors.</param>
        /// <param name="images">List of IndecesMapping to hold the 'similarity' value for each image in the collection.</param>
        public async Task FindMatches(FileImage source, List<FileImage> targets, ConcurrentBag<ImageCompareResult> possibleDuplicates, double threshold, CancellationToken cts, PauseToken pts)
        {
            await Task.Run(async () =>
            {
                if (source.SURFDescriptors.Rows < 40)
                { // dann kann das bild nicht gut erkannt werden und das ergebnis kann nicht aussagekräftig sein -> also überspringen!
                    return;
                }

                double similarity = 0D;

                // jenachdem ob source oder target mehr rows hat, soll das größere als 'source' und das kleinere als 'target' behandelt werden
                foreach (var target in targets)
                {
                    if (target.SURFDescriptors.Rows < 40)
                    { // dann kann das bild nicht gut erkannt werden und das ergebnis kann nicht aussagekräftig sein -> also überspringen!
                        continue;
                    }

                    similarity = 0D;

                    var indices = new Mat<int>(source.SURFDescriptors.Rows, Knn); // matrix that will contain indices of the 2-nearest neighbors found
                    var dists = new Mat<float>(source.SURFDescriptors.Rows, Knn); // matrix that will contain distances to the 2-nearest neighbors found

                    // create FLANN index with 4 kd-trees and perform KNN search over it look for 2 nearest neighbours
                    var flannIndex = new OpenCvSharp.Flann.Index(target.SURFDescriptors, new KDTreeIndexParams(4));
                    flannIndex.KnnSearch(source.SURFDescriptors, indices, dists, Knn, new SearchParams(32));

                    for (int i = 0; i < indices.Rows; i++)
                    {
                        // filter out all inadequate pairs based on distance between pairs
                        if (dists.Get<float>(i, 0) < (0.6 * dists.Get<float>(i, 1)))
                        {
                            similarity++;
                        }
                    }

                    similarity = (similarity / (double)source.SURFDescriptors.Rows) * 100D;

                    if (similarity >= threshold)
                    { // ergebnis hinzufügen
                        possibleDuplicates.Add(new ImageCompareResult() { FileA = source, FileB = target, FLANN = similarity });
                    }

                    await pts.WaitWhilePausedAsync();

                    if (cts.IsCancellationRequested)
                        break;
                }
            });
        }

        public void Dispose()
        {
            if (surfDetector != null)
            {
                surfDetector.Dispose();
                surfDetector = null;
            }
        }
    }
}
