using System;
using System.Collections.Generic;
using System.Threading;
using nickeltin.Core.Runtime;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace nickeltin.Core.Editor.EmbeddedPackages
{
    internal static class EmbeddedPackagesValidator
    {
        public readonly struct ValidationData
        {
            public readonly EmbeddedPackage package;
            public readonly bool hasSomethigToImport;

            public ValidationData(EmbeddedPackage package, bool hasSomethigToImport)
            {
                this.package = package;
                this.hasSomethigToImport = hasSomethigToImport;
            }
        }

        /// <summary>
        /// Provides "safe" async validation handle. Prevents multiple validation processes, provides simple cancelling process. 
        /// </summary>
        public class AsyncValidator
        {
            public SingleDataCallback onPackageValidation;
            public MultipleDataCallback onAllPackagesValidation;
            public Action<bool> onValidationStateChanged;

            private readonly int _delay;
            private readonly CancellationTokenSource _cancellationTokenSource;
            private bool _isValidating;

            public AsyncValidator(int delayBetweenValidations = 1000)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _delay = delayBetweenValidations;
            }

            
            public bool IsValidating
            {
                get => _isValidating;
                private set
                {
                    if (_isValidating != value)
                    {
                        _isValidating = value;
                        onValidationStateChanged?.Invoke(value);
                    }   
                }
            }

            public void Cancel()
            {
                if (!IsValidating)
                {
                    Log("Not validaiting, and can't be cancelled!");
                    return;
                }
                
                IsValidating = false;
                _cancellationTokenSource.Cancel();
            }
            
            public bool Validate(EmbeddedPackage[] packages)
            {
                if (IsValidating)
                {
                    Log("Already validating!");
                    return false;
                }
                
                var validationBegun = ValidatePackagesAsync(
                    packages, data =>
                    {
                        onPackageValidation?.Invoke(data);
                    }, datas =>
                    {
                        IsValidating = false;
                        onAllPackagesValidation?.Invoke(datas);
                    }, _cancellationTokenSource, _delay);

                IsValidating = validationBegun;
                return validationBegun;
            }
        }
        
        public delegate void SingleDataCallback(ValidationData validationData);
        public delegate void MultipleDataCallback(ValidationData[] validationDatas);
        

        private static SynchronizationContext _unitySyncContext;

        [InitializeOnLoadMethod]
        private static void CacheContext()
        {
            _unitySyncContext = SynchronizationContext.Current;
        }
        
        public static ValidationData[] ValidatePackages(EmbeddedPackage[] packages)
        {
            var data = new ValidationData[packages.Length];
            for (var i = 0; i < packages.Length; i++)
            {
                var package = packages[i];
                var validatedState = EmbeddedPackagesUtil.ValidateEmbeddedPackageUpdates(package, false);
                var dataEntry = new ValidationData(package, validatedState);
                data[i] = dataEntry;
            }
            return data;
        }
        public static void Log(object msg)
        {
            Debug.LogFormat(LogType.Log, LogOption.None, null, "{0} {1} {2}", DateTime.Now.ToString("ss.fff"),
                "PACKAGES VALIDATOR:".Bold(), msg);
        }
        
        /// <summary>
        /// Starts pseudo-async packages validation.
        /// Real async validation can't be done because unity not thread-save, and most of code base should be executed on main thread.
        /// Faking async by making delays between dsipatching jobs. Jobs dispatched on temp thread, but then synchronized with unity main thread. 
        /// </summary>
        /// <param name="packages">Packages set</param>
        /// <param name="singlePackageValidationCallback">Invoked whenever packages validation job done</param>
        /// <param name="completeCallback">Invoked when all packages validation jobs done. Not garanteed, because jobs can be cancelled.</param>
        /// <param name="cancellationTokenSource">Checks for token cancelled status before starting any new validation job.</param>
        /// <param name="dealy">Delay between validation jobs, for making pseudo async effect.</param>
        /// <returns></returns>
        private static bool ValidatePackagesAsync(EmbeddedPackage[] packages, 
            SingleDataCallback singlePackageValidationCallback, 
            MultipleDataCallback completeCallback,
            CancellationTokenSource cancellationTokenSource,
            int dealy = 1000)
        {
            if (EditorApplication.isCompiling)
            {
                Log("Can't validate packages while compiling");
                return false;
            }
            
            var data = new List<ValidationData>();
            var progressId = Progress.Start("Validating packages");
            
            void RemoveProgress()
            {
                Progress.Remove(progressId);
            }

            void RunOnAnotherThread(CancellationTokenSource cancellation)
            {
                for (var i = 0; i < packages.Length; i++)
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        RemoveProgress();
                        return;
                    }
                    
                    var package = packages[i];
                    var progress = (float)i / packages.Length;
                    
                    _unitySyncContext.Post(_ =>
                    {
                        var validatedState = EmbeddedPackagesUtil.ValidateEmbeddedPackageUpdates(package, false);
                        var dataEntry = new ValidationData(package, validatedState);
                        data.Add(dataEntry);
                        singlePackageValidationCallback?.Invoke(dataEntry);
                        Progress.Report(progressId, progress);
                    }, null);
                    
                    Thread.Sleep(dealy);
                }
                _unitySyncContext.Post(_ =>
                {
                    RemoveProgress();
                    completeCallback?.Invoke(data.ToArray());
                }, null);
            }
            
            var queuedState = ThreadPool.QueueUserWorkItem(RunOnAnotherThread, cancellationTokenSource, true);

            if (!queuedState)
            {
                RemoveProgress();
            }
            
            return queuedState;
        }
    }
}