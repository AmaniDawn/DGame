using System;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace DGame
{
    internal sealed class SortHelper
    {
        public delegate int SortCompare(string lString, string rString);

        public static readonly HashSet<string> SortedGuids = new HashSet<string>();
        public static readonly Dictionary<string, SortType> SortedAssetDict = new Dictionary<string, SortType>();
        public static SortType CurSortType = SortType.None;
        public static SortType PathSortType = SortType.None;
        public static SortType NameSortType = SortType.None;

        public static readonly Dictionary<SortType, SortCompare> CompareFunctionDict =
            new Dictionary<SortType, SortCompare>()
            {
                { SortType.AscByPath, CompareWithPath },
                { SortType.DescByPath, CompareWithPathDesc },
                { SortType.AscByName, CompareWithName },
                { SortType.DescByName, CompareWithNameDesc }
            };

        public static void Init()
        {
            SortedGuids.Clear();
            SortedAssetDict.Clear();
        }

        public static void ChangeSortType(short sortGroup, Dictionary<SortType, SortType> handler, ref SortType recoverType)
        {
            if (SortConfig.SortTypeGroup[CurSortType] == sortGroup)
            {
                CurSortType = handler[CurSortType];
            }
            else
            {
                CurSortType = recoverType;
                if (CurSortType == SortType.None)
                {
                    CurSortType = handler[CurSortType];
                }
            }
            recoverType = CurSortType;
        }

        public static void SortChild(ReferenceFinderData.AssetDescription data)
        {
            if (data == null)
            {
                return;
            }

            if (SortedAssetDict.TryGetValue(data.path, out var oldSortType))
            {
                if (CurSortType == oldSortType)
                {
                    return;
                }

                if (SortConfig.SortTypeGroup[oldSortType] == SortConfig.SortTypeGroup[CurSortType])
                {
                    FastSort(data.dependencies);
                    FastSort(data.references);
                }
                else
                {
                    NormalSort(data.dependencies);
                    NormalSort(data.references);
                }
                SortedAssetDict[data.path] = CurSortType;
            }
            else
            {
                NormalSort(data.dependencies);
                NormalSort(data.references);
                SortedAssetDict.Add(data.path, CurSortType);
            }
        }

        private static void NormalSort(List<string> strList)
        {
            SortCompare curCompare = CompareFunctionDict[CurSortType];
            strList.Sort((l, r) => curCompare(l, r));
        }

        private static void FastSort(List<string> strList)
        {
            int i = 0;
            int j = strList.Count - 1;

            while (i < j)
            {
                (strList[i], strList[j]) = (strList[j], strList[i]);
                i++;
                j--;
            }
        }

        public static void SortByName() => ChangeSortType(SortConfig.TYPE_BY_NAME_GROUP,
            SortConfig.SortTypeChangeByNameHandler, ref NameSortType);

        public static void SortByPath() => ChangeSortType(SortConfig.TYPE_BY_PATH_GROUP,
            SortConfig.SortTypeChangeByPathHandler, ref PathSortType);

        private static int CompareWithNameDesc(string lstring, string rstring)
            => 0 - CompareWithName(lstring, rstring);

        private static int CompareWithName(string lstring, string rstring)
        {
            Dictionary<string, ReferenceFinderData.AssetDescription> asset = ResourceReferenceInfo.Data.assetDict;
            return string.Compare(asset[lstring].name, asset[rstring].name, StringComparison.Ordinal);
        }

        private static int CompareWithPathDesc(string lstring, string rstring)
            => 0 - CompareWithPath(lstring, rstring);

        private static int CompareWithPath(string lstring, string rstring)
        {
            Dictionary<string, ReferenceFinderData.AssetDescription> asset = ResourceReferenceInfo.Data.assetDict;
            return string.Compare(asset[lstring].path, asset[rstring].path, StringComparison.Ordinal);
        }
    }
}