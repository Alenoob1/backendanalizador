namespace servicios.Models
{
    public class ImageClassificationResult
    {
        public string FileName { get; set; } = string.Empty;
        public string PredictedLabel { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }

}
