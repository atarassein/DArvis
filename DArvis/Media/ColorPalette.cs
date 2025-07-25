﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace DArvis.Media
{
    public sealed class ColorPalette : IEnumerable<Color>
    {
        public const int ColorCount = 256;

        private readonly List<Color> colors;

        public string Name { get; set; }

        public int Count => colors.Count;

        public ColorPalette(string filename)
           : this(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read), leaveOpen: false)
        {
            Name = filename;
        }

        public Color this[int index]
        {
            get => colors[index];
            set => colors[index] = value;
        }

        public ColorPalette(Stream stream, bool leaveOpen = true)
        {
            colors = new List<Color>(ColorCount);

            for (int i = 0; i < ColorCount; i++)
            {
                var red = (byte)stream.ReadByte();
                var green = (byte)stream.ReadByte();
                var blue = (byte)stream.ReadByte();

                var color = Color.FromRgb(red, green, blue);
                colors.Add(color);
            }

            if (!leaveOpen)
                stream.Dispose();
        }

        public ColorPalette(IEnumerable<Color> collection)
        {
            if (collection != null)
                colors = new List<Color>(collection);
            else
                colors = new List<Color>();
        }

        public ColorPalette MakeGrayscale(double redWeight = 0.30, double greenWeight = 0.59, double blueWeight = 0.11)
        {
            var grayColors = new List<Color>(Count);

            foreach (var color in this)
            {
                var luma = color.R * redWeight + color.G * greenWeight + color.B * blueWeight;
                luma = Math.Min(luma, 255);

                var gray = Color.FromRgb((byte)luma, (byte)luma, (byte)luma);
                grayColors.Add(gray);
            }

            return new ColorPalette(grayColors);
        }

        public IEnumerator<Color> GetEnumerator()
        {
            foreach (var color in colors)
                yield return color;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
