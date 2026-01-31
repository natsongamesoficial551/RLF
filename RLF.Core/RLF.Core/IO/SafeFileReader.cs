using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace RLF.Core.IO
{
    /// <summary>
    /// Leitor de arquivos seguro com fallback para backup.
    /// </summary>
    public static class SafeFileReader
    {
        private const int DefaultBufferSize = 4096;
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 50;

        /// <summary>
        /// Lê todo o texto de um arquivo de forma segura.
        /// </summary>
        public static FileOperationResult ReadAllText(string filePath, out string content, Encoding encoding = null)
        {
            content = null;

            if (string.IsNullOrEmpty(filePath))
                return FileOperationResult.Fail(filePath, FileOperationStatus.InvalidPath, "Caminho vazio");

            encoding = encoding ?? Encoding.UTF8;
            var sw = Stopwatch.StartNew();

            // Tenta arquivo principal
            var result = TryReadText(filePath, encoding, out content);

            if (result.IsSuccess)
            {
                sw.Stop();
                return FileOperationResult.Success(filePath, result.BytesProcessed, sw.Elapsed.TotalMilliseconds);
            }

            // Tenta backup
            string backupPath = filePath + ".bak";
            if (File.Exists(backupPath))
            {
                result = TryReadText(backupPath, encoding, out content);
                if (result.IsSuccess)
                {
                    sw.Stop();
                    return FileOperationResult.Success(filePath, result.BytesProcessed, sw.Elapsed.TotalMilliseconds);
                }
            }

            return result;
        }

        /// <summary>
        /// Lê todas as linhas de um arquivo.
        /// </summary>
        public static FileOperationResult ReadAllLines(string filePath, out string[] lines, Encoding encoding = null)
        {
            lines = null;

            var result = ReadAllText(filePath, out string content, encoding);

            if (result.IsSuccess && content != null)
            {
                lines = content.Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.None);
            }

            return result;
        }

        /// <summary>
        /// Lê todos os bytes de um arquivo.
        /// </summary>
        public static FileOperationResult ReadAllBytes(string filePath, out byte[] data)
        {
            data = null;

            if (string.IsNullOrEmpty(filePath))
                return FileOperationResult.Fail(filePath, FileOperationStatus.InvalidPath, "Caminho vazio");

            var sw = Stopwatch.StartNew();

            // Tenta arquivo principal
            var result = TryReadBytes(filePath, out data);

            if (result.IsSuccess)
            {
                sw.Stop();
                return FileOperationResult.Success(filePath, result.BytesProcessed, sw.Elapsed.TotalMilliseconds);
            }

            // Tenta backup
            string backupPath = filePath + ".bak";
            if (File.Exists(backupPath))
            {
                result = TryReadBytes(backupPath, out data);
                if (result.IsSuccess)
                {
                    sw.Stop();
                    return FileOperationResult.Success(filePath, result.BytesProcessed, sw.Elapsed.TotalMilliseconds);
                }
            }

            return result;
        }

        /// <summary>
        /// Verifica se um arquivo existe (incluindo backup).
        /// </summary>
        public static bool Exists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            return File.Exists(filePath) || File.Exists(filePath + ".bak");
        }

        /// <summary>
        /// Obtém tamanho do arquivo em bytes.
        /// </summary>
        public static long GetFileSize(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var info = new FileInfo(filePath);
                    return info.Length;
                }
            }
            catch { }

            return -1;
        }

        private static FileOperationResult TryReadText(string path, Encoding encoding, out string content)
        {
            content = null;

            if (!File.Exists(path))
                return FileOperationResult.Fail(path, FileOperationStatus.FileNotFound, "Arquivo não encontrado");

            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize))
                    using (var reader = new StreamReader(stream, encoding))
                    {
                        content = reader.ReadToEnd();
                        return FileOperationResult.Success(path, encoding.GetByteCount(content));
                    }
                }
                catch (IOException) when (retry < MaxRetries - 1)
                {
                    System.Threading.Thread.Sleep(RetryDelayMs);
                }
                catch (Exception ex)
                {
                    return FileOperationResult.FromException(path, ex);
                }
            }

            return FileOperationResult.Fail(path, FileOperationStatus.FileLocked, "Arquivo bloqueado após retentativas");
        }

        private static FileOperationResult TryReadBytes(string path, out byte[] data)
        {
            data = null;

            if (!File.Exists(path))
                return FileOperationResult.Fail(path, FileOperationStatus.FileNotFound, "Arquivo não encontrado");

            for (int retry = 0; retry < MaxRetries; retry++)
            {
                try
                {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize))
                    {
                        data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);
                        return FileOperationResult.Success(path, data.Length);
                    }
                }
                catch (IOException) when (retry < MaxRetries - 1)
                {
                    System.Threading.Thread.Sleep(RetryDelayMs);
                }
                catch (Exception ex)
                {
                    return FileOperationResult.FromException(path, ex);
                }
            }

            return FileOperationResult.Fail(path, FileOperationStatus.FileLocked, "Arquivo bloqueado após retentativas");
        }
    }
}