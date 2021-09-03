using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace MachineLearning
{
    class Model
    {
        public Prediction getPrediction(byte[] bytes, string predictorKey, string predictorURL)
        {
            var client = new HttpClient();

            // Request headers - Add("Prediction-Key","<Replace This Value with Prediction Key>")
            client.DefaultRequestHeaders.Add("Prediction-Key", predictorKey);

            // Prediction URL
            string url = predictorURL;

            HttpResponseMessage response;

            using (var content = new ByteArrayContent(bytes))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = client.PostAsync(url, content).Result;
                var test = response.Content.ReadAsStringAsync().Result;
                PredictionResult predictionResults = JsonConvert.DeserializeObject<PredictionResult>(test);

                //uncomment if you want to see results displayed in console
                displayPredictionResults(response);

                //Only need the first result because response is sorted from highest to lowest probability
                return predictionResults.predictions[0];   
            }

        }

        public void displayPredictionResults(HttpResponseMessage response)
        {
            Console.WriteLine();
            Console.WriteLine("Machine Prediction Results: ");
            Console.WriteLine();
            Console.WriteLine(response.Content.ReadAsStringAsync().Result);
        }
    }
}
