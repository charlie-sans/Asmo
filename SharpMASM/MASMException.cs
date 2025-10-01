using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpCommon;


/*
 * Main Exception class that is thrown when an error occurs in the MASM code.
 */

namespace SharpMASM
{
    public class MASMException : Exception
    {

        private int? v1;
        private string? v2;
        private string? v3;

        public MASMException(string message) : base(message)
        {
            if (CmdArgs.GetInstance().Verbose || CmdArgs.GetInstance().VeryVerbose)
            {
                Console.WriteLine($"MASMException: {message}");
            }
            //TODO: fix this quick
           // Common.box(Common.RandomString(Common.MASMerrormessages)), message, "error");

        }

        public MASMException(string message, int v1, string v2, string v3) : this(message)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }

        public override string ToString()
        {
            // Customize the output message here
            return $"MASMException: {Message}\nStack Trace: {StackTrace}";
        }
    }
}
