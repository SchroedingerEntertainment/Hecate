﻿// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using SE.Hecate.Build;
using SE.Parsing;
using SE.SharpLang;

namespace SE.Hecate.Sharp
{
    /// <summary>
    /// Static (Private | Protected | Internal | Public)* 'Main' RoundBracketOpen;
    /// </summary>
    class MainRule : ParserRule<SharpToken>
    {
        Linter linter;
        /// <summary>
        /// The Linter instance this rule is assigned to
        /// </summary>
        public Linter Linter
        {
            [MethodImpl(OptimizationExtensions.ForceInline)]
            get { return linter; }
            [MethodImpl(OptimizationExtensions.ForceInline)]
            set { linter = value; }
        }
        
        SharpModuleSettings settings;
        /// <summary>
        /// A CSharp configuration instance currently in process
        /// </summary>
        public SharpModuleSettings Settings
        {
            [MethodImpl(OptimizationExtensions.ForceInline)]
            get { return settings; }
            [MethodImpl(OptimizationExtensions.ForceInline)]
            set { settings = value; }
        }

        /// <summary>
        /// Creates this rule
        /// </summary>
        public MainRule()
        { }
        public override void Dispose()
        {
            linter = null;
            settings = null;
        }

        protected override ProductionState Process(SharpToken value)
        {
            if (linter.Scope >= 2)
            {
                switch (State)
                {
                    #region Static
                    default:
                    case 0: switch (value)
                        {
                            case SharpToken.Static:
                                {
                                    OnReset();
                                    State = 1;
                                }
                                return ProductionState.Preserve;
                            default:
                                return ProductionState.Revert;
                        }
                    #endregion

                    #region (Private | Protected | Internal | Public)
                    case 1: switch (value)
                        {
                            case SharpToken.Void:
                            case SharpToken.Int:
                                return ProductionState.Shift;
                            case SharpToken.Private:
                            case SharpToken.Protected:
                            case SharpToken.Internal:
                            case SharpToken.Public:
                                return ProductionState.Preserve;
                        }
                        goto case 0;
                    #endregion

                    #region 'Main'
                    case 2: switch (value)
                        {
                            case SharpToken.Identifier:
                                {
                                    if (!linter.Buffer.Equals("Main", StringComparison.InvariantCulture))
                                        break;
                                }
                                return ProductionState.Shift;
                        }
                        goto case 0;
                    #endregion

                    #region CurlyBraceOpen
                    case 3: switch (value)
                        {
                            case SharpToken.RoundBracketOpen:
                                return ProductionState.Success;
                        }
                        goto case 0;
                        #endregion
                }
            }
            else return ProductionState.Revert;
        }

        [MethodImpl(OptimizationExtensions.ForceInline)]
        public override void OnReset()
        { }
        [MethodImpl(OptimizationExtensions.ForceInline)]
        public override void OnCompleted()
        {
            lock (settings)
            {
                if (settings.AssemblyType < BuildModuleType.Console)
                    settings.AssemblyType = BuildModuleType.Console;
            }
        }
    }
}
