using EFT.HandBook;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventoryOrganizingFeatures.Reflections
{
    internal class HandbookNode : ReflectionBase
    {
        public HandbookData Data { get; }
        public Dictionary<string, HandbookNode> ChildrenDict { get; }
        public Dictionary<string, HandbookNode>.ValueCollection Children { get; }
        public string[] Category { get; }
        public string CategoryString { get; }

        public HandbookNode(object instance)
        {
            Instance = instance;
            ReflectedType = instance.GetType();

            Data = GetFieldValue<HandbookData>("Data");
            ChildrenDict = ReflectChildrenDict();
            Children = ChildrenDict.Values;
            Category = GetFieldValue<string[]>("Category");
            CategoryString = string.Join(" > ", Category.Select(cat => cat.Localized()).ToArray());
        }

        public bool CategoryContains(string findStr , bool caseSensitive = false)
        {
            return Category.Any(cat =>
            {
                if (caseSensitive)
                {
                    return cat.Localized().Contains(findStr);
                }
                else
                {
                    return cat.Localized().ToLower().Contains(findStr.ToLower());
                }
            });
        }

        private Dictionary<string, HandbookNode> ReflectChildrenDict()
        {
            var result = new Dictionary<string, HandbookNode>();
            foreach (DictionaryEntry child in GetFieldValue<IDictionary>("ChildrenDict"))
            {
                result.Add((string)child.Key, new HandbookNode(child.Value));
            }
            return result;
        }
    }
}
