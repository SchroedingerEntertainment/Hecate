﻿// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using SE.Hecate.Build;

namespace SE.Hecate.Sharp
{
    /// <summary>
    /// A comparer to sort CSharp code modules along their dependencies
    /// </summary>
    public class SharpModuleComparer : IComparer<BuildModule>
    {
        /// <summary>
        /// A default comparer instance
        /// </summary>
        public readonly static SharpModuleComparer Default = new SharpModuleComparer();

        /// <summary>
        /// Creates a new comparer instance
        /// </summary>
        public SharpModuleComparer()
        { }

        public int Compare(BuildModule x, BuildModule y)
        {
            SharpModule xm;
            SharpModule ym;

            x.TryGetProperty<SharpModule>(out xm);
            y.TryGetProperty<SharpModule>(out ym);

            if (x.Location == y.Location) return 0;
            else if (xm.Default.AssemblyType == ym.Default.AssemblyType && x.IsPackage == y.IsPackage)
            {
                if (xm.Default.Dependencies.Contains(y)) return 1;
                else if (ym.Default.Dependencies.Contains(x)) return -1;
                else return xm.Default.Dependencies.Count.CompareTo(ym.Default.Dependencies.Count);
            }
            else if (x.IsPackage) return 1;
            else if (y.IsPackage) return -1;
            else return xm.Default.AssemblyType.CompareTo(ym.Default.AssemblyType);
        }
    }
}
