using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public static  class Extensions
    {

     
        public static dynamic GetDynRuntimeValue<T>(this T item, string field, bool ignoreCase = false) where T : class
            {
                dynamic finalvalue = null;

                var fields = field.Split('.');

                fields = field.Length > 1 ? fields : new string[] { field };

                var enumerable = fields as string[] ?? fields.ToArray();
                var props = TypeDescriptor.GetProperties(item);
                for (var i = 0; i <= enumerable.Count() - 1; i++)
                {

                    if (props.Find(enumerable[i], ignoreCase) == null) return null;

                    var value = props.Find(enumerable[i], ignoreCase)?.GetValue(item);

                    if (i >= enumerable.Length - 1)
                    {
                        finalvalue = value;
                        continue;
                    }

                    props = TypeDescriptor.GetProperties(value);
                    var finalProp = enumerable[i + 1];
                    if (value is null) return new object();
                    if (props.Find(finalProp, ignoreCase) == null) return null;
                    finalvalue = props.Find(finalProp, ignoreCase)?.GetValue(value);
                    break;
                }

                return finalvalue;
            }

        public static void WriteBuityfullConnectionString(this string conn) {


            var words = conn.Split(';');
            
            for (var i =0; i < 2;i++) { 
            
                var words1 = words[i].Split('=');


                var towrite = words1[0] switch
                {
                    "Data Source" => "DATABASE SERVER",
                    "Initial Catalog" => "DATABASE",
                    _ => words1[0]
                };

                Console.WriteLine($"  {towrite}: {words1[1]}");
            
            }
        
        
        
        }

    }
}
