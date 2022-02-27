namespace UglyLines.Desktop.Views;

public record FieldSettings
{
    public double LeftMargin { get; init; }
    public double TopMargin { get; init; }
    public double CellSize { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    
    public double FieldToCanvasX(int fieldX) => LeftMargin + CellSize*fieldX;
    public double FieldToCanvasY(int fieldY) => TopMargin + CellSize*fieldY;
}