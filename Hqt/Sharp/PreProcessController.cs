﻿// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using SE.Flex;
using SE.Hecate.Build;

namespace SE.Hecate.Sharp
{
    /// <summary>
    /// Pipeline node to perform CSharp code file lookups
    /// </summary>
    [ProcessorUnit(IsExtension = true)]
    public class PreprocessController : ProcessorUnit, IPrioritizedActor
    {
        /// <summary>
        /// Files that are considered to belong to CSharp code
        /// </summary>
        public readonly static string[] ValidFileExtensions = new string[]
        {
            "*.cs",
            "*.resx"
        };

        private static Filter fileFilter;
        private static Filter directoryFilter;

        int IPrioritizedActor.Priority
        {
            get { return 0; }
        }
        public override PathDescriptor Target
        {
            get { return Application.SdkRoot; }
        }
        public override bool Enabled
        {
            get { return true; }
        }
        public override UInt32 Family
        {
            get { return (UInt32)ProcessorFamilies.Preprocess; }
        }

        static PreprocessController()
        {
            fileFilter = new Filter();
            foreach (string extension in ValidFileExtensions)
            {
                fileFilter.Add(extension);
            }

            directoryFilter = new Filter();
            FilterToken root = directoryFilter.Add("...");
            FilterToken token = directoryFilter.Add(root, ".*");
            token.Type = FilterType.And;
            token.Exclude = true;
            token = directoryFilter.Add(root, "obj");
            token.Type = FilterType.And;
            token.Exclude = true;
            token = directoryFilter.Add(root, "bin");
            token.Type = FilterType.And;
            token.Exclude = true;
        }
        /// <summary>
        /// Creates a new node instance
        /// </summary>
        public PreprocessController()
        { }

        public void Attach(PriorityDispatcher owner)
        { }
        public void Detach(PriorityDispatcher owner)
        { }

        public bool OnNext(KernelMessage value)
        {
            try
            {
                return Process(value);
            }
            catch (Exception er)
            {
                Application.Error(er);
                return false;
            }
        }
        public bool OnError(Exception error)
        {
            return true;
        }
        public void OnCompleted()
        { }

        private static async Task<int> Process(BuildModule module, BuildProfile profile)
        {
            List<FileSystemDescriptor> files = CollectionPool<List<FileSystemDescriptor>, FileSystemDescriptor>.Get();
            foreach (PathDescriptor subFolder in module.Location.FindDirectories(directoryFilter))
            {
                if (subFolder.Name.Equals("resources", StringComparison.InvariantCultureIgnoreCase))
                {
                    files.AddRange(subFolder.GetFiles());
                }
                else subFolder.FindFiles(fileFilter, files);
            }
            module.Location.FindFiles(fileFilter, files, PathSeekOptions.Forward | PathSeekOptions.RootLevel);
            if (files.Where(x => fileFilter.IsMatch(x.FullName)).Any())
            {
                PreprocessCommand command = new PreprocessCommand(module, profile, files);
                try
                {
                    bool exit; if (Kernel.Dispatch(command, out exit))
                    {
                        return await command.Task.ContinueWith<int>((task) =>
                        {
                            CollectionPool<List<FileSystemDescriptor>, FileSystemDescriptor>.Return(files);
                            return task.Result;

                        });
                    }
                    else if (exit)
                    {
                        return Application.FailureReturnCode;
                    }
                }
                finally
                {
                    command.Release();
                }
            }
            return Application.SuccessReturnCode;
        }
        public override bool Process(KernelMessage command)
        {
            List<object> modules = CollectionPool<List<object>, object>.Get();
            try
            {
                if (PropertyManager.FindProperties(x => x.Value is BuildModule, modules) > 0)
                {
                    BuildProfile profile; command.TryGetProperty<BuildProfile>(out profile);
                    foreach (BuildModule module in modules)
                    {
                        command.Attach(Process(module, profile));
                    }
                    return true;
                }
            }
            finally
            {
                CollectionPool<List<object>, object>.Return(modules);
            }
            return false;
        }
    }
}
