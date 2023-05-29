using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace nickeltin.Core.Editor
{
    /// <summary>
    /// PM - Package manager
    /// Wrapper around regular packagme manager request featuring <see cref="Completed"/> event;
    /// </summary>
    /// <typeparam name="RequestType"></typeparam>
    internal sealed class PMRequest<RequestType> where RequestType : Request
    {
        public delegate void RequestCompletedHandler(RequestType request, StatusCode status);

        private Progress _progress;
    
        public readonly RequestType Request;

        public StatusCode Status => Request.Status;

        public bool IsCompleted => Request.IsCompleted;

        public event RequestCompletedHandler Completed;
        
        public PMRequest(RequestType request)
        {
            Request = request;
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        public void AddProgress(Progress progress)
        {
            _progress = progress;
        }

        private void Update()
        {
            if (!IsCompleted) return;
            
            EditorApplication.update -= Update;

            _progress?.Finish(Progress.ConvertStatusCode(Request.Status));
            Completed?.Invoke(Request, Request.Status);
        }

        ~PMRequest()
        {
            EditorApplication.update -= Update;
        }

        public static implicit operator PMRequest<RequestType>(RequestType request)
        {
            return new PMRequest<RequestType>(request);
        }
    }
}