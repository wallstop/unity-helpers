using System;

// This file should PASS: regular C# classes don't require the one-class-per-file rule
public class RegularClassA
{
    public int Value { get; set; }
}

public class RegularClassB
{
    public string Name { get; set; }
}

public static class RegularStaticHelper
{
    public static void DoSomething() { }
}

internal class InternalHelper
{
    private readonly int _value;

    public InternalHelper(int value)
    {
        _value = value;
    }
}
