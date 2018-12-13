using System;

namespace TrunkBot.Labeling
{
    public class Label
    {
        internal string Name;
        internal DateTime Date;

        public Label(string name, DateTime date)
        {
            Name = name;
            Date = date;
        }
    }
}
