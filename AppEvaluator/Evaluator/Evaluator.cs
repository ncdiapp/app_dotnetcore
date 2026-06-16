using System;
#if NETFRAMEWORK
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
#else
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Concurrent;
#endif
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;

namespace ExpressionEval
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class Evaluator
	{
    #region Construction
		public Evaluator(EvaluatorItem[] items)
		{
      ConstructEvaluator(items);
		}

    public Evaluator(Type returnType, string expression, string name)
    {
      EvaluatorItem[] items = { new EvaluatorItem(returnType, expression, name) };
      ConstructEvaluator(items);
    }

    public Evaluator(EvaluatorItem item)
    {
      EvaluatorItem[] items = { item };
      ConstructEvaluator(items);
    }

    private void ConstructEvaluator(EvaluatorItem[] items)
    {
#if NETFRAMEWORK

      CSharpCodeProvider codeProvider = new CSharpCodeProvider();
      CompilerParameters cp = new CompilerParameters();
      cp.ReferencedAssemblies.Add("system.dll");
      cp.ReferencedAssemblies.Add("system.data.dll");
      cp.ReferencedAssemblies.Add("system.xml.dll");
      cp.GenerateExecutable = false;
      cp.GenerateInMemory = true;

      StringBuilder code = new StringBuilder();
      code.Append("using System; \n");
      code.Append("using System.Data; \n");
      code.Append(" using System.Globalization; \n");



      code.Append("using System.Xml; \n");
      code.Append("namespace PLMRunTimeCompiler { \n");
      code.Append("  public class _Evaluator { \n");
      foreach(EvaluatorItem item in items)
      {
        code.AppendFormat("    public {0} {1}() ",
                          item.ReturnType.Name,
                          item.Name);
        code.Append("{ ");
        code.AppendFormat("      return ({0}); ", item.Expression);
        code.Append("}\n");
      };
      code.Append (@"  public static bool IsDDLHasValue(object value)
        {
             int ?  v= value as int?;

             return v.HasValue;

        }");

      code.Append(@"  public static bool IsDateHasValue(object sourceValue)
        {
             if (sourceValue == null) return false;

            DateTime outvalue;
            if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
            {
                return true;
            }
            return false;

        }");



      code.Append(@"  public static bool IsNumericHasValue(object value)
              {
                   double  ?  v= value as double ?;

                   return v.HasValue;

              }");



      code.Append(@"  public static bool IsChecBoxkHasValue(object sourceValue)
        {
             if (sourceValue == null) return false;

            Boolean outvalue;
            if (Boolean.TryParse(sourceValue.ToString(), out outvalue))
            {
                return true;
            }

            return false;

        }");


      code.Append(@"   public static DateTime? ConvertValueToDate(object sourceValue)
        {
            if (sourceValue == null) return null;

            DateTime outvalue;
            if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }

            return null;
        }");


      code.Append(@"   public static Boolean? ConvertValueToBoolean(object sourceValue)
        {
            if (sourceValue == null) return null;

            Boolean outvalue;
            if (Boolean.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }

            return null;
        }");


      code.Append(@"   public static int? ConvertValueToInt(object sourceValue)
        {
            if (sourceValue == null) return null;

            int outvalue;
            if (int.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }

            return null;
        }");

      code.Append(@"    public static decimal? ConvertValueToDecimal(object sourceValue)
        {
            if (sourceValue == null) return null;

            decimal outvalue;
            if (decimal.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue;
            }
            return null;
        }");


      code.Append(@"     public static int GetDay(object sourceValue)
        {
            if (sourceValue == null) return 0;

            DateTime outvalue;
            if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
            {
                return  outvalue.Day;
            }

            return 0;

        }");

      code.Append(@"      public static int GetMonth(object sourceValue)
        {
            if (sourceValue == null) return 0;

            DateTime outvalue;
            if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue.Month;
            }

            return 0;

        }");


      code.Append(@"   public static int GetYear(object sourceValue)
        {
            if (sourceValue == null) return 0;

            DateTime outvalue;
            if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
            {
                return outvalue.Year;
            }

            return 0;

        }");


      code.Append(@"     public static int GetWeekOfYear(object sourceValue)
        {

            if (sourceValue == null) return 0;

            DateTime time;
            if (DateTime.TryParse(sourceValue.ToString(), out time))
            {
                DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
                if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
                {
                    time = time.AddDays(3);
                }

                return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            }

            return 0;


        }  ");

      code.Append(@"     public static int GetDayOfYear(object sourceValue)
        {
            if (sourceValue == null) return 0;

            DateTime outvalue;
            if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
            {
                return (int)outvalue.DayOfYear;
            }

            return 0;

        } ");


      code.Append(@"
        public static DateTime FirstDateOfWeek(int year, int weekOfYear)
        {
            System.Globalization.CultureInfo ci = CultureInfo.CurrentCulture;
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = (int)ci.DateTimeFormat.FirstDayOfWeek - (int)jan1.DayOfWeek;
            DateTime firstWeekDay = jan1.AddDays(daysOffset);
            int firstWeek = ci.Calendar.GetWeekOfYear(jan1, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek);
            if ((firstWeek <= 1 || firstWeek >= 52) && daysOffset >= -3)
            {
                weekOfYear -= 1;
            }
            return firstWeekDay.AddDays(weekOfYear * 7);
        }");




      code.Append("} }");

      CompilerResults cr = codeProvider.CompileAssemblyFromSource(cp, code.ToString());
      if (cr.Errors.HasErrors)
      {
        StringBuilder error = new StringBuilder();
        error.Append("Error Compiling Expression: ");
        foreach (CompilerError err in cr.Errors)
        {
          error.AppendFormat("{0}\n", err.ErrorText);
        }
        throw new Exception("Error Compiling Expression: " + error.ToString());
      }
      Assembly a = cr.CompiledAssembly;
      _Compiled = a.CreateInstance("PLMRunTimeCompiler._Evaluator");

#else

      _items = new Dictionary<string, EvaluatorItem>(items.Length);
      foreach (EvaluatorItem item in items)
      {
        _items[item.Name] = item;
      }
      _Compiled = null; // not used on .NET 10

#endif
    }
    #endregion

    #region Public Members
    public int EvaluateInt(string name)
    {
      return (int) Evaluate(name);
    }

    public string EvaluateString(string name)
    {
      return (string) Evaluate(name);
    }

    public bool EvaluateBool(string name)
    {
      return (bool) Evaluate(name);
    }

    public object Evaluate(string name)
    {
#if NETFRAMEWORK
      MethodInfo mi = _Compiled.GetType().GetMethod(name);
      return mi.Invoke(_Compiled, null);
#else
      if (_items == null || !_items.TryGetValue(name, out var item))
        throw new InvalidOperationException($"No expression named '{name}'.");
      return RunCached(item.Expression);
#endif
    }
    #endregion

    #region Static Members
#if !NETFRAMEWORK
    private static readonly ScriptOptions _scriptOptions = ScriptOptions.Default
        .AddImports("System", "System.Data", "System.Xml", "System.Globalization");
    private static readonly ConcurrentDictionary<string, Script<object>> _scriptCache = new();

    private static object RunCached(string code)
    {
      var script = _scriptCache.GetOrAdd(code, c => CSharpScript.Create<object>(c, _scriptOptions));
      return script.RunAsync().GetAwaiter().GetResult().ReturnValue;
    }
#endif

    static public int EvaluateToInteger(string code)
    {
#if NETFRAMEWORK
      Evaluator eval = new Evaluator(typeof(int), code, staticMethodName);
      return (int) eval.Evaluate(staticMethodName);
#else
      return (int) RunCached(code);
#endif
    }

    static public string EvaluateToString(string code)
    {
#if NETFRAMEWORK
      Evaluator eval = new Evaluator(typeof(string), code, staticMethodName);
      return (string) eval.Evaluate(staticMethodName);
#else
      return (string) RunCached(code);
#endif
    }

    static public bool EvaluateToBool(string code)
    {
#if NETFRAMEWORK
      Evaluator eval = new Evaluator(typeof(bool), code, staticMethodName);
      return (bool) eval.Evaluate(staticMethodName);
#else
      return (bool) RunCached(code);
#endif
    }

    static public object EvaluateToObject(string code)
    {
#if NETFRAMEWORK
      Evaluator eval = new Evaluator(typeof(object), code, staticMethodName);
      return eval.Evaluate(staticMethodName);
#else
      return RunCached(code);
#endif
    }
    #endregion

    #region Private
    const string staticMethodName = "__foo";
    object? _Compiled = null;
#if !NETFRAMEWORK
    private Dictionary<string, EvaluatorItem>? _items;
#endif
    #endregion
	}

  public class EvaluatorItem
  {
    public EvaluatorItem(Type returnType, string expression, string name)
    {
      ReturnType = returnType;
      Expression = expression;
      Name = name;
    }

    public Type ReturnType;
    public string Name;
    public string Expression;
  }

  /// <summary>
  /// Static helper methods previously compiled into the dynamic CodeDom assembly.
  /// On .NET 10 these are available directly; expressions that call them should
  /// reference this class (e.g. EvaluatorHelpers.IsDDLHasValue(...)).
  /// </summary>
  public static class EvaluatorHelpers
  {
    public static bool IsDDLHasValue(object value)
    {
      int? v = value as int?;
      return v.HasValue;
    }

    public static bool IsDateHasValue(object sourceValue)
    {
      if (sourceValue == null) return false;

      DateTime outvalue;
      if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
      {
        return true;
      }
      return false;
    }

    public static bool IsNumericHasValue(object value)
    {
      double? v = value as double?;
      return v.HasValue;
    }

    public static bool IsChecBoxkHasValue(object sourceValue)
    {
      if (sourceValue == null) return false;

      Boolean outvalue;
      if (Boolean.TryParse(sourceValue.ToString(), out outvalue))
      {
        return true;
      }
      return false;
    }

    public static DateTime? ConvertValueToDate(object sourceValue)
    {
      if (sourceValue == null) return null;

      DateTime outvalue;
      if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
      {
        return outvalue;
      }
      return null;
    }

    public static Boolean? ConvertValueToBoolean(object sourceValue)
    {
      if (sourceValue == null) return null;

      Boolean outvalue;
      if (Boolean.TryParse(sourceValue.ToString(), out outvalue))
      {
        return outvalue;
      }
      return null;
    }

    public static int? ConvertValueToInt(object sourceValue)
    {
      if (sourceValue == null) return null;

      int outvalue;
      if (int.TryParse(sourceValue.ToString(), out outvalue))
      {
        return outvalue;
      }
      return null;
    }

    public static decimal? ConvertValueToDecimal(object sourceValue)
    {
      if (sourceValue == null) return null;

      decimal outvalue;
      if (decimal.TryParse(sourceValue.ToString(), out outvalue))
      {
        return outvalue;
      }
      return null;
    }

    public static int GetDay(object sourceValue)
    {
      if (sourceValue == null) return 0;

      DateTime outvalue;
      if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
      {
        return outvalue.Day;
      }
      return 0;
    }

    public static int GetMonth(object sourceValue)
    {
      if (sourceValue == null) return 0;

      DateTime outvalue;
      if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
      {
        return outvalue.Month;
      }
      return 0;
    }

    public static int GetYear(object sourceValue)
    {
      if (sourceValue == null) return 0;

      DateTime outvalue;
      if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
      {
        return outvalue.Year;
      }
      return 0;
    }

    public static int GetWeekOfYear(object sourceValue)
    {
      if (sourceValue == null) return 0;

      DateTime time;
      if (DateTime.TryParse(sourceValue.ToString(), out time))
      {
        DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
        {
          time = time.AddDays(3);
        }
        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
      }
      return 0;
    }

    public static int GetDayOfYear(object sourceValue)
    {
      if (sourceValue == null) return 0;

      DateTime outvalue;
      if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
      {
        return (int)outvalue.DayOfYear;
      }
      return 0;
    }

    public static DateTime FirstDateOfWeek(int year, int weekOfYear)
    {
      CultureInfo ci = CultureInfo.CurrentCulture;
      DateTime jan1 = new DateTime(year, 1, 1);
      int daysOffset = (int)ci.DateTimeFormat.FirstDayOfWeek - (int)jan1.DayOfWeek;
      DateTime firstWeekDay = jan1.AddDays(daysOffset);
      int firstWeek = ci.Calendar.GetWeekOfYear(jan1, ci.DateTimeFormat.CalendarWeekRule, ci.DateTimeFormat.FirstDayOfWeek);
      if ((firstWeek <= 1 || firstWeek >= 52) && daysOffset >= -3)
      {
        weekOfYear -= 1;
      }
      return firstWeekDay.AddDays(weekOfYear * 7);
    }
  }
}

//subitemid_7887=IsDDLHasValue(subitemid_6541) && !string.IsNullOrEmpty(subitemid_7640) && subitemid_7980 =="A" && subitemid_7981>0
//subitemid_7896=!string.IsNullOrEmpty(subitemid_6641)
//subitemid_7903=!string.IsNullOrEmpty(subitemid_7713)
//subitemid_7904=!string.IsNullOrEmpty(subitemid_7847)
//subitemid_8615=(subitemid_8527>0 && !string.IsNullOrEmpty(subitemid_8608)) || subitemid_8527==0
//subitemid_7997=string.IsNullOrEmpty(subitemid_7743) && IsDDLHasValue(subitemid_7737)
//subitemid_7998=!string.IsNullOrEmpty(subitemid_4687)
//subitemid_8056=subitemid_7980=="A" || string.IsNullOrEmpty(subitemid_7980)
//subitemid_7965=(!string.IsNullOrEmpty(subitemid_7796)  &&  !subitemid_7799&&subitemid_7966&&subitemid_8113) || (subitemid_7799) || (!subitemid_8113)
//subitemid_8105=!string.IsNullOrEmpty(subitemid_7950)
//subitemid_8107=string.IsNullOrEmpty(subitemid_6540)
//subitemid_8203=(string.IsNullOrEmpty(subitemid_6620)   || !subitemid_8113) &&  (!subitemid_7799)
//subitemid_9284=[DateNull]
