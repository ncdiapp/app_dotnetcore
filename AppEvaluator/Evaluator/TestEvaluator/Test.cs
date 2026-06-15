using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace PlmExpressionEval
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Test
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
        [STAThread]

        static void Mainday(string[] args)
        {
            var date = System.DateTime.Now;
            Console.WriteLine("GetDay: {0}", GetDay(date));
            Console.WriteLine("GetMonth: {0}", GetMonth(date));
            Console.WriteLine("GetYear: {0}", GetYear(date));

            Console.WriteLine("GetDayOfWeek: {0}", GetDayOfWeek(date));
            Console.WriteLine("GetDayOfYear: {0}", GetDayOfYear(date));
            Console.WriteLine("GetWeekOfYear: {0}", GetWeekOfYear(date));

            Console.WriteLine("GetWeekOfYear: {0}", FirstDateOfWeek(GetYear(date), GetWeekOfYear(date)));

            Console.WriteLine("GetWeekOfYear 2016 1 week: {0}", FirstDateOfWeek(2016, 1));

            
           



        }
		static void Main(string[] args)
		{


			Evaluator e = new Evaluator();
			//e.EvaluateInt()
		 //   Console.WriteLine("Test0: {0}", Evaluator.EvaluateToInteger("(30 + 4) * 2"));
		 //  Console.WriteLine("Test1: {0}", Evaluator.EvaluateToString("\"Hello \" + \"There\""));
		 // Console.WriteLine("Test2: {0}", Evaluator.EvaluateToBool("30 == 40"));
		 //  Console.WriteLine("Test3: {0}", Evaluator.EvaluateToObject("new DataSet()"));

			// Console.WriteLine("Test5: {0}", Evaluator.EvaluateToObject("System.DateTime.Now.AddDays(3)"));

			//var date= System.DateTime.Now.AddDays(3);

			//Console.WriteLine("Test5: {0}", date.ToString ());

			//System.DateTime aDate = new DateTime(1979, 7, 8).AddDays(4); 

			// bool result = string.Empty == null || "" ==string.Empty ;

			//string evaluee = @"string.Empty == null || "" == null";

			//bool result = string.IsNullOrEmpty("");

			//string evaluee2 = "string.IsNullOrEmpty(\"\")";

			//object evalue = Evaluator.EvaluateToObject(evaluee2);


			//  bool  value =  IsIntHasValue(123);





			// int op;
			//  int.TryParse("1233", out op);

			// string ress= @"  int op; int.TryParse(" +1233+", out op); ";


			//IsDDLHasValue
			//IsDateHasValue
			//IsImageHasValue
			//IsNumericHasValue
			//IsStringHasValue

			decimal decail = 10.0m;

            string dt = System.DateTime.Now.ToString ();


			Stopwatch s = new Stopwatch();
			s.Start();
 
            object t1 = Evaluator.EvaluateToObject("IsDDLHasValue(null)");

			s.Stop();

			var t0 = s.Elapsed;

            object t2 = Evaluator.EvaluateToObject("IsDDLHasValue(1234)");

            object t3 = Evaluator.EvaluateToObject("IsDateHasValue(\""+dt+"\")");
            object t4 = Evaluator.EvaluateToObject("IsDateHasValue(null)");
          
            object t5 = Evaluator.EvaluateToObject("IsNumericHasValue(" + decail + ")");


            object ttrue = Evaluator.EvaluateToObject("IsChecBoxkHasValue(true)");

            object ttfasel = Evaluator.EvaluateToObject("IsChecBoxkHasValue(false)");

            object ttfaselempty = Evaluator.EvaluateToObject("IsChecBoxkHasValue( null )");

            Test2 atest2 = new Test2();
            Test3 atest3 = new Test3();





        ThreadStart aStart = ()=>
            {
                for ( int i=0;i<100;i++)
                {
                    EvaluatorItem[] items = {
                                new EvaluatorItem(typeof(int), "(30 + 4) * 2", "GetNumber"),
                                new EvaluatorItem(typeof(string), "\"Hello \" + \"There\"", "GetString"),
                                new EvaluatorItem(typeof(bool), "30 == 40", "GetBool"),
                                new EvaluatorItem(typeof(object), "new DateTime(1979, 7, 8).AddDays(4).AddDays(4)", "GetDate"),
                                new EvaluatorItem(typeof(object), " (new DateTime(1979, 7, 8) - ( new DateTime(1979, 7,8).AddHours (-1))).TotalHours ", "GetHours")
                              };

                    Evaluator eval = new Evaluator(items);
                    Console.WriteLine("TestStatic0: {0}", eval.EvaluateInt("GetNumber"));
                    Console.WriteLine("TestStatic1: {0}", eval.EvaluateString("GetString"));
                    Console.WriteLine("TestStatic2: {0}", eval.EvaluateBool("GetBool"));
                    Console.WriteLine("TestStatic3: {0}", eval.Evaluate("GetDate"));
                    Console.WriteLine("TestStatic GetHours: {0}", eval.Evaluate("GetHours"));
                    if (i == 50)
                    {
                        atest3.Name = i.ToString() + "can not destructor";
                        Thread.Sleep(1000);

                       // new DateTime(1979, 7, 8).AddHours (
                    }
                    
                  
                }
            
            };
                Thread   aThread = new Thread(aStart);
                aThread.Start();
 


    }


        public static bool IsIntHasValue(object value)
        {
             int ?  v= value as int?;

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

        public static int GetDay(object sourceValue)
        {
            if (sourceValue == null) return 0;

            DateTime outvalue;
            if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
            {
                return  outvalue.Day;
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

        public static int GetDayOfWeek(object sourceValue)
        {
            if (sourceValue == null) return 0;

            DateTime outvalue;
            if (DateTime.TryParse(sourceValue.ToString(), out outvalue))
            {
                return (int)outvalue.DayOfWeek;
            }

            return 0;

        }

        //Iso8601
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
        }

      
	}


    public class  Test2
    {
        ~Test2()
        {
            System.Console.Write ("destrcme)")  ;
        
        }


       public Test2()
        {
            System.Console.Write("construct)");

        }
    
    }

    public class Test3
    {
        ~Test3()
        {
           

        }


        public string Name
        {
            get;
            set;
        }
        public Test3()
        {
            System.Console.Write("construct)");

        }

    }
}
