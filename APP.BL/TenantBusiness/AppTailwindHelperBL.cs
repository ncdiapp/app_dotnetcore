using System.Collections.Generic;
using System.Data;
using System.Linq;
using System;
using APP.Components.EntityDto;
using System.Data.Common;
using APP.Components.Dto;
using System.Diagnostics;
using System.IO;
using AngleSharp.Dom;
using AngleSharp;
using System.Text;
using System.Threading.Tasks;
using APP.Components.EntityConverter;
using APP.Framework.Communication;
using APP.Framework.Validation;
using APP.LBL.DatabaseSpecific;
using APP.LBL.EntityClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using APP.LBL;
using System.Text.RegularExpressions;

using System.Drawing;
using Newtonsoft.Json;
using System.Dynamic;

using AngleSharp.Html.Parser;
using Google.Protobuf.WellKnownTypes;

using APP.Framework;
namespace App.BL
{
    public static class AppTailwindHelperBL
    {
        private static readonly Dictionary<string, Dictionary<string, string>> TailWindMap = GetTailWindMapConstants();

        private static readonly List<double> PaddingArray = new List<double>
        {
            0, 0.25, 0.5, 0.75, 1, 1.25, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10, 12, 14, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64, 72, 80, 96
        };

        private static readonly List<double> MarginArray = new List<double>
        {
            -0.25, -0.5, -0.75, -1, -1.25, -1.5, -2, -2.5, -3, -4, -5, -6, -8, -10, -12, -14, -16, -20, -24, -28, -32, -36, -40, -44, -48, -52, -56, -60, -64, -72, -80, -96,
            0, 0.25, 0.5, 0.75, 1, 1.25, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10, 12, 14, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64, 72, 80, 96
        };

        private static readonly List<double> DimensionArray = new List<double>
        {
            0, 0.25, 0.5, 0.75, 1, 1.25, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10, 12, 14, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64, 72, 80, 96
        };

        private static readonly List<double> FontSizeArray = new List<double>
        {
            0.75, 0.875, 1, 1.125, 1.25, 1.5, 1.875, 2.25, 2.5, 3, 3.75, 4.5, 6, 8
        };

        private static readonly List<double> LineHeightArray = new List<double>
        {
            0.75, 1, 1.25, 1.5, 1.75, 2, 2.25, 2.5
        };

        private static readonly List<double> BorderRadiusArray = new List<double> { 0, 0.125, 0.25, 0.375, 0.5, 0.75, 1, 1.5 };


        private static readonly int ColorMatchMaxDistance = 10;


       


        public static string ConvertOneHtmlPageInlineStylesToTailwind(string html, TailwindConvertSettingDto settings)
        {
            var parser = new HtmlParser();
            var document = parser.ParseDocument(html);

            // Use document.Body to get only the content within the <body> tag
            var bodyElement = document.Body;

            // Ensure we have a valid body element
            if (bodyElement != null)
            {
                foreach (var element in bodyElement.QuerySelectorAll("*"))
                {
                    var style = element.GetAttribute("style");
                    if (!string.IsNullOrEmpty(style))
                    {
                        var originalStyles = new Dictionary<string, string>();

                        var styleProperties = style.Split(';')
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToArray();

                        foreach (var propertyValue in styleProperties)
                        {
                            var parts = propertyValue.Split(':');
                            if (parts.Length == 2)
                            {
                                var property = parts[0].Trim();
                                var value = parts[1].Trim();

                                var tailWindStyles = new List<string>();
                                var errors = new List<string>();

                                bool success = ConvertOneStylePropertyToTailwindClass(property, value, tailWindStyles, errors, settings);

                                if (success)
                                {
                                    if (tailWindStyles.Any())
                                    {
                                        element.SetAttribute("class", ((element.GetAttribute("class") ?? "") + " " + string.Join(" ", tailWindStyles)).Trim());
                                    }
                                }
                                else
                                {
                                    originalStyles[property] = value;
                                }
                            }
                        }

                        if (originalStyles.Any())
                        {
                            var newStyle = string.Join("; ", originalStyles.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                            element.SetAttribute("style", newStyle);
                        }
                        else
                        {
                            element.RemoveAttribute("style");
                        }
                    }
                }
            }

            // Return only the content within the <body> tag
            return bodyElement.InnerHtml;
        }



        public static bool ConvertOneStylePropertyToTailwindClass(
            string property,
            string value,
            List<string> tailWindStyles,
            List<string> errors,
            TailwindConvertSettingDto settings)
        {
            string processedProperty = ProcessProperty(property, value);
            string processedValue = ProcessValue(processedProperty, value, tailWindStyles, errors, settings);

            //Console.WriteLine($"{processedProperty}, {processedValue}");

            if (TailWindMap.ContainsKey(processedProperty))                
            {
                if (TailWindMap[processedProperty].ContainsKey(processedValue))
                {
                    tailWindStyles.Add(TailWindMap[processedProperty][processedValue].Substring(1));
                    return true;
                }
                else
                {
                    if (settings.UseArbitraryValueIfCannotFindDefaultClass && AppTailwindConstants.DictCssPropertyAndTwArbitraryValue.ContainsKey(processedProperty))
                    {
                        
                        string rgbaPattern = @"rgba\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+(\.\d+)?)\s*\)";
                        string rgbPattern = @"rgb\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)";

                        string arbitraryValue = Regex.Replace(value, rgbaPattern, "rgba($1,$2,$3,$4)");

                        arbitraryValue = Regex.Replace(arbitraryValue, rgbPattern, "rgb($1,$2,$3)").Trim();

                        if (!arbitraryValue.Contains(" "))
                        {
                            if (arbitraryValue != "rgba(0,0,0,0)")
                            {
                                tailWindStyles.Add(AppTailwindConstants.DictCssPropertyAndTwArbitraryValue[processedProperty].Replace("value", arbitraryValue));
                            }

                            return true;
                        }
                        else
                        {
                            errors.Add($"{property}: {value};");
                            return false;
                        }                        
                    }
                    else
                    {
                        errors.Add($"{property}: {value};");
                        return false;
                    }                    
                }                
            }
            else
            {
                errors.Add($"{property}: {value};");
                return false;
            }
        }

        private static string ProcessProperty(string property, string value)
        {
            switch (property)
            {
                case "background":
                    if (IsColor(value))
                    {
                        return "background-color";
                    }
                    return property;
                default:
                    return property;
            }
        }

        private static string ProcessValue(
            string property,
            string value,
            List<string> tailWindStyles,
            List<string> errors,
            TailwindConvertSettingDto settings)
        {
            if (new List<string> { "0em", "0ex", "0ch", "0rem", "0vw", "0vh", "0%", "0px" }.Contains(value))
            {
                return "0";
            }

            switch (property)
            {
                case "color":
                case "background-color":
                case "border-color":
                    return ConvertColor(value, settings);
                case "font-weight":
                    return ConvertFontWeight(value);
                case "line-height":
                    return ConvertLineHeight(value, settings);
                case "font-size":
                    return ConvertFontSize(value, settings);
                case "height":
                case "width":
                    return ConvertDimensions(value, settings);
                case "padding":
                case "margin":
                case "padding-top":
                case "padding-left":
                case "padding-right":
                case "padding-bottom":
                case "margin-top":
                case "margin-left":
                case "margin-right":
                case "margin-bottom":
                case "top":
                case "right":
                case "bottom":
                case "left":               
                    return ConvertSpacing(property, value, tailWindStyles, errors, settings);
                case "border-radius":
                    return ConvertBorderRadius(value, settings);
                default:
                    return value;
            }
        }


        private static string ConvertPxToRem(
            List<double> remArray,
            string value,
            int conversionFactor = 16)
        {

            int? numericVal = ControlTypeValueConverter.ConvertValueToInt(value.Replace("px", ""));
                
            double min = remArray.Min();
            double max = remArray.Max();

            if (numericVal.HasValue && numericVal.Value > 0 && numericVal.Value <= conversionFactor * max && numericVal.Value >= conversionFactor * min)
            {
                double rem = numericVal.Value / (double)conversionFactor;
                //double closest = RoundToNearestRem(remArray, rem);
                //return closest == 0 ? "0" : $"{closest}rem";

                return rem == 0 ? "0" : $"{rem}rem";
            }

            return value;
        }

        private static double RoundToNearestRem(List<double> remArray, double num)
        {
            return remArray.Aggregate((prev, curr) => Math.Abs(curr - num) < Math.Abs(prev - num) ? curr : prev);
        }

        private static Dictionary<string, Dictionary<string, string>> GetTailWindMapConstants()
        {
            var tailWindMap = new Dictionary<string, Dictionary<string, string>>();

          
            foreach (var property in AppTailwindConstants.StyleConstants)
            {
                string key = property.Name;
                var nestedDict = property.Value.ToObject<Dictionary<string, string>>();
                tailWindMap[key] = nestedDict;
            }
           
            return tailWindMap;
        }



        // Converts the font weight from string to corresponding numeric value
        private static string ConvertFontWeight(string fontWeight)
        {
            switch (fontWeight.ToLower())
            {
                case "normal":
                    return "400";
                case "bold":
                    return "700";
                default:
                    return fontWeight;
            }
        }

        // Converts the font size using the convertUnit function
        private static string ConvertFontSize(string fontSize, TailwindConvertSettingDto settings)
        {
            return ConvertUnit(FontSizeArray, fontSize, settings.RemConversion, true);
        }

        // Converts the line height using the convertUnit function
        private static string ConvertLineHeight(string lineHeight, TailwindConvertSettingDto settings)
        {
            return ConvertUnit(LineHeightArray, lineHeight, settings.RemConversion, true);
        }

        private static int RemConversion { get; set; } = 16;  // Default rem conversion factor


        private static string ConvertUnit(List<double> remArray, string value, int conversionFactor = 16, bool stripLeadingZeros = false)
        {
            string converted = value;

            if (value.EndsWith("rem"))
            {
               //converted = $"{RoundToNearestRem(remArray, double.Parse(value.Replace("rem", "")))}rem";

                converted = value;
            }
            else if (value.EndsWith("px"))
            {
                converted = ConvertPxToRem(remArray, value, conversionFactor);
            }

            if (stripLeadingZeros)
            {
                converted = converted.TrimStart('0').Insert(0, ".");
            }

            return converted;
        }



        // Converts the spacing property (e.g., padding or margin) based on settings and Tailwind styles
        private static string ConvertSpacing(
            string property,
            string spacing,
            List<string> tailWindStyles,
            List<string> errors,
            TailwindConvertSettingDto settings)
        {
            if (!settings.AutoConvertSpacing)
            {
                return spacing;
            }

            // Return if specific properties and spacing do not require conversion
            if ((property == "padding" || property == "margin") && spacing == "1px")
            {
                return spacing;
            }

            if (new List<string> { "-1px", "auto" }.Contains(spacing) &&
                new List<string> { "margin", "margin-left", "margin-right", "margin-top", "margin-bottom" }.Contains(property))
            {
                return spacing;
            }

            // Handle multiple spacing values (e.g., "padding: 1rem 0.5rem;")
            var dimensions = spacing.Split(' ');

            // Choose the appropriate array for padding or margin conversions
            var remArray = property.StartsWith("padding") ? PaddingArray : MarginArray;

            // Handle single spacing values
            if (dimensions.Length == 1)
            {
                return ConvertUnit(remArray, dimensions[0], settings.RemConversion);
            }

            // Cannot handle shorthand spacing values, return original spacing
            return spacing;
        }



        // Converts dimensions based on settings
        private static string ConvertDimensions(object dimension, TailwindConvertSettingDto settings)
        {
            // Return dimension as-is if it's 0 or "1px"
            if (dimension.Equals(0) || dimension.Equals("1px"))
            {
                return dimension.ToString();
            }

            // Use UnitConverter to convert the dimension value
            return ConvertUnit(DimensionArray, dimension.ToString(), settings.RemConversion);
        }

        // Convert color using nearest color if settings allow auto color conversion
        private static string ConvertColor(string color, TailwindConvertSettingDto settings)
        {
            if (!settings.AutoConvertColor || !IsColor(color))
            {
                return color;
            }

            try
            {
                List<string> tailwindColorHexList = AppTailwindConstants.ColorConstants.Colors.ToObject<List<string>>(); 
                
                return NearestColor(tailwindColorHexList, color);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error converting color: " + e.Message);
                return color;
            }
        }

        private static bool IsColor(string strColor)
        {
            try
            {
                // Check if the string is a named color
                Color color = Color.FromName(strColor);
                if (color.IsKnownColor)
                {
                    return true;
                }

                // Check if the string is a valid Hex code
                if (Regex.IsMatch(strColor, @"^#[0-9A-F]{6}$", RegexOptions.IgnoreCase))
                {
                    return true;
                }

                // Check if the string is a valid RGB or RGBA color
                if (Regex.IsMatch(strColor, @"^rgb\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}\s*\)$", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(strColor, @"^rgba\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*(?:0|1|0?\.\d+|1?\.\d+)\s*\)$", RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        // Find the nearest color from a dictionary of known colors
        private static string NearestColor(List<string> tailwindColorHexList, string targetColor)
        {
            Color target = ColorTranslator.FromHtml(targetColor);
            return tailwindColorHexList
                .Select(c => new {Hex = c, Color = ColorTranslator.FromHtml(c), Distance = ColorDistance(target, ColorTranslator.FromHtml(c)) })
                .Where(o=>o.Distance <= ColorMatchMaxDistance).OrderBy(c => c.Distance)
                .First()
                .Hex;
        }

        // Calculate the distance between two colors
        private static double ColorDistance(Color c1, Color c2)
        {
            return Math.Sqrt(Math.Pow(c1.R - c2.R, 2) + Math.Pow(c1.G - c2.G, 2) + Math.Pow(c1.B - c2.B, 2));
        }

        private static Dictionary<string, string> ColorMap = new Dictionary<string, string>
        {
            { "red", "#FF0000" },
            { "blue", "#0000FF" },
            { "green", "#00FF00" },
            // Add more colors as needed
        };



        // Method to convert border radius using UnitConverter
        private static string ConvertBorderRadius(string borderRadius, TailwindConvertSettingDto settings)
        {
            return ConvertUnit(BorderRadiusArray, borderRadius, settings.RemConversion);
        }
    }

    public class TailwindConvertSettingDto
    {
        public bool AutoConvertSpacing { get; set; } = true;    // Whether to auto-convert spacing

        public int RemConversion { get; set; } = 16;            // Rem conversion factor (e.g., 16px = 1rem)

        public bool AutoConvertColor { get; set; } = true;  // Default to true for auto color conversion

        public bool UseArbitraryValueIfCannotFindDefaultClass { get; set; } = true; //bg-[#123456] pt-[1234px]
    }

    
}