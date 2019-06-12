public readonly struct Quad
{
    public readonly float MinX, MaxX, MinY, MaxY;

    public Quad(float minX, float maxX, float minY, float maxY)
    {
        MinX = minX;
        MaxX = maxX;
        MinY = minY;
        MaxY = maxY;
    }
}
