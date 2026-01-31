using RLF.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RLF.Core.Configuration
{
    /// <summary>
    /// Leitor de arquivos .ini com suporte a seções, comentários e validação.
    /// Thread-safe e com cache interno para performance.
    /// </summary>
    public sealed class IniReader
    {
        // Estrutura interna: Seção -> (Chave -> Valor)
        private readonly Dictionary<string, Dictionary<string, string>> _data;

        // Lock para operações thread-safe
        private readonly object _lock = new object();

        // Configurações
        private readonly string _filePath;
        private bool _isLoaded;

        // Logger opcional
        private Logger _logger;

        /// <summary>
        /// Indica se o arquivo foi carregado com sucesso.
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                lock (_lock)
                {
                    return _isLoaded;
                }
            }
        }

        /// <summary>
        /// Caminho do arquivo .ini.
        /// </summary>
        public string FilePath
        {
            get { return _filePath; }
        }

        /// <summary>
        /// Construtor. Não carrega o arquivo automaticamente.
        /// </summary>
        /// <param name="filePath">Caminho completo do arquivo .ini</param>
        /// <param name="logger">Logger opcional para registrar operações</param>
        public IniReader(string filePath, Logger logger = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            _filePath = filePath;
            _logger = logger;
            _data = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            _isLoaded = false;
        }

        /// <summary>
        /// Define o logger após construção.
        /// </summary>
        /// <param name="logger">Logger a ser usado</param>
        public void SetLogger(Logger logger)
        {
            lock (_lock)
            {
                _logger = logger;
            }
        }

        /// <summary>
        /// Carrega o arquivo .ini do disco.
        /// </summary>
        /// <returns>True se carregado com sucesso</returns>
        public bool Load()
        {
            lock (_lock)
            {
                try
                {
                    _logger?.Debug($"IniReader: Loading file '{_filePath}'");

                    // Verifica se o arquivo existe
                    if (!File.Exists(_filePath))
                    {
                        _logger?.Warning($"IniReader: File not found '{_filePath}'");
                        return false;
                    }

                    // Limpa dados anteriores
                    _data.Clear();

                    // Lê todas as linhas
                    string[] lines = File.ReadAllLines(_filePath, Encoding.UTF8);

                    if (lines == null || lines.Length == 0)
                    {
                        _logger?.Warning($"IniReader: File is empty '{_filePath}'");
                        _isLoaded = true; // Tecnicamente foi carregado, apenas vazio
                        return true;
                    }

                    // Processa linhas
                    string currentSection = string.Empty;
                    int lineNumber = 0;

                    foreach (string rawLine in lines)
                    {
                        lineNumber++;

                        // Remove espaços em branco
                        string line = rawLine?.Trim();

                        // Ignora linhas vazias ou comentários
                        if (string.IsNullOrWhiteSpace(line) ||
                            line.StartsWith(";") ||
                            line.StartsWith("#") ||
                            line.StartsWith("//"))
                        {
                            continue;
                        }

                        // Detecta seção [NomeSeção]
                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            currentSection = line.Substring(1, line.Length - 2).Trim();

                            if (string.IsNullOrWhiteSpace(currentSection))
                            {
                                _logger?.Warning($"IniReader: Empty section name at line {lineNumber}");
                                currentSection = string.Empty;
                                continue;
                            }

                            // Cria seção se não existir
                            if (!_data.ContainsKey(currentSection))
                            {
                                _data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                _logger?.Debug($"IniReader: Created section '{currentSection}'");
                            }

                            continue;
                        }

                        // Detecta par chave=valor
                        int separatorIndex = line.IndexOf('=');

                        if (separatorIndex <= 0)
                        {
                            _logger?.Warning($"IniReader: Invalid format at line {lineNumber}: '{line}'");
                            continue;
                        }

                        string key = line.Substring(0, separatorIndex).Trim();
                        string value = line.Substring(separatorIndex + 1).Trim();

                        if (string.IsNullOrWhiteSpace(key))
                        {
                            _logger?.Warning($"IniReader: Empty key at line {lineNumber}");
                            continue;
                        }

                        // Garante que há uma seção (usa seção vazia se não houver)
                        if (!_data.ContainsKey(currentSection))
                        {
                            _data[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        }

                        // Adiciona ou atualiza valor
                        _data[currentSection][key] = value;
                        _logger?.Debug($"IniReader: Loaded [{currentSection}] {key} = {value}");
                    }

                    _isLoaded = true;
                    _logger?.Info($"IniReader: Successfully loaded '{_filePath}' with {GetSectionCount()} section(s)");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.Error($"IniReader: Failed to load '{_filePath}'", ex);
                    _isLoaded = false;
                    return false;
                }
            }
        }

        /// <summary>
        /// Salva os dados atuais de volta no arquivo .ini.
        /// </summary>
        /// <returns>True se salvo com sucesso</returns>
        public bool Save()
        {
            lock (_lock)
            {
                try
                {
                    _logger?.Debug($"IniReader: Saving to '{_filePath}'");

                    StringBuilder content = new StringBuilder();

                    // Header
                    content.AppendLine("; Configuration file for Real Life Framework");
                    content.AppendLine($"; Last saved: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    content.AppendLine();

                    // Escreve cada seção
                    foreach (var section in _data)
                    {
                        // Nome da seção
                        if (!string.IsNullOrWhiteSpace(section.Key))
                        {
                            content.AppendLine($"[{section.Key}]");
                        }

                        // Pares chave-valor
                        foreach (var pair in section.Value)
                        {
                            content.AppendLine($"{pair.Key}={pair.Value}");
                        }

                        // Linha em branco entre seções
                        content.AppendLine();
                    }

                    // Cria diretório se não existir
                    string directory = Path.GetDirectoryName(_filePath);
                    if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Salva no arquivo
                    File.WriteAllText(_filePath, content.ToString(), Encoding.UTF8);

                    _logger?.Info($"IniReader: Successfully saved '{_filePath}'");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.Error($"IniReader: Failed to save '{_filePath}'", ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// Lê um valor string de uma seção.
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <param name="key">Nome da chave</param>
        /// <param name="defaultValue">Valor padrão se não encontrar</param>
        /// <returns>Valor encontrado ou defaultValue</returns>
        public string GetString(string section, string key, string defaultValue = "")
        {
            lock (_lock)
            {
                if (!_isLoaded)
                {
                    _logger?.Warning("IniReader: Attempted to read before loading file");
                    return defaultValue;
                }

                section = section ?? string.Empty;

                if (string.IsNullOrWhiteSpace(key))
                    return defaultValue;

                if (!_data.ContainsKey(section))
                    return defaultValue;

                if (!_data[section].ContainsKey(key))
                    return defaultValue;

                return _data[section][key] ?? defaultValue;
            }
        }

        /// <summary>
        /// Lê um valor inteiro de uma seção.
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <param name="key">Nome da chave</param>
        /// <param name="defaultValue">Valor padrão se não encontrar ou conversão falhar</param>
        /// <returns>Valor encontrado ou defaultValue</returns>
        public int GetInt(string section, string key, int defaultValue = 0)
        {
            string value = GetString(section, key, null);

            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            int result;
            if (int.TryParse(value, out result))
                return result;

            _logger?.Warning($"IniReader: Failed to parse int [{section}] {key} = '{value}'");
            return defaultValue;
        }

        /// <summary>
        /// Lê um valor float de uma seção.
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <param name="key">Nome da chave</param>
        /// <param name="defaultValue">Valor padrão se não encontrar ou conversão falhar</param>
        /// <returns>Valor encontrado ou defaultValue</returns>
        public float GetFloat(string section, string key, float defaultValue = 0f)
        {
            string value = GetString(section, key, null);

            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            float result;
            if (float.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out result))
                return result;

            _logger?.Warning($"IniReader: Failed to parse float [{section}] {key} = '{value}'");
            return defaultValue;
        }

        /// <summary>
        /// Lê um valor booleano de uma seção.
        /// Aceita: true/false, yes/no, 1/0
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <param name="key">Nome da chave</param>
        /// <param name="defaultValue">Valor padrão se não encontrar ou conversão falhar</param>
        /// <returns>Valor encontrado ou defaultValue</returns>
        public bool GetBool(string section, string key, bool defaultValue = false)
        {
            string value = GetString(section, key, null);

            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            value = value.ToLowerInvariant();

            // Aceita múltiplos formatos
            if (value == "true" || value == "yes" || value == "1" || value == "on")
                return true;

            if (value == "false" || value == "no" || value == "0" || value == "off")
                return false;

            _logger?.Warning($"IniReader: Failed to parse bool [{section}] {key} = '{value}'");
            return defaultValue;
        }

        /// <summary>
        /// Define um valor string em uma seção.
        /// Cria a seção se não existir.
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <param name="key">Nome da chave</param>
        /// <param name="value">Valor a definir</param>
        /// <returns>True se definido com sucesso</returns>
        public bool SetString(string section, string key, string value)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(key))
                    return false;

                section = section ?? string.Empty;
                value = value ?? string.Empty;

                try
                {
                    // Cria seção se não existir
                    if (!_data.ContainsKey(section))
                    {
                        _data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }

                    _data[section][key] = value;
                    _logger?.Debug($"IniReader: Set [{section}] {key} = {value}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.Error($"IniReader: Failed to set [{section}] {key}", ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// Define um valor inteiro em uma seção.
        /// </summary>
        public bool SetInt(string section, string key, int value)
        {
            return SetString(section, key, value.ToString());
        }

        /// <summary>
        /// Define um valor float em uma seção.
        /// </summary>
        public bool SetFloat(string section, string key, float value)
        {
            return SetString(section, key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Define um valor booleano em uma seção.
        /// </summary>
        public bool SetBool(string section, string key, bool value)
        {
            return SetString(section, key, value ? "true" : "false");
        }

        /// <summary>
        /// Verifica se uma seção existe.
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <returns>True se a seção existe</returns>
        public bool HasSection(string section)
        {
            lock (_lock)
            {
                if (!_isLoaded)
                    return false;

                section = section ?? string.Empty;
                return _data.ContainsKey(section);
            }
        }

        /// <summary>
        /// Verifica se uma chave existe em uma seção.
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <param name="key">Nome da chave</param>
        /// <returns>True se a chave existe</returns>
        public bool HasKey(string section, string key)
        {
            lock (_lock)
            {
                if (!_isLoaded)
                    return false;

                section = section ?? string.Empty;

                if (string.IsNullOrWhiteSpace(key))
                    return false;

                if (!_data.ContainsKey(section))
                    return false;

                return _data[section].ContainsKey(key);
            }
        }

        /// <summary>
        /// Remove uma seção completa.
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <returns>True se removida com sucesso</returns>
        public bool RemoveSection(string section)
        {
            lock (_lock)
            {
                if (!_isLoaded)
                    return false;

                section = section ?? string.Empty;

                bool removed = _data.Remove(section);

                if (removed)
                {
                    _logger?.Debug($"IniReader: Removed section '{section}'");
                }

                return removed;
            }
        }

        /// <summary>
        /// Remove uma chave de uma seção.
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <param name="key">Nome da chave</param>
        /// <returns>True se removida com sucesso</returns>
        public bool RemoveKey(string section, string key)
        {
            lock (_lock)
            {
                if (!_isLoaded)
                    return false;

                section = section ?? string.Empty;

                if (string.IsNullOrWhiteSpace(key))
                    return false;

                if (!_data.ContainsKey(section))
                    return false;

                bool removed = _data[section].Remove(key);

                if (removed)
                {
                    _logger?.Debug($"IniReader: Removed [{section}] {key}");
                }

                return removed;
            }
        }

        /// <summary>
        /// Obtém todas as chaves de uma seção.
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <returns>Array de chaves ou array vazio</returns>
        public string[] GetKeys(string section)
        {
            lock (_lock)
            {
                if (!_isLoaded)
                    return new string[0];

                section = section ?? string.Empty;

                if (!_data.ContainsKey(section))
                    return new string[0];

                var keys = new string[_data[section].Count];
                _data[section].Keys.CopyTo(keys, 0);
                return keys;
            }
        }

        /// <summary>
        /// Obtém todas as seções.
        /// </summary>
        /// <returns>Array de nomes de seções</returns>
        public string[] GetSections()
        {
            lock (_lock)
            {
                if (!_isLoaded)
                    return new string[0];

                var sections = new string[_data.Count];
                _data.Keys.CopyTo(sections, 0);
                return sections;
            }
        }

        /// <summary>
        /// Retorna o número total de seções.
        /// </summary>
        /// <returns>Quantidade de seções</returns>
        public int GetSectionCount()
        {
            lock (_lock)
            {
                return _isLoaded ? _data.Count : 0;
            }
        }

        /// <summary>
        /// Retorna o número de chaves em uma seção.
        /// </summary>
        /// <param name="section">Nome da seção</param>
        /// <returns>Quantidade de chaves ou -1 se seção não existir</returns>
        public int GetKeyCount(string section)
        {
            lock (_lock)
            {
                if (!_isLoaded)
                    return -1;

                section = section ?? string.Empty;

                if (!_data.ContainsKey(section))
                    return -1;

                return _data[section].Count;
            }
        }

        /// <summary>
        /// Limpa todos os dados carregados.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _data.Clear();
                _isLoaded = false;
                _logger?.Debug("IniReader: Cleared all data");
            }
        }

        /// <summary>
        /// Recarrega o arquivo do disco.
        /// </summary>
        /// <returns>True se recarregado com sucesso</returns>
        public bool Reload()
        {
            lock (_lock)
            {
                _logger?.Debug("IniReader: Reloading file");
                Clear();
                return Load();
            }
        }
    }
}