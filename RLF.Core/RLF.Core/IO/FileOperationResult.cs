using System;

namespace RLF.Core.IO
{
    /// <summary>
    /// Status de uma operação de arquivo.
    /// </summary>
    public enum FileOperationStatus
    {
        Success,
        FileNotFound,
        AccessDenied,
        FileLocked,
        DiskFull,
        InvalidPath,
        Timeout,
        Unknown
    }

    /// <summary>
    /// Resultado de uma operação de arquivo.
    /// </summary>
    public sealed class FileOperationResult
    {
        public bool IsSuccess { get; }
        public FileOperationStatus Status { get; }
        public string FilePath { get; }
        public string ErrorMessage { get; }
        public Exception Exception { get; }
        public long BytesProcessed { get; }
        public double ElapsedMs { get; }

        private FileOperationResult(
            bool success,
            FileOperationStatus status,
            string filePath,
            string errorMessage = null,
            Exception exception = null,
            long bytesProcessed = 0,
            double elapsedMs = 0)
        {
            IsSuccess = success;
            Status = status;
            FilePath = filePath;
            ErrorMessage = errorMessage;
            Exception = exception;
            BytesProcessed = bytesProcessed;
            ElapsedMs = elapsedMs;
        }

        public static FileOperationResult Success(string filePath, long bytes = 0, double elapsedMs = 0)
        {
            return new FileOperationResult(true, FileOperationStatus.Success, filePath,
                bytesProcessed: bytes, elapsedMs: elapsedMs);
        }

        public static FileOperationResult Fail(string filePath, FileOperationStatus status, string message, Exception ex = null)
        {
            return new FileOperationResult(false, status, filePath, message, ex);
        }

        public static FileOperationResult FromException(string filePath, Exception ex)
        {
            var status = FileOperationStatus.Unknown;
            var message = ex.Message;

            if (ex is System.IO.FileNotFoundException)
                status = FileOperationStatus.FileNotFound;
            else if (ex is UnauthorizedAccessException)
                status = FileOperationStatus.AccessDenied;
            else if (ex is System.IO.IOException)
            {
                if (ex.Message.Contains("being used"))
                    status = FileOperationStatus.FileLocked;
                else if (ex.Message.Contains("disk") || ex.Message.Contains("space"))
                    status = FileOperationStatus.DiskFull;
            }
            else if (ex is ArgumentException || ex is System.IO.PathTooLongException)
                status = FileOperationStatus.InvalidPath;
            else if (ex is TimeoutException)
                status = FileOperationStatus.Timeout;

            return new FileOperationResult(false, status, filePath, message, ex);
        }

        public override string ToString()
        {
            if (IsSuccess)
                return $"[OK] {FilePath} ({BytesProcessed} bytes, {ElapsedMs:F2}ms)";

            return $"[FAIL:{Status}] {FilePath} - {ErrorMessage}";
        }
    }
}