﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace DArvis.Metadata
{
    public sealed class SkillMetadataManager
    {
        public const string SkillMetadataFile = @"Skills.xml";

        private static readonly SkillMetadataManager instance = new();
        public static SkillMetadataManager Instance => instance;

        private SkillMetadataManager() { }

        private readonly ConcurrentDictionary<string, SkillMetadata> skills = new(StringComparer.OrdinalIgnoreCase);

        public event SkillMetadataEventHandler SkillAdded;
        public event SkillMetadataEventHandler SkillChanged;
        public event SkillMetadataEventHandler SkillRemoved;

        public int Count => skills.Count;

        public IEnumerable<SkillMetadata> Skills => from s in skills.Values orderby s.Name ascending select s;

        public void AddSkill(SkillMetadata skill)
        {
            if (skill == null)
                throw new ArgumentNullException(nameof(skill));

            string skillName = skill.Name.Trim();
            bool wasUpdated = false;

            if (skills.ContainsKey(skillName))
                wasUpdated = true;

            skills[skillName] = skill;

            if (wasUpdated)
                OnSkillChanged(skill);
            else
                OnSkillAdded(skill);
        }

        public bool ContainsSkill(string skillName)
        {
            skillName = skillName.Trim();
            return skills.ContainsKey(skillName);
        }

        public SkillMetadata GetSkill(string skillName)
        {
            skillName = skillName.Trim();

            skills.TryGetValue(skillName, out var skill);
            return skill;
        }

        public bool RemoveSkill(string skillName)
        {
            skillName = skillName.Trim();

            var wasRemoved = skills.TryRemove(skillName, out var removedSkill);

            if (wasRemoved)
                OnSkillRemoved(removedSkill);

            return wasRemoved;
        }

        public bool RenameSkill(string originalName, string newName)
        {
            var wasFound = skills.TryRemove(originalName, out var skill);

            if (wasFound)
            {
                OnSkillRemoved(skill);
                skills[newName] = skill;
                OnSkillAdded(skill);
            }

            return wasFound;
        }

        public void ClearSkills()
        {
            foreach (var skill in skills.Values)
                OnSkillRemoved(skill);

            skills.Clear();
        }

        public void LoadFromFile(string filename)
        {
            using var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            LoadFromStream(inputStream);
        }

        public void LoadFromStream(Stream stream)
        {
            var serializer = new XmlSerializer(typeof(SkillMetadataCollection));

            if (serializer.Deserialize(stream) is not SkillMetadataCollection collection)
                return;

            foreach (var skill in collection.Skills)
                AddSkill(skill);
        }

        public void SaveToFile(string filename)
        {
            using var outputStream = File.Create(filename);

            SaveToStream(outputStream);
            outputStream.Flush();
        }

        public void SaveToStream(Stream stream)
        {
            var collection = new SkillMetadataCollection(Skills);
            var serializer = new XmlSerializer(typeof(SkillMetadataCollection));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            serializer.Serialize(stream, collection, namespaces);
        }

        private void OnSkillAdded(SkillMetadata skill) => SkillAdded?.Invoke(this, new SkillMetadataEventArgs(skill));
        private void OnSkillChanged(SkillMetadata skill) => SkillChanged?.Invoke(this, new SkillMetadataEventArgs(skill));
        private void OnSkillRemoved(SkillMetadata skill) => SkillRemoved?.Invoke(this, new SkillMetadataEventArgs(skill));
    }
}
