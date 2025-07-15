﻿
namespace DArvis.Common
{
    public class DisplayValue : ObservableObject
    {
        private string displayName = string.Empty;
        private object value;

        public string DisplayName
        {
            get => displayName;
            set => SetProperty(ref displayName, value);
        }

        public object Value
        {
            get => value;
            set => SetProperty(ref this.value, value);
        }

        public override string ToString() => DisplayName;
    }
}
