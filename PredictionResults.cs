using System;
using System.Collections.Generic;
using System.Text;

namespace MachineLearning
{
    class PredictionResult
    {
        //public string id { get; set; }
        //public string project { get; set; }
        //public string iteration { get; set; }
        public List<Prediction> predictions { get; set; }

    }

    class Prediction
    {
        public decimal probability { get; set; }
        public string tagId { get; set; }
        public string tagName { get; set; }
        public BoundingBox boundingBox { get; set; }
    }

    class BoundingBox
    {
        public decimal left { get; set; }
        public decimal top { get; set; }
        public decimal width { get; set; }
        public decimal height { get; set; }

    }
}
