using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpiceWeb.Mvc.Core.Extensions
{
    //menambhakan class ini digunakan untuk mrepresentasikan Extension yang digunakan untuk Category List (dropdown)
    public static class ReflectionExtension
    {
        //untuk ekstraksi property value dari model
        public static string GetPropertyValue<T>(this T item, string propertyName)
        {
            return item.GetType().GetProperty(propertyName).GetValue(item, null).ToString();
        }
    }
}
