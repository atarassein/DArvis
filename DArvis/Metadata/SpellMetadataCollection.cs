﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DArvis.Metadata
{
    [Serializable]
    [XmlRoot("SpellMetadata")]
    public sealed class SpellMetadataCollection
    {
        private string version;
        private List<SpellMetadata> spells;

        [XmlAttribute("FileVersion")]
        [DefaultValue(null)]
        public string Version
        {
            get => version;
            set => version = value;
        }

        [XmlIgnore]
        public int Count => spells.Count;

        [XmlArray("Spells")]
        [XmlArrayItem("Spell")]
        public List<SpellMetadata> Spells
        {
            get => spells;
            private set => spells = value;
        }

        public SpellMetadataCollection()
        {
            spells = new List<SpellMetadata>();
        }

        public SpellMetadataCollection(int capacity)
        {
            spells = new List<SpellMetadata>(capacity);
        }

        public SpellMetadataCollection(IEnumerable<SpellMetadata> collection)
           : this()
        {
            if (collection != null)
                spells.AddRange(collection);
        }

        public override string ToString() => $"Count = {Count}";
    }
}
