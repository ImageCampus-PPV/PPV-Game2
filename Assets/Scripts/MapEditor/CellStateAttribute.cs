#if UNITY_EDITOR
using System;

public class CellStateAttribute : Attribute
{
    public float r;
    public float g;
    public float b;
    public float a;
    public CellStateAttribute(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }
}

#endif