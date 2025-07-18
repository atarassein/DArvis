namespace DArvis.Extensions;

public class ConsoleColor
{
    public static string Red = "\x1b[31m";
    public static string Green = "\x1b[32m";
    public static string Yellow = "\x1b[33m";
    public static string Blue = "\x1b[34m";
    public static string Reset = "\x1b[0m";
}

public class ConsoleOutputExtension
{
    public static string ColorText(string text, string color)
    {
        return $"{color}{text}{ConsoleColor.Reset}";
    }
    
    // i want another colortext function  with an out var for text
    public static void ColorText(string text, string color, out string coloredText)
    {
        coloredText = $"{color}{text}{ConsoleColor.Reset}";
    }
}