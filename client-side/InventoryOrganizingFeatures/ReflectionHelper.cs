using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InventoryOrganizingFeatures
{
    internal static class ReflectionHelper
    {
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

        public static MethodInfo FindMethodByArgTypes(object instance, Type[] methodArgTypes, BindingFlags bindingAttr = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
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
            if(validMethods.Count() > 1) throw new AmbiguousMatchException();
            return validMethods.FirstOrDefault();
        }

        public static T InvokeMethod<T>(object targetObj, string methodName, object[] args, Type[] methodArgTypes = null)
        {
            return (T)AccessTools.Method(targetObj.GetType(), methodName, methodArgTypes).Invoke(targetObj, args);
        }
    }
}
