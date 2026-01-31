namespace RLF.Core.Needs
{
    public sealed class NeedState
    {
        public NeedType Type { get; }
        public float Value { get; private set; }
        public float Max { get; }

        public NeedState(NeedType type, float initialValue, float maxValue)
        {
            Type = type;
            Max = maxValue <= 0f ? 100f : maxValue;
            Value = Clamp(initialValue);
        }

        public void Decrease(float amount)
        {
            if (amount <= 0f) return;
            Value = Clamp(Value - amount);
        }

        public void Increase(float amount)
        {
            if (amount <= 0f) return;
            Value = Clamp(Value + amount);
        }

        private float Clamp(float value)
        {
            if (value < 0f) return 0f;
            if (value > Max) return Max;
            return value;
        }
    }
}
