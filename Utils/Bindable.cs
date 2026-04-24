using System;

namespace Tewi.Helpers
{
    public class Bindable<T>
    {
        private T _value;
        public event Action<T> OnChanged;

        public T Value
        {
            get => _value;
            set
            {
                if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    OnChanged?.Invoke(_value);
                }
            }
        }

        public Bindable(T defaultValue = default) => _value = defaultValue;
    }
}
