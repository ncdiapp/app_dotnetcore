using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
namespace DynamicAssemblies
{
    class testCodeDom
    {
        [STAThread]
        static void Main(string[] args)
        {
            CCodeGenerator.Main1CcodeGen();
            CsharpCodeGenSample.MainSample();

            Console.ReadLine();
        }

       
    }
}