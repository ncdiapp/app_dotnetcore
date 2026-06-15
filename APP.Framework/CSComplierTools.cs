using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
#if NETFRAMEWORK
using Microsoft.CSharp;
using System.CodeDom.Compiler;
#endif

namespace APP.Framework
{
	public static class CSComplierTools
	{
		// TODO-PHASE3: Replace with Microsoft.CodeAnalysis.CSharp.Scripting (Roslyn) on .NET 10.
		// CSharpCodeProvider is .NET Framework only; guarded here to allow net10.0 compilation.
		public static Assembly CompileAssembly(string[] sourceFiles, string outputAssemblyPath)
		{
#if NETFRAMEWORK
			var codeProvider = new CSharpCodeProvider();

			var compilerParameters = new CompilerParameters
			{
				GenerateExecutable = false,     // Make a DLL
				GenerateInMemory = false,       // Explicitly save it to path specified by compilerParameters.OutputAssembly
				IncludeDebugInformation = true, // Enable debugging - generate .pdb
				OutputAssembly = outputAssemblyPath
			};

			compilerParameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);

			var result = codeProvider.CompileAssemblyFromFile(compilerParameters, sourceFiles);

			if (result.Errors.HasErrors) throw new Exception("Assembly compilation failed.");

			return result.CompiledAssembly;
#else
			throw new PlatformNotSupportedException("Use CSharpScript.EvaluateAsync (Roslyn) on .NET 10. See TODO-PHASE3.");
#endif
		}

		/// <summary>
		/// Returns all types that implement the specified interface.
		/// </summary>
		/// <param name="assembly">Assembly to search.</param>
		/// <param name="interfaceType">Interface that types must implement.</param>
		/// <returns></returns>
		public static List<Type> GetTypesImplementingInterface(Assembly assembly, Type interfaceType)
		{
			if (!interfaceType.IsInterface) throw new ArgumentException("Not an interface.", "interfaceType");

			return assembly.GetTypes()
						   .Where(t => interfaceType.IsAssignableFrom(t))
						   .ToList();
		}

		private static void runMsbuild()
		{
			//http://superuser.com/questions/604953/how-can-i-compile-a-net-project-without-having-visual-studio-installed
			//e MSBuild.exe to compile your solution.
			var frameworkPath = RuntimeEnvironment.GetRuntimeDirectory();
			var cscPath = Path.Combine(frameworkPath, "MSBuild.exe");

			//Console.WriteLine(frameworkPath);  // C:\Windows\Microsoft.NET\Framework\v4.0.30319
			//Console.WriteLine(cscPath);
			//"C:\Users\Oliver\Documents\My Project\My Project.sln" /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"

			//msbuild myproject.csproj /noconsolelogger /l:ConsoleLogger,Microsoft.Build.Engine.dll;performancesummary

			string srcFodler = @"""C:\PLM Solution - Development - Test\Minification\AppExternalMethod\AppExternalMethod.csproj""" + " /t:build"; //          /p:Configuration = Release /p:Platform =" +@"""Any CPU""";



			CreateProcess(cscPath, srcFodler);


		}

		private static void runCSc()
		{
			var frameworkPath = RuntimeEnvironment.GetRuntimeDirectory();
			var cscPath = Path.Combine(frameworkPath, "csc.exe");

			//Console.WriteLine(frameworkPath);  // C:\Windows\Microsoft.NET\Framework\v4.0.30319
			//Console.WriteLine(cscPath);

			string srcFodler = @"""C:\PLM Solution - Development - Test\Minification\AppExternalMethod\testcompiler.cs""";



			CreateProcess(cscPath, srcFodler);


		}


		private static void CreateProcess(string program, string arguments)
		{

			// Use ProcessStartInfo class
			ProcessStartInfo startInfo = new ProcessStartInfo();

			startInfo.FileName = program;

			startInfo.Arguments = arguments;

			startInfo.UseShellExecute = false;

			startInfo.RedirectStandardOutput = true;
			startInfo.CreateNoWindow = true;




			try
			{

				using (Process exeProcess = Process.Start(startInfo))
				{
					string line = string.Empty;

					while (!exeProcess.StandardOutput.EndOfStream)
					{
						line = line + System.Environment.NewLine + exeProcess.StandardOutput.ReadLine();
						// do something with line
					}

					//exeProcess.WaitForExit();


				}
			}
			catch (Exception ex)
			{

			}
			//Process process = new Process();

			//process.StartInfo.FileName = program;
			//process.StartInfo.Arguments = arguments;
			//process.StartInfo.UseShellExecute = true;
			//process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
			//process.StartInfo.CreateNoWindow = false;

			//process.Start();

			//process.StandardOutput.ReadToEnd();
			//process.WaitForExit();




		}
	}
}
