﻿// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using SE.Parsing;
using SE.SharpLang;

namespace SE.Hecate.Sharp
{
    /// <summary>
    /// Namespace (Identifier | (Identifier Dot))+ CurlyBracketOpen;
    /// </summary>
    public class NamespaceRule : ParserRule<SharpToken>
    {
        Stack<ValueTuple<int, string>> scopes;
        StringBuilder buffer;

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

        CacheEntry cache;
        /// <summary>
        /// A CSharp cache entry instance assigned to this rule
        /// </summary>
        public CacheEntry Cache
        {
            [MethodImpl(OptimizationExtensions.ForceInline)]
            get { return cache; }
            [MethodImpl(OptimizationExtensions.ForceInline)]
            set { cache = value; }
        }

        /// <summary>
        /// Creates this rule
        /// </summary>
        public NamespaceRule()
        {
            this.buffer = new StringBuilder();
        }
        [MethodImpl(OptimizationExtensions.ForceInline)]
        public override void Dispose()
        {
            linter = null;
            settings = null;

            StackPool<ValueTuple<int, string>>.Return(scopes);
            scopes = null;
        }

        protected override ProductionState Process(SharpToken value)
        {
            switch (State)
            {
                #region Namespace
                default:
                case 0: switch (value)
                    {
                        case SharpToken.CurlyBracketClose:
                            {
                                while (scopes.Count > 0 && scopes.Peek().Item1 >= linter.Scope)
                                    scopes.Pop();
                            }
                            goto default;
                        case SharpToken.Namespace:
                            {
                                OnReset();
                                State = 1;
                            }
                            return ProductionState.Preserve;
                        default:
                            return ProductionState.Revert;
                    }
                #endregion

                #region (Identifier | (Identifier
                case 1: switch (value)
                    {
                        case SharpToken.Identifier:
                            {
                                buffer.Append(linter.Buffer);
                            }
                            return ProductionState.Shift;
                    }
                    goto case 0;
                #endregion

                #region  Dot))+ CurlyBracketOpen
                case 2: switch (value)
                    {
                        case SharpToken.Dot:
                            {
                                buffer.Append('.');
                                State = 1;
                            }
                            return ProductionState.Preserve;
                        case SharpToken.CurlyBracketOpen:
                            return ProductionState.Success;
                    }
                    goto case 0;
                    #endregion
            }
        }

        [MethodImpl(OptimizationExtensions.ForceInline)]
        public override void OnReset()
        {
            if (scopes == null)
            {
                scopes = StackPool<ValueTuple<int, string>>.Get();
            }
            buffer.Clear();
        }
        public override void OnCompleted()
        {
            ValueTuple<int, string> current = ValueTuple.Create(linter.Scope - 1, buffer.ToString());
            foreach (ValueTuple<int, string> scope in scopes)
            {
                buffer.Insert(0, '.');
                buffer.Insert(0, scope.Item2);
            }
            lock (settings.Namespaces)
            {
                settings.Namespaces.Add(buffer.ToString());
            }
            cache.Namespaces.Add(buffer.ToString());
            scopes.Push(current);
        }
    }
}
