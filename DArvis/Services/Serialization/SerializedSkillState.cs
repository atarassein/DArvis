using System;
using System.Xml.Serialization;

namespace DArvis.Services.Serialization
{
    [Serializable]
    public sealed class SerializedSkillState
    {
        [XmlAttribute("Name")]
        public string SkillName { get; set; }

        public SerializedSkillState() { }

        public override string ToString() => SkillName;
    }
}
