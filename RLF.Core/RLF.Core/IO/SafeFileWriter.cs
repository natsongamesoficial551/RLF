using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace RLF.Core.IO
{
    /// <summary>
    /// Escritor de arquivos seguro com backup atômico.
    /// Previne corrupção de dados em caso de crash.
    /// </summary>
    public static class SafeFileWriter
    {
        private const int DefaultBufferSize = 4096;
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 50;

        /// <summary>
        /// Escreve texto em arquivo de forma segura.
        /// Usa arquivo temporário + rename atômico.
        /// </summary>
        public static FileOperationResult WriteAllText(string filePath, string content, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(filePath))
                return FileOperationResult.Fail(filePath, FileOperationStatus.InvalidPath, "Caminho vazio");

            encoding = encoding ?? Encoding.UTF8;
            var sw = Stopwatch.StartNew();

            string directory = Path.GetDirectoryName(filePath);
            string tempPath = null;
            string backupPath = null;

            try
            {
                // Garante que diretório existe
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Cria arquivo temporário
                tempPath = filePath + ".tmp";
                backupPath = filePath + ".bak";

                // Escreve no arquivo temporário
                for (int retry = 0; retry < MaxRetries; retry++)
                {
                    try
                    {
                        using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, DefaultBufferSize))
                        using (var writer = new StreamWriter(stream, encoding))
                        {
                            writer.Write(content);
                            writer.Flush();
                            stream.Flush(true); // Força sync com disco
                        }
                        break;
                    }
                    catch (IOException) when (retry < MaxRetries - 1)
                    {
                        System.Threading.Thread.Sleep(RetryDelayMs);
                    }
                }

                // Se arquivo original existe, faz backup
                if (File.Exists(filePath))
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);

                    File.Move(filePath, backupPath);
                }

                // Move temp para destino final (atômico)
                File.Move(tempPath, filePath);

                // Remove backup se tudo OK
                if (File.Exists(backupPath))
                {
                    try { File.Delete(backupPath); } catch { }
                }

                sw.Stop();
                long bytes = encoding.GetByteCount(content);
                return FileOperationResult.Success(filePath, bytes, sw.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                // Tenta restaurar backup se existir
                TryRestoreBackup(filePath, backupPath);

                // Limpa temp
                TryDeleteFile(tempPath);

                return FileOperationResult.FromException(filePath, ex);
            }
        }

        /// <summary>
        /// Escreve linhas em arquivo de forma segura.
        /// </summary>
        public static FileOperationResult WriteAllLines(string filePath, string[] lines, Encoding encoding = null)
        {
            if (lines == null)
                return FileOperationResult.Fail(filePath, FileOperationStatus.Unknown, "Linhas nulas");

            string content = string.Join(Environment.NewLine, lines);
            return WriteAllText(filePath, content, encoding);
        }

        /// <summary>
        /// Escreve bytes em arquivo de forma segura.
        /// </summary>
        public static FileOperationResult WriteAllBytes(string filePath, byte[] data)
        {
            if (string.IsNullOrEmpty(filePath))
                return FileOperationResult.Fail(filePath, FileOperationStatus.InvalidPath, "Caminho vazio");

            if (data == null)
                return FileOperationResult.Fail(filePath, FileOperationStatus.Unknown, "Dados nulos");

            var sw = Stopwatch.StartNew();

            string directory = Path.GetDirectoryName(filePath);
            string tempPath = null;
            string backupPath = null;

            try
            {
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                tempPath = filePath + ".tmp";
                backupPath = filePath + ".bak";

                // Escreve no arquivo temporário
                for (int retry = 0; retry < MaxRetries; retry++)
                {
                    try
                    {
                        using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, DefaultBufferSize))
                        {
                            stream.Write(data, 0, data.Length);
                            stream.Flush(true);
                        }
                        break;
                    }
                    catch (IOException) when (retry < MaxRetries - 1)
                    {
                        System.Threading.Thread.Sleep(RetryDelayMs);
                    }
                }

                // Backup e swap atômico
                if (File.Exists(filePath))
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);

                    File.Move(filePath, backupPath);
                }

                File.Move(tempPath, filePath);

                if (File.Exists(backupPath))
                {
                    try { File.Delete(backupPath); } catch { }
                }

                sw.Stop();
                return FileOperationResult.Success(filePath, data.Length, sw.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                TryRestoreBackup(filePath, backupPath);
                TryDeleteFile(tempPath);
                return FileOperationResult.FromException(filePath, ex);
            }
        }

        /// <summary>
        /// Append seguro (sem atomicidade completa, mas com retry).
        /// </summary>
        public static FileOperationResult AppendText(string filePath, string content, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(filePath))
                return FileOperationResult.Fail(filePath, FileOperationStatus.InvalidPath, "Caminho vazio");

            encoding = encoding ?? Encoding.UTF8;
            var sw = Stopwatch.StartNew();

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                for (int retry = 0; retry < MaxRetries; retry++)
                {
                    try
                    {
                        using (var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read, DefaultBufferSize))
                        using (var writer = new StreamWriter(stream, encoding))
                        {
                            writer.Write(content);
                        }
                        break;
                    }
                    catch (IOException) when (retry < MaxRetries - 1)
                    {
                        System.Threading.Thread.Sleep(RetryDelayMs);
                    }
                }

                sw.Stop();
                long bytes = encoding.GetByteCount(content);
                return FileOperationResult.Success(filePath, bytes, sw.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                return FileOperationResult.FromException(filePath, ex);
            }
        }

        private static void TryRestoreBackup(string filePath, string backupPath)
        {
            try
            {
                if (!string.IsNullOrEmpty(backupPath) && File.Exists(backupPath))
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    File.Move(backupPath, filePath);
                }
            }
            catch { }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }
    }
}