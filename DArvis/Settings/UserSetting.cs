﻿using System;
using System.ComponentModel;
using System.Xml.Serialization;

using DArvis.Common;

namespace DArvis.Settings
{
    [Serializable]
    public sealed class UserSetting : ObservableObject
    {
        private string key;
        private string displayText;
        private object value;

        [XmlAttribute("Key")]
        public string Key
        {
            get => key;
            set => SetProperty(ref key, value);
        }

        [XmlIgnore]
        public string DisplayText
        {
            get => displayText;
            set => SetProperty(ref displayText, value);
        }

        [XmlAttribute("Value")]
        [DefaultValue(null)]
        public object Value
        {
            get => value;
            set => SetProperty(ref this.value, value);
        }

        // Needed for XAML -- Do not remove!
        public UserSetting() { }

        public UserSetting(string key, string displayText, object value = null)
        {
            this.key = key;
            this.displayText = displayText;
            this.value = value;
        }

        public override string ToString() => DisplayText ?? Key;
    }
}
