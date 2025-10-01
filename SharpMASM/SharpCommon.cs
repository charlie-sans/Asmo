using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpCommon
{
    public class Common
    {
        public static string[] MASMerrormessages = {"oh god it exploded!!!", "that's not good, did you push deez?", "uhh ohhh that's not good", "who patted the cat aggressively"};
        public static void box(string title, string message, string type)
        {
            string color;
            bool isError = false;
            switch (type.ToLower())
            {
                case "error":
                    isError = true;
                    color = "\u001B[31m"; // Red
                    break;
                case "info":
                default:
                    color = "\u001B[34m"; // Blue
                    break;
            }

            string reset = "\u001B[0m";
            string[] lines = message.Split('\n');
            if (isError)
            {
                title = "Error: " + title;
                int maxLength = title.Length;
                foreach (string line in lines)
                {
                    if (line.Length > maxLength)
                    {
                        maxLength = line.Length;
                    }
                }

                string border = "+" + new string('-', maxLength + 2) + "+";
                Console.WriteLine(color + border);
                Console.WriteLine("| " + title + new string(' ', maxLength - title.Length) + " |");
                Console.WriteLine(border);

                foreach (string line in lines)
                {
                    Console.WriteLine("| " + line + new string(' ', maxLength - line.Length) + " |");
                }

                Console.WriteLine(border + reset);
            }
            else
            {
                int maxLength = title.Length;
                foreach (string line in lines)
                {
                    if (line.Length > maxLength)
                    {
                        maxLength = line.Length;
                    }
                }

                string border = "+" + new string('-', maxLength + 2) + "+";
                Console.WriteLine(color + border);
                Console.WriteLine("| " + title + new string(' ', maxLength - title.Length) + " |");
                Console.WriteLine(border);

                foreach (string line in lines)
                {
                    Console.WriteLine("| " + line + new string(' ', maxLength - line.Length) + " |");
                }

                Console.WriteLine(border + reset);
            }
        }

        // Overloaded method for backward compatibility
        public static void box(string title, string message)
        {
            box(title, message, "info");
        }

  

        public static string[] Split(string input)
        {
            return input.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
