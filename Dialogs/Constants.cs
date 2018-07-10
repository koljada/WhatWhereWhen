using System;

namespace SimpleEchoBot.Dialogs
{
    public class Constants
    {
        public static readonly string HELP = "Commands: " + Environment.NewLine +
                    "\t\t  - type `new` to get a new question;" + Environment.NewLine +
                    "\t\t  - type `answer` to get an answer to the current question; " +
                        "By default a new question will be kicked off. If you don't want that you can add a `-off` parameter. Like `answer -off`" + Environment.NewLine+
                    "\t\t  - type `level [0-5]` to set question's complexity level(from 1 to 5). Use 0 for a random level(set by default)";

    }
}