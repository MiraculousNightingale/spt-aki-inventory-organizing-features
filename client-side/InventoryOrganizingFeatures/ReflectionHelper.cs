using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InventoryOrganizingFeatures
{
    /// <summary>
    /// Extension and helper class to simplify reflection.
    /// </summary>
    internal static class ReflectionHelper
    {
        // public static Type FindClassType()
        //private static Dictionary <string, Type> TypeCache = new Dictionary <string, Type> ();
        //private static Dictionary<Type, FieldInfo> FieldInfoCache = new Dictionary<Type, FieldInfo>();
        //private static Dictionary<Type, PropertyInfo> PropertyInfoCache = new Dictionary<Type, PropertyInfo>();
        //private static Dictionary<Type, MethodInfo> MethodInfoCache = new Dictionary<Type, MethodInfo>();

        public static Type FindClassTypeByMethodNames(string[] methodNames)
        {
            var validClasses = AccessTools.AllTypes().Where(type =>
            {
                if (type.IsClass)
                {
                    var methods = AccessTools.GetMethodNames(type);
                    return methodNames.All(searchedMethodName => methods.Contains(searchedMethodName));
                }
                return false;
            });
            if (validClasses.Count() > 1) throw new AmbiguousMatchException();
            return validClasses.FirstOrDefault();
        }

        public static Type FindClassTypeByFieldNames(string[] fieldNames)
        {
            var validClasses = AccessTools.AllTypes().Where(type =>
            {
                if (type.IsClass)
                {
                    var fields = AccessTools.GetFieldNames(type);
                    return fieldNames.All(searchedFieldName => fields.Contains(searchedFieldName));
                }
                return false;
            });
            if (validClasses.Count() > 1) throw new AmbiguousMatchException();
            return validClasses.FirstOrDefault();
        }

        // public static Type FindClassTypeByPropertyNames

        public static MethodInfo FindMethodByArgTypes(this object instance, Type[] methodArgTypes, BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            return FindMethodByArgTypes(instance.GetType(), methodArgTypes, bindingAttr);
        }

        public static MethodInfo FindMethodByArgTypes(Type type, Type[] methodArgTypes, BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            var validMethods = type.GetMethods(bindingAttr).Where(method =>
            {
                var parameters = method.GetParameters();
                return methodArgTypes.All(argType => parameters.Any(param => param.ParameterType == argType));
            });
            if (validMethods.Count() > 1) throw new AmbiguousMatchException();
            return validMethods.FirstOrDefault();
        }

        public static T InvokeMethod<T>(this Type staticType, string methodName, object[] args = null, Type[] methodArgTypes = null)
        {
            return (T)InvokeMethod(staticType, methodName, args, methodArgTypes); 
        }

        public static object InvokeMethod(this Type staticType, string methodName, object[] args = null, Type[] methodArgTypes = null)
        {
            var method = AccessTools.Method(staticType, methodName, methodArgTypes);
            if (method == null) throw new Exception("ReflectionHelper.InvokeMethod | Found method is null.");
            var parameters = method.GetParameters();
            // auto-compensate for default parameters if they aren't provided
            // or you'll get "Number of parameters specified does not match..."
            if (args.Length < parameters.Length)
            {
                Array.Resize(ref args, parameters.Length);
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] ??= Type.Missing;
                }
            }
            return method.Invoke(null, args);
        }

        public static T InvokeMethod<T>(this object targetObj, string methodName, object[] args = null, Type[] methodArgTypes = null)
        {
            return (T)InvokeMethod(targetObj, methodName, args, methodArgTypes);
        }

        public static object InvokeMethod(this object targetObj, string methodName, object[] args = null, Type[] methodArgTypes = null)
        {
            var method = AccessTools.Method(targetObj.GetType(), methodName, methodArgTypes);
            if (method == null) throw new Exception("ReflectionHelper.InvokeMethod | Found method is null.");
            var parameters = method.GetParameters();
            // auto-compensate for default parameters if they aren't provided
            // or you'll get "Number of parameters specified does not match..."
            if (args.Length < parameters.Length)
            {
                Array.Resize(ref args, parameters.Length);
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] ??= Type.Missing;
                }
            }
            return method.Invoke(targetObj, args);
        }

        public static T GetFieldValue<T>(this object targetObj, string fieldName)
        {
            return (T)GetFieldValue(targetObj, fieldName);
        }

        public static object GetFieldValue(this object targetObj, string fieldName)
        {
            var fieldInfo = AccessTools.Field(targetObj.GetType(), fieldName);
            if (fieldInfo == null) throw new Exception("ReflectionHelper.GetFieldValue | Found field is null");
            return fieldInfo.GetValue(targetObj);
        }

        public static T GetPropertyValue<T>(this object targetObj, string fieldName)
        {
            return (T)GetPropertyValue(targetObj, fieldName);
        }

        public static object GetPropertyValue(this object targetObj, string propertyName)
        {
            var propertyInfo = AccessTools.Property(targetObj.GetType(), propertyName);
            if (propertyInfo == null) throw new Exception("ReflectionHelper.GetPropertyValue | Found property is null");
            return propertyInfo.GetValue(targetObj);
        }
    }
}
