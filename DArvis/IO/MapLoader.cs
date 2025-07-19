using System;
using System.Globalization;
using System.IO;
using DArvis.Models;
using Path = System.IO.Path;

namespace DArvis.IO;

public class MapLoader
{
    
    private static byte[] sotp = File.ReadAllBytes("sotp.dat");
    
    public static int[,] LoadMapGridFromAttributes(MapLocationAttributes attributes)
    {
        if (attributes == null)
            throw new ArgumentNullException(nameof(attributes), "MapLocationAttributes cannot be null.");
        if (attributes.MapNumber < 1 || attributes.Width < 1 || attributes.Height < 1)
            throw new ArgumentException("Invalid map attributes provided. MapNumber, Width, and Height must be greater than 0.");
        
        int mapNumber = attributes.MapNumber; 
        int width = attributes.Width;
        int height = attributes.Height;
        
        var path = Path.Combine(Environment.CurrentDirectory, "maps") + "\\lod" +
                   mapNumber.ToString(CultureInfo.InvariantCulture) + ".map";

        if (!File.Exists(path))
            throw new FileNotFoundException("Map file not found: " + path);
        
        // 15 is the magic number for walls in the sotp.dat file
        
        Func<short, short, bool> isWall = (lWord , rWord) =>
        {
            if (lWord == 0 && rWord == 0)
                return false;

            if (lWord != 0 && sotp[lWord - 1] == 0x0F)
                return true;

            return rWord != 0 && sotp[rWord - 1] == 0x0F;
        };
        
        try
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var reader = new BinaryReader(stream);
            int[,] grid = new int[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    reader.BaseStream.Seek(2, SeekOrigin.Current);
                    grid[x, y] = isWall(reader.ReadInt16(), reader.ReadInt16()) ? (int)TileFlags.Wall : (int)TileFlags.None;
                }
            }

            reader.Close();
            stream.Close();

            return grid;
        } catch (Exception ex)
        {
            throw new IOException("Error reading map file: " + path, ex);
        }
    }
}