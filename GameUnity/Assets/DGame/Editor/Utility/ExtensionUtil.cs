using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;

namespace DGame
{
    public static class ExtensionUtil
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

    }
}