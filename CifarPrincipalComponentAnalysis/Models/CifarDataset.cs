//#define CHECK_PCA

using Accord.Math;
using Accord.Math.Decompositions;
using Accord.Statistics;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.Serialization.Formatters.Binary;
using System;
using CifarPrincipalComponentAnalysis.Utilities;

namespace CifarPrincipalComponentAnalysis.Models
{
    public abstract class CifarDataset
    {
        public bool IsEnabledReadDataset = true;
        public bool IsEnabledSelectDataset = true;

        public bool IsEnabledShowDataImages = false;
        public bool IsShowingDataImages = false;

        public bool IsEnabledShowTestImages = false;
        public bool IsShowingTestImages = false;

        public bool IsEnabledShowPcaFilters = false;
        public bool IsShowingPcaFilters = false;

        public bool IsVisiblePreviousButton = false;
        public bool IsVisibleNextButton = false;

        public bool IsVisiblePicture20 = true;
        public bool IsVisiblePicture10 = true;
        public bool IsVisiblePicture5 = true;

        public string SelectDatasetButtonContent = "Select Dataset";
        public int SelectedNumberOfEigenvectors = 0;

        public bool HasPcaFilters
        {
            get
            {
                if (_pcaFilters != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public PicturesRowsColumnsEnum PicturesRowsColumns = PicturesRowsColumnsEnum.PICTURES_20;

        public string PageNumber
        {
            get
            {
                if (_isReadingCompleted)
                {
                    return $"page {ImageIndex / (ImageRowNum * ImageColumnNum) + 1}";
                }
                else
                {
                    return "";
                }
            }
        }

        protected List<SmallImageData> _dataImages;
        protected List<SmallImageData> _testImages;

        private List<SmallImageData> _pcaFilters;

        private List<SmallImageData> DataImages
        {
            get
            {
                if (IsShowingDataImages)
                {
                    return _dataImages;
                }
                else if (IsShowingTestImages)
                {
                    return _testImages;
                }
                else
                {
                    return _pcaFilters;
                }
            }
        }

        public int ImageIndex
        {
            get
            {
                if (IsShowingDataImages)
                {
                    return _dataImageIndex;
                }
                else if (IsShowingTestImages)
                {
                    return _testImageIndex;
                }
                else
                {
                    return _pcaFilterIndex;
                }
            }
            set
            {
                if (IsShowingDataImages)
                {
                    _dataImageIndex = value;
                }
                else if (IsShowingTestImages)
                {
                    _testImageIndex = value;
                }
                else
                {
                    _pcaFilterIndex = value;
                }
            }
        }

        protected const int ImageWidth = 32;  // pixel
        protected const int ImageHeight = 32; // pixel
        protected const int ImageChannels = 3; // number of image channels (RGB)
        public int ImageDataSize => ImageWidth * ImageHeight * ImageChannels;

        protected const int ImageMargin = 2;

        protected const double DpiX = 96;
        protected const double DpiY = 96;

        protected const int ImageAreaSize = ImageWidth * ImageHeight;
        protected const int ImageByteSize = ImageAreaSize * 4; // PixelFormats.Pbgra32 has 4 bytes for each pixel
        protected const int ImageStride = ImageWidth * 4;      // PixelFormats.Pbgra32 has 4 bytes for each pixel

        protected readonly byte[] _pixels = new byte[ImageByteSize];

        protected int _dataImageIndex = 0;
        protected int _testImageIndex = 0;
        protected int _pcaFilterIndex = 0;

        protected int _imageColumnNum;
        public int ImageColumnNum
        {
            get => _imageColumnNum;
            set => _imageColumnNum = value;
        }

        protected int _imageRowNum;
        public int ImageRowNum
        {
            get => _imageRowNum;
            set => _imageRowNum = value;
        }

        protected int _areaWidth;
        protected int _areaHeight;

        protected WriteableBitmap _writeableBitmap;
        protected bool _isReadingCompleted;
        public bool IsReadingCompleted => _isReadingCompleted;

        private const string Debug_X_cov_Accord_File = "Debug_X_cov_Accord.bin";
        private const string PCA_Mean_Accord_File = "PCA_Mean_Accord.bin";
        private const string PCA_result_U_Accord_File = "PCA_result_U_Accord.bin";
        private const string PCA_result_W_Accord_File = "PCA_result_W_Accord.bin";
        private const string PCA_result_V_Accord_File = "PCA_result_V_Accord.bin";

        private double[] m_Mean;
        private double[,] m_U;
        private double[] m_W;
        private double[,] m_V;

        private string _selectedPath;

        public CifarDataset()
        {
            ImageColumnNum = 20;
            ImageRowNum = 20;

            _areaWidth = (ImageWidth + ImageMargin * 2) * ImageColumnNum; // pixel
            _areaHeight = (ImageHeight + ImageMargin * 2) * ImageRowNum;  // pixel

            _writeableBitmap = new WriteableBitmap(_areaWidth, _areaHeight, DpiX, DpiY, PixelFormats.Pbgra32, null);
        }

        public bool ReadDataset(string selectedPath)
        {
            string[] files = Directory.GetFiles(selectedPath);
            List<string> fileNames = new List<string>();

            foreach (string file in files)
            {
                fileNames.Add(Path.GetFileName(file));
            }

            if (CheckDirectory(selectedPath, fileNames) == false)
            {
                return false;
            }

            if (ReadFiles(selectedPath) == false)
            {
                return false;
            }

            string pca_Mean_File = Path.Combine(selectedPath, PCA_Mean_Accord_File);
            if (File.Exists(pca_Mean_File))
            {
                try
                {
                    m_Mean = (double[]) ReadDeserializedBinary(pca_Mean_File);
                }
                catch (Exception ex)
                {
                    CustomMaterialDesignMessageBox.Show($"Failed to read a binary file({pca_Mean_File}). {ex}");
                    return false;
                }
            }

            string pca_V_File = Path.Combine(selectedPath, PCA_result_V_Accord_File);
            if (File.Exists(pca_V_File))
            {
                try
                {
                    m_V = (double[,]) ReadDeserializedBinary(pca_V_File);
                    PreparePcaFilterImages();
                }
                catch (Exception ex)
                {
                    CustomMaterialDesignMessageBox.Show($"Failed to read a binary file({pca_V_File}). {ex}");
                    return false;
                }
            }

#if CHECK_PCA

            string pca_W_File = Path.Combine(selectedPath, PCA_result_W_Accord_File);
            if (File.Exists(pca_W_File))
            {
                try
                {
                    m_W = (double[]) ReadDeserializedBinary(pca_W_File);
                }
                catch (Exception ex)
                {
                    CustomMaterialDesignMessageBox.Show($"Failed to read a binary file({pca_W_File}). {ex}");
                    return false;
                }
            }

            string debug_X_cov_File = Path.Combine(selectedPath, Debug_X_cov_Accord_File);
            if (File.Exists(debug_X_cov_File))
            {
                try
                {
                    double[,] X_cov = (double[,]) ReadDeserializedBinary(debug_X_cov_File);
                }
                catch (Exception ex)
                {
                    CustomMaterialDesignMessageBox.Show($"Failed to read a binary file({debug_X_cov_File}). {ex}");
                    return false;
                }
            }

#endif // CHECK_PCA

            _isReadingCompleted = true;
            _selectedPath = selectedPath;
            return true;
        }

        public WriteableBitmap GetPreviousImage()
        {
            ImageIndex -= (ImageRowNum * ImageColumnNum);
            if (ImageIndex < 0)
            {
                // Show last page
                int remainder = DataImages.Count % (ImageRowNum * ImageColumnNum);
                if (remainder > 0)
                {
                    ImageIndex = DataImages.Count - remainder;
                }
                else
                {
                    ImageIndex += DataImages.Count;
                }
            }
            WriteImages();
            return _writeableBitmap;
        }

        public WriteableBitmap GetNextImage()
        {
            ImageIndex += (ImageRowNum * ImageColumnNum);
            if (ImageIndex >= DataImages.Count)
            {
                ImageIndex -= DataImages.Count;
            }
            WriteImages();
            return _writeableBitmap;
        }

        private void WriteImages()
        {
            _areaWidth = (ImageWidth + ImageMargin * 2) * ImageColumnNum; // pixel
            _areaHeight = (ImageHeight + ImageMargin * 2) * ImageRowNum;  // pixel
            _writeableBitmap = new WriteableBitmap(_areaWidth, _areaHeight, DpiX, DpiY, PixelFormats.Pbgra32, null);

            for (int column = 0; column < ImageColumnNum; column++)
            {
                for (int row = 0; row < ImageRowNum; row++)
                {
                    WriteSmallImage(column, row);
                }
            }
        }

        private void WriteSmallImage(int column, int row)
        {
            int remainder = ImageIndex % (ImageRowNum * ImageColumnNum);
            int initialIndex = ImageIndex - remainder;
            int n = row * ImageColumnNum + column + initialIndex;

            if (n >= DataImages.Count)
            {
                return;
            }

            if ((IsShowingDataImages || IsShowingTestImages) && SelectedNumberOfEigenvectors != 0)
            {
                SetImageRepsentedByEigenvectors(DataImages[n]);
            }
            else
            {
                for (int i = 0; i < ImageAreaSize; i++)
                {
                    _pixels[4 * i] = DataImages[n].BlueChannelData[i];      // blue
                    _pixels[4 * i + 1] = DataImages[n].GreenChannelData[i]; // green
                    _pixels[4 * i + 2] = DataImages[n].RedChannelData[i];   // red
                    _pixels[4 * i + 3] = (byte) 255;  // alpha
                }
            }

            int x = ImageMargin + (ImageWidth + ImageMargin * 2) * column;
            int y = ImageMargin + (ImageHeight + ImageMargin * 2) * row;
            _writeableBitmap.WritePixels(new Int32Rect(0, 0, ImageWidth, ImageHeight), _pixels, ImageStride, x, y);
        }

        private void SetImageRepsentedByEigenvectors(SmallImageData imageData)
        {
            double[] image = new double[ImageDataSize];

            for (int i = 0; i < ImageAreaSize; i++)
            {
                image[3 * i] = (double) imageData.BlueChannelData[i]; // blue
                image[3 * i + 1] = (double) imageData.GreenChannelData[i]; // green
                image[3 * i + 2] = (double) imageData.RedChannelData[i]; // red
            }
            image = image.Subtract(m_Mean);

            double[] principalComponentScores = new double[SelectedNumberOfEigenvectors];
            for (int i = 0; i < SelectedNumberOfEigenvectors; i++)
            {
                principalComponentScores[i] = image.Dot(m_V.GetColumn(i));
            }

            // double initial values are 0
            double[] imageRepsentedByEigenvectors = new double[ImageDataSize];
            for (int i = 0; i < SelectedNumberOfEigenvectors; i++)
            {
                imageRepsentedByEigenvectors = imageRepsentedByEigenvectors.Add(m_V.GetColumn(i).Multiply(principalComponentScores[i]));
            }

            imageRepsentedByEigenvectors = imageRepsentedByEigenvectors.Add(m_Mean);

            for (int i = 0; i < ImageAreaSize; i++)
            {
                _pixels[4 * i] = (byte) imageRepsentedByEigenvectors[3 * i]; // blue
                _pixels[4 * i + 1] = (byte) imageRepsentedByEigenvectors[3 * i + 1]; // green
                _pixels[4 * i + 2] = (byte) imageRepsentedByEigenvectors[3 * i + 2]; // red
                _pixels[4 * i + 3] = (byte) 255;  // alpha
            }
        }

        public WriteableBitmap GetWriteableBitmap()
        {
            if (_isReadingCompleted == false)
            {
                // returns empty image
                return _writeableBitmap;
            }

            WriteImages();
            return _writeableBitmap;
        }

        public bool GetPcaEigenVectors()
        {
            if (_pcaFilters == null)
            {
                try
                {
                    CalculatePCA();
                }
                catch (Exception ex)
                {
                    CustomMaterialDesignMessageBox.Show($"Failed to calculate PCA. {ex}");
                    return false;
                }
            }

            if (_pcaFilters == null)
                return false;

            IsShowingPcaFilters = true;
            WriteImages();

            return true;
        }

        private void CalculatePCA()
        {
            int numberOfImages = _dataImages.Count;

            double[,] X_input = new double[numberOfImages, ImageDataSize];
            for (int row = 0; row < numberOfImages; row++)
            {
                for (int column_i = 0; column_i < ImageAreaSize; column_i++)
                {
                    X_input[row, 3 * column_i] = (double) _dataImages[row].BlueChannelData[column_i]; // blue
                    X_input[row, 3 * column_i + 1] = (double) _dataImages[row].GreenChannelData[column_i]; // green
                    X_input[row, 3 * column_i + 2] = (double) _dataImages[row].RedChannelData[column_i]; // red
                }
            }

            // column means
            double[] m_Mean = X_input.Mean(dimension: 0);
            SaveSerializedBinary(m_Mean, Path.Combine(_selectedPath, PCA_Mean_Accord_File));

            X_input = X_input.Subtract(m_Mean, dimension: (VectorType)0);
            double[,] X_cov = X_input.Transpose().Dot(X_input);

            X_input = null;
            GC.Collect();
            SaveSerializedBinary(X_cov, Path.Combine(_selectedPath, Debug_X_cov_Accord_File));

            SingularValueDecomposition svd = new SingularValueDecomposition(X_cov);

            m_U = svd.LeftSingularVectors;
            m_W = svd.Diagonal;
            m_V = svd.RightSingularVectors;

            SaveSerializedBinary(m_U, Path.Combine(_selectedPath, PCA_result_U_Accord_File));
            SaveSerializedBinary(m_W, Path.Combine(_selectedPath, PCA_result_W_Accord_File));
            SaveSerializedBinary(m_V, Path.Combine(_selectedPath, PCA_result_V_Accord_File));

            PreparePcaFilterImages();
        }

        private static void SaveSerializedBinary(object t, string path)
        {
            using(Stream stream = File.Open(path, FileMode.Create))
            {
                BinaryFormatter bformatter = new BinaryFormatter();
                bformatter.Serialize(stream, t);
            }
        }

        private static object ReadDeserializedBinary(string path)
        {
            using(Stream stream = File.Open(path, FileMode.Open))
            {
                BinaryFormatter bformatter = new BinaryFormatter();
                return bformatter.Deserialize(stream);
            }
        }

        private void PreparePcaFilterImages()
        {
            _pcaFilters = new List<SmallImageData>();

            for (int column = 0; column < m_V.Columns(); column++)
            {
                double[] eigenVector = m_V.GetColumn(column);

                // change the range of the eigen vector values to display as the bitmaps: "Min: 0, Max: 255"
                double minValue = eigenVector.Min();
                eigenVector = eigenVector.Subtract(minValue);

                double maxValueModifier = eigenVector.Max() / 255.0;
                eigenVector = eigenVector.Divide(maxValueModifier);

                int label = column;
                string className = label.ToString();

                byte[] blueChannelData = new byte[ImageAreaSize];
                byte[] greenChannelData = new byte[ImageAreaSize];
                byte[] redChannelData = new byte[ImageAreaSize];

                for (int row = 0; row < eigenVector.Length; row += 3)
                {
                    int index = row / 3;
                    blueChannelData[index] = (byte) eigenVector[row];
                    greenChannelData[index] = (byte) eigenVector[row + 1];
                    redChannelData[index] = (byte) eigenVector[row + 2];
                }

                _pcaFilters.Add(new SmallImageData(label, className, ImageWidth, ImageHeight, redChannelData, greenChannelData, blueChannelData));
            }
        }

        protected abstract bool CheckDirectory(string selectedPath, List<string> fileNames);
        protected abstract bool ReadFiles(string selectedPath);
    }
}
