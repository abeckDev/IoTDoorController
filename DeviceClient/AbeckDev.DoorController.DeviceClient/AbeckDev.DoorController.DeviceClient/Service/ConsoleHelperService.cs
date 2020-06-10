using System;
using System.Collections.Generic;
using System.Text;

namespace AbeckDev.DoorController.DeviceClient.Service
{
    public static class ConsoleHelperService
    {
        public static void colorMessage(string text, ConsoleColor clr)
        {
            Console.ForegroundColor = clr;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        public static void greenMessage(string text)
        {
            colorMessage(text, ConsoleColor.Green);
        }

        public static void redMessage(string text)
        {
            colorMessage(text, ConsoleColor.Red);
        }
    }
}
