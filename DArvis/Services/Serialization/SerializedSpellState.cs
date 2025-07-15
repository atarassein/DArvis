﻿using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DArvis.Models;

namespace DArvis.Services.Serialization
{
    [Serializable]
    public sealed class SerializedSpellState
    {
        [XmlAttribute("Name")]
        public string SpellName { get; set; }

        [XmlAttribute("Mode")]
        public SpellTargetMode TargetMode { get; set; }

        [XmlAttribute("Target")]
        [DefaultValue(null)]
        public string TargetName { get; set; }

        [XmlAttribute("X")]
        [DefaultValue(0)]
        public double LocationX { get; set; }

        [XmlAttribute("Y")]
        [DefaultValue(0)]
        public double LocationY { get; set; }

        [XmlAttribute("OffsetX")]
        [DefaultValue(0)]
        public double OffsetX { get; set; }

        [XmlAttribute("OffsetY")]
        [DefaultValue(0)]
        public double OffsetY { get; set; }

        [XmlAttribute("InnerRadius")]
        [DefaultValue(0)]
        public int InnerRadius { get; set; }

        [XmlAttribute("OuterRadius")]
        [DefaultValue(0)]
        public int OuterRadius { get; set; }

        [XmlAttribute("TargetLevel")]
        [DefaultValue(0)]
        public int TargetLevel { get; set; }

        public override string ToString() => SpellName;
    }
}
