namespace BuildHelper.Workflows
{
    using System.Collections.Generic;

    public class CustomValueRange
    {
        public virtual string MajorDimension { get; set; }

        public virtual string Range { get; set; }

        public virtual IList<IList<object>> Values { get; set; }

        public virtual string ETag { get; set; }
    }
}