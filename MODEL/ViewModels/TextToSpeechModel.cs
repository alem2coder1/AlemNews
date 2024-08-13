namespace MODEL.ViewModels;

public class TextToSpeechModel
{
    public string Text { get; set; }
    public string Language { get; set; }
    public string Voice { get; set; } = "kk-issai-high.onnx";
}