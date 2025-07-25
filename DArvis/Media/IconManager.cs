﻿using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

using DArvis.IO;
using DArvis.Settings;

namespace DArvis.Media
{
    public sealed class IconManager
    {
        private static readonly IconManager instance = new();
        public static IconManager Instance => instance;

        private IconManager() { }

        private TaskScheduler context;
        private readonly ConcurrentDictionary<int, BitmapSource> skillIcons = new();
        private readonly ConcurrentDictionary<int, BitmapSource> spellIcons = new();
        private ColorPalette skillIconPalette;
        private ColorPalette spellIconPalette;
        private EpfImage skillIconImage;
        private EpfImage spellIconImage;

        public TaskScheduler Context
        {
            get => context;
            set => context = value;
        }

        public int SkillIconCount => skillIcons.Count;
        public int SpellIconCount => spellIcons.Count;

        public IEnumerable<BitmapSource> SkillIcons => from s in skillIcons.Values select s;
        public IEnumerable<BitmapSource> SpellIcons => from s in spellIcons.Values select s;

        public void AddSkillIcon(int index, BitmapSource source) => skillIcons[index] = source;

        public void AddSpellIcon(int index, BitmapSource source) => spellIcons[index] = source;

        public bool ContainsSkillIcon(int index) => skillIcons.ContainsKey(index);

        public bool ContainsSpellIcon(int index) => spellIcons.ContainsKey(index);

        public BitmapSource GetSkillIcon(int index)
        {
            if (skillIcons.ContainsKey(index))
                return skillIcons[index];

            var bitmap = RenderSkillIcon(index);

            if (bitmap == null)
                return null;

            var bitmapSource = bitmap.CreateBitmapSource();
            bitmapSource.Freeze();
            skillIcons[index] = bitmapSource;

            if (skillIcons.ContainsKey(index))
                return skillIcons[index];
            else
                return null;
        }

        public BitmapSource GetSpellIcon(int index)
        {
            if (spellIcons.ContainsKey(index))
                return spellIcons[index];

            var bitmap = RenderSpellIcon(index);

            if (bitmap == null)
                return null;

            var bitmapSource = bitmap.CreateBitmapSource();
            bitmapSource.Freeze();

            spellIcons[index] = bitmapSource;

            if (spellIcons.ContainsKey(index))
                return spellIcons[index];
            else
                return null;
        }

        public bool RemoveSkillIcon(int index) => skillIcons.TryRemove(index, out _);

        public bool RemoveSpellIcon(int index) => spellIcons.TryRemove(index, out _);

        public void ClearSkillIcons() => skillIcons.Clear();

        public void ClearSpellIcons() => spellIcons.Clear();

        RenderedBitmap RenderSkillIcon(int index)
        {
            var settings = UserSettingsManager.Instance.Settings;

            skillIconPalette ??= GetColorPalette(settings.IconDataFile, settings.SkillPaletteFile);
            skillIconImage ??= GetEpfImage(settings.IconDataFile, settings.SkillIconFile);

            if (skillIconPalette == null || skillIconImage == null)
                return null;

            if (index >= skillIconImage.FrameCount)
                return null;

            var frame = skillIconImage.GetFrameAt(index);
            var bitmap = RenderManager.Render(frame, skillIconPalette);

            return bitmap;
        }

        RenderedBitmap RenderSpellIcon(int index)
        {
            var settings = UserSettingsManager.Instance.Settings;

            spellIconPalette ??= GetColorPalette(settings.IconDataFile, settings.SpellPaletteFile);
            spellIconImage ??= GetEpfImage(settings.IconDataFile, settings.SpellIconFile);

            if (spellIconPalette == null || spellIconImage == null)
                return null;

            if (index >= spellIconImage.FrameCount)
                return null;

            var frame = spellIconImage.GetFrameAt(index);
            var bitmap = RenderManager.Render(frame, spellIconPalette);

            return bitmap;
        }

        public void ReloadIcons()
        {
            var settings = UserSettingsManager.Instance.Settings;

            skillIconPalette = GetColorPalette(settings.IconDataFile, settings.SkillPaletteFile);
            spellIconPalette = GetColorPalette(settings.IconDataFile, settings.SpellPaletteFile);

            skillIconImage = GetEpfImage(settings.IconDataFile, settings.SkillIconFile);
            spellIconImage = GetEpfImage(settings.IconDataFile, settings.SpellIconFile);
        }

        static string GetRelativePath(string currentDirectory, string filename)
        {
            if (Path.IsPathRooted(filename))
                return filename;

            var directory = Path.GetDirectoryName(currentDirectory);
            var path = Path.Combine(directory, filename);

            return path;
        }

        static ColorPalette GetColorPalette(string archiveFile, string paletteFile)
        {
            var archivePath = GetRelativePath(UserSettingsManager.Instance.Settings.ClientPath, archiveFile);
            var archive = FileArchiveManager.Instance.GetArchive(archivePath);

            if (archive == null || !archive.ContainsFile(paletteFile))
                return null;

            try
            {
                using var inputStream = archive.GetStream(paletteFile);
                return new ColorPalette(inputStream);
            }
            catch { return null; }
        }

        static EpfImage GetEpfImage(string archiveFile, string epfFile)
        {
            var archivePath = GetRelativePath(UserSettingsManager.Instance.Settings.ClientPath, archiveFile);
            var archive = FileArchiveManager.Instance.GetArchive(archivePath);

            if (archive == null || !archive.ContainsFile(epfFile))
                return null;

            try
            {
                using var inputStream = archive.GetStream(epfFile);
                return new EpfImage(inputStream);
            }
            catch { return null; }
        }
    }
}
