using System.Collections;
using System.Collections.Generic;
using System.Windows;

namespace DArvis.Models;
public class PathFinder
{

    public static Path FindPath(PointVector start, PointVector end, int[,] terrain)
    {
        return new Path();
    }
}

public class Path : IEnumerable<PointVector>
{
    public PointVector[] Nodes { get; set; } = [];
    
    public IEnumerator<PointVector> GetEnumerator()
    {
        throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class PointVector
{
    public Point Position { get; set; }
    public Direction Direction { get; set; } = Direction.None;
    public PointVector? PreviousPosition { get; set; }
    public PointVector? NextPosition { get; set; }
}

