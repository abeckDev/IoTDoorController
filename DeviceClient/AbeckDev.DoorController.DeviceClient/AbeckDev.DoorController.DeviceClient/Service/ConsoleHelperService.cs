using System;
using System.Collections.Generic;
using System.Text;

namespace AbeckDev.DoorController.DeviceClient.Service
{
    public static class ConsoleHelperService
    {
        /// <summary>
        /// Outputs a Text with the given Color
        /// </summary>
        /// <param name="text">The Text to print</param>
        /// <param name="clr">The color which should be used to print the text</param>
        public static void colorMessage(string text, ConsoleColor clr)
        {
            Console.ForegroundColor = clr;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        /// <summary>
        /// Send a green message to console
        /// </summary>
        /// <param name="text"></param>
        public static void greenMessage(string text)
        {
            colorMessage(text, ConsoleColor.Green);
        }

        /// <summary>
        /// Send a red message to console
        /// </summary>
        /// <param name="text"></param>
        public static void redMessage(string text)
        {
            colorMessage(text, ConsoleColor.Red);
        }
    }
}
