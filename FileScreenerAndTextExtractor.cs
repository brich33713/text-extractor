using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

namespace MachineLearning
{
    class FileScreenerAndTextExtractor
    {
        

        public static void Main()
        {

            //These are the only 4 inputs we will need when converting this to a class to be used throughout code
            //imageLocation can be URL or File Path
            string imageLocation = "https://www.velvetjobs.com/assets/resume-templates/w600/one-column-other2.jpg";
            var predictionEndpoint = "https://readingcontracts.cognitiveservices.azure.com/";
            var subscriptionKey = "c8b79f70682b4891806adfd0c05ffa19";
            int probabilityThreshold = 57;

            //keys and urls set to variables for better readability
            var predictionKey = "3e42f4f3fa8d4834b9a90f9a6389e745";
            var ObjectDetectorURL = "https://customvisionvideo.cognitiveservices.azure.com/customvision/v3.0/Prediction/df089820-db3c-434a-b396-3e87efcb1969/detect/iterations/Iteration2/image";
            var ClassificationURL = "https://customvisionvideo.cognitiveservices.azure.com/customvision/v3.0/Prediction/1b47f747-ef32-4337-b1a8-4150f9b8d16d/classify/iterations/Iteration3/image";

            //Creates the ComputerVisionClient which is OCR powered by Azure.
            ComputerVisionClient client = Authenticate(predictionEndpoint, subscriptionKey);
            var model = new Model();

            //if image is local file change second parameter to true
            byte[] imageByteData = GetImageAsByteArray(imageLocation,false);

            //Pass url into so that machine can make a prediction and return results. This is the machine that tell us the type of file
            Console.WriteLine("First Machine Running");
            var imagePrediction = model.getPrediction(imageByteData, predictionKey, ClassificationURL);

            //if prediction passes parameters and is of right type
            //Pass url into so that machine can make a prediction and return results. This is the machine that will find the area that has text we need to extract.
            if (Decimal.Round(imagePrediction.probability * 100) >= probabilityThreshold && imagePrediction.tagName == "resume")
            {
                Console.WriteLine("");
                Console.WriteLine("First Machine complete. Second running");
                var locationPrediction = model.getPrediction(imageByteData, predictionKey, ObjectDetectorURL);

                MemoryStream croppedImg = CropImage(imageByteData, locationPrediction.boundingBox);
                
                //Uncomment to see cropped image. Image is saved in bin folder.
                CreateTestCroppedImage(imageByteData, locationPrediction.boundingBox);

                //Pass in image and get extracted text(s)
                var extractedText = ReadFileLocal(client, croppedImg);
                Console.WriteLine("");
                Console.WriteLine("Complete");
                Console.ReadLine();
            } else if(imagePrediction.tagName == "cover letter")
            {
                Console.WriteLine("Cover Letter Found");
                Console.ReadLine();
            }
            {
                Console.WriteLine("Can't confidently determine type");
                Console.ReadLine();
            }
        }

        private static byte[] GetImageAsByteArray(string imageFilePath, bool isLocalFile)
        {
            if (!isLocalFile)
            {
                //probably can be refactored to pass in client, since client will be used throughout the code
                var client = new WebClient();

                //converts url to byte array
                return client.DownloadData(imageFilePath);
            }
            
            //create stream from local file
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);            
        }

        public static List<string> ReadFileLocal(ComputerVisionClient client, MemoryStream croppedImg)
        {
            List<string> extractedText = new List<string>();
            
            // Read text from image
            var textHeaders = client.ReadInStreamAsync(croppedImg).Result;

            // After the request, get the operation location (operation ID)
            string operationLocation = textHeaders.OperationLocation;
            Thread.Sleep(2000);

            // Retrieve the URI where the recognized text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            // Extract the text
            ReadOperationResult results;
            do
            {
                results = client.GetReadResultAsync(Guid.Parse(operationId)).Result;
            }
            while ((results.Status == OperationStatusCodes.Running ||
                results.Status == OperationStatusCodes.NotStarted));

            //Uncomment if you need to see extracted text
            displayExtractedText(results);

            foreach(var result in results.AnalyzeResult.ReadResults)
            {
                foreach(Line line in result.Lines)
                {
                    extractedText.Add(line.Text);
                }
            }

            return extractedText;


        }

        public static MemoryStream CropImage(byte[] imageByteData, BoundingBox boundingBox)
        {
            //creates image
            Image img = Image.FromStream(new MemoryStream(imageByteData));
           
            //cropped image dimensions. Bounding box returns values as a percentage. Actual measurements need to be calculated.
            int x = (int)Decimal.Round(boundingBox.left * img.Width);
            int y = (int)Decimal.Round(boundingBox.top * img.Height);
            int width = (int)Decimal.Round(boundingBox.width * img.Width);
            int height = (int)Decimal.Round(boundingBox.height * img.Height);

            //cropped image dimensions
            Rectangle crop = new Rectangle(x, y, width, height);

            //Cropped image will be fit into these dimensions
            Rectangle resize = new Rectangle(0, 0, width, height);

            var bmp = new Bitmap(crop.Width, crop.Height);
            using (var gr = Graphics.FromImage(bmp))
            {
                gr.DrawImage(img, resize, crop, GraphicsUnit.Pixel);
            }

            MemoryStream memoryStream = new MemoryStream();

            //Refactor to make ImageFormat equal to original file type and not always default to png
            bmp.Save(memoryStream, ImageFormat.Png);
            
            //Reset Stream
            memoryStream.Position = 0;
            
            return memoryStream;
        }

        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
              { Endpoint = endpoint };
            return client;
        }


        //Methods for helping users during development.
        //CreateTestCroppedImage saves croppedImage using machine results to local bin folder, so you can check if getting proper results.
        //displayExtractedText displays extracted text in console.
        

        public static void CreateTestCroppedImage(byte[] imageByteData, BoundingBox boundingBox)
        {
            //creates image
            Image img = Image.FromStream(new MemoryStream(imageByteData));

            //cropped image dimensions. Bounding box returns values as a percentage. Actual measurements need to be calculated.
            int x = (int)Decimal.Round(boundingBox.left * img.Width);
            int y = (int)Decimal.Round(boundingBox.top * img.Height);
            int width = (int)Decimal.Round(boundingBox.width * img.Width);
            int height = (int)Decimal.Round(boundingBox.height * img.Height);

            //cropped image dimensions
            Rectangle crop = new Rectangle(x, y, width, height);

            //Cropped image will be fit into these dimensions
            Rectangle resize = new Rectangle(0, 0, width, height);

            var bmp = new Bitmap(crop.Width, crop.Height);
            using (var gr = Graphics.FromImage(bmp))
            {
                gr.DrawImage(img, resize, crop, GraphicsUnit.Pixel);
            }

            string location = "testCropping.jpeg";

            //File is saved to bin folder
            bmp.Save(location, ImageFormat.Jpeg);
        }

        public static void displayExtractedText(ReadOperationResult results)
        {

            Console.WriteLine();
            Console.WriteLine("Text Extracted From Image: ");
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {
                    Console.WriteLine(line.Text);
                }
            }
        }

        //Constructor logic

        //public string imageLocation { get; set; }
        //public byte[] imageByteData { get; set; }
        //public string predictionEndpoint { get; set; }
        //public string subscriptionKey { get; set; }
        //public int probabilityThreshold { get; set; }


        //public FileScreenerAndTextExtractor(string imageLocation, bool isLocalFile, string predictionEndpoint, string subscriptionKey, int probabilityThreshold)
        //{
        //    this.imageLocation = imageLocation;
        //    imageByteData = GetImageAsByteArray(imageLocation, isLocalFile);
        //    this.predictionEndpoint = predictionEndpoint;
        //    this.subscriptionKey = subscriptionKey;
        //    this.probabilityThreshold = probabilityThreshold;
        //    string ObjectDetectorURL = "https://customvisionvideo.cognitiveservices.azure.com/customvision/v3.0/Prediction/df089820-db3c-434a-b396-3e87efcb1969/detect/iterations/Iteration2/image";
        //    string ClassificationURL = "https://customvisionvideo.cognitiveservices.azure.com/customvision/v3.0/Prediction/1b47f747-ef32-4337-b1a8-4150f9b8d16d/classify/iterations/Iteration1/image";

        //}
    }
}

