namespace Geomoir.Data
{
    public class Ref<T>
    {
        public T Value { get; set; }

        public Ref() { }

        public Ref(T Value)
        {
            this.Value = Value;
        }
    }

    public class Ref<T1, T2>
    {
        public T1 Value1 { get; set; }
        public T2 Value2 { get; set; }

        public Ref() { }

        public Ref(T1 Value1, T2 Value2)
        {
            this.Value1 = Value1;
            this.Value2 = Value2;
        }
    }
}
