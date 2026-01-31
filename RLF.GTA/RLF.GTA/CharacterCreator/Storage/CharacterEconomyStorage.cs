using System;
using System.IO;
using System.Globalization;
using RLF.GTA.CharacterCreator.Integration;

namespace RLF.GTA.CharacterCreator.Storage
{
    /// <summary>
    /// Salva e carrega dados de economia em arquivos INI (um por personagem)
    /// </summary>
    public class CharacterEconomyStorage
    {
        private readonly string _basePath;

        public CharacterEconomyStorage(string basePath)
        {
            _basePath = basePath;

            // Cria pasta se não existir
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
                System.Diagnostics.Debug.WriteLine($"📁 Pasta criada: {_basePath}");
            }
        }

        /// <summary>
        /// Salva economia de um personagem
        /// </summary>
        public bool SaveCharacterEconomy(string characterId, CharacterEconomySaveData data)
        {
            if (string.IsNullOrEmpty(characterId) || data == null)
                return false;

            try
            {
                string filePath = GetEconomyFilePath(characterId);

                using (StreamWriter writer = new StreamWriter(filePath, false))
                {
                    writer.WriteLine("[Economy]");
                    writer.WriteLine($"CharacterId={data.CharacterId}");
                    writer.WriteLine($"PocketMoney={data.PocketMoney.ToString(CultureInfo.InvariantCulture)}");
                    writer.WriteLine($"BankMoney={data.BankMoney.ToString(CultureInfo.InvariantCulture)}");
                    writer.WriteLine($"CreatedAt={data.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")}");
                    writer.WriteLine($"LastActive={data.LastActive.ToString("yyyy-MM-dd HH:mm:ss")}");
                }

                System.Diagnostics.Debug.WriteLine($"💾 Economia salva: {characterId}");
                System.Diagnostics.Debug.WriteLine($"   Bolso: ${data.PocketMoney:N2}");
                System.Diagnostics.Debug.WriteLine($"   Banco: ${data.BankMoney:N2}");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao salvar economia: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Carrega economia de um personagem
        /// </summary>
        public CharacterEconomySaveData LoadCharacterEconomy(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return null;

            try
            {
                string filePath = GetEconomyFilePath(characterId);

                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"ℹ️ Arquivo não existe: {filePath}");
                    return CreateDefaultEconomy(characterId);
                }

                var data = new CharacterEconomySaveData
                {
                    CharacterId = characterId,
                    PocketMoney = 500m, // Padrão
                    BankMoney = 0m,
                    CreatedAt = DateTime.UtcNow,
                    LastActive = DateTime.UtcNow
                };

                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("PocketMoney="))
                        {
                            string value = line.Substring("PocketMoney=".Length);
                            decimal tempDecimal;
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out tempDecimal))
                            {
                                data.PocketMoney = tempDecimal;
                            }
                        }
                        else if (line.StartsWith("BankMoney="))
                        {
                            string value = line.Substring("BankMoney=".Length);
                            decimal tempDecimal;
                            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out tempDecimal))
                            {
                                data.BankMoney = tempDecimal;
                            }
                        }
                        else if (line.StartsWith("CreatedAt="))
                        {
                            string value = line.Substring("CreatedAt=".Length);
                            DateTime tempDateTime;
                            if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out tempDateTime))
                            {
                                data.CreatedAt = tempDateTime;
                            }
                        }
                        else if (line.StartsWith("LastActive="))
                        {
                            string value = line.Substring("LastActive=".Length);
                            DateTime tempDateTime;
                            if (DateTime.TryParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out tempDateTime))
                            {
                                data.LastActive = tempDateTime;
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"📂 Economia carregada: {characterId}");
                System.Diagnostics.Debug.WriteLine($"   Bolso: ${data.PocketMoney:N2}");
                System.Diagnostics.Debug.WriteLine($"   Banco: ${data.BankMoney:N2}");

                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao carregar economia: {ex.Message}");
                return CreateDefaultEconomy(characterId);
            }
        }

        /// <summary>
        /// Deleta arquivo de economia
        /// </summary>
        public bool DeleteCharacterEconomy(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return false;

            try
            {
                string filePath = GetEconomyFilePath(characterId);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    System.Diagnostics.Debug.WriteLine($"🗑️ Economia deletada: {characterId}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao deletar economia: {ex.Message}");
                return false;
            }
        }

        private string GetEconomyFilePath(string characterId)
        {
            return Path.Combine(_basePath, $"{characterId}_economy.ini");
        }

        private CharacterEconomySaveData CreateDefaultEconomy(string characterId)
        {
            return new CharacterEconomySaveData
            {
                CharacterId = characterId,
                PocketMoney = 500m,
                BankMoney = 0m,
                CreatedAt = DateTime.UtcNow,
                LastActive = DateTime.UtcNow
            };
        }
    }
}