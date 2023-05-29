using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine.Networking;
using UProgress = UnityEditor.Progress;


namespace nickeltin.Core.Editor
{
    /// <summary>
    /// Class binded with <see cref="UnityEditor.Progress"/>
    /// </summary>
    internal sealed class Progress
    {
        private readonly int _progressId;
        private readonly UProgress.Options _options;
        private readonly HashSet<Progress> _childs;

        public Progress(string name, string desc, UProgress.Options options) : this(name, desc, options, -1)
        {
        }
        
        private Progress(string name, string desc, UProgress.Options options, int parentId)
        {
            _options = options;
            _childs = new HashSet<Progress>();
            _progressId = UProgress.Start(name, desc, options, parentId);
        }

        public Progress AddChild(string name, string desc, UProgress.Options options)
        {
            var child = new Progress(name, desc, options, _progressId);
            _childs.Add(child);
            return child;
        }

        public static UProgress.Status ConvertStatusCode(StatusCode status)
        {
            switch (status)
            {
                case StatusCode.InProgress:
                    return UProgress.Status.Running;
                case StatusCode.Success:
                    return UProgress.Status.Succeeded;
                case StatusCode.Failure:
                    return UProgress.Status.Failed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
        
        public static UProgress.Status ConvertStatusCode(UnityWebRequest.Result status)
        {
            switch (status)
            {
                case UnityWebRequest.Result.InProgress:
                    return UProgress.Status.Running;
                case UnityWebRequest.Result.Success:
                    return UProgress.Status.Succeeded;
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.DataProcessingError:
                    return UProgress.Status.Failed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        public void Finish(UProgress.Status status = UProgress.Status.Succeeded)
        {
            UProgress.Finish(_progressId, status);
        }

        public void Update(float progress, string desc = "")
        {
            if (_options.HasFlag(UProgress.Options.Indefinite))
            {
                throw new Exception("Can't update Indefinite progress");
            }
            
            UProgress.Report(_progressId, progress, desc);
        }
    }
}