using RLF.Core.Logging;
using System;

namespace RLF.Core.Utilities
{
    /// <summary>
    /// Executor seguro de operações com tratamento automático de exceções.
    /// Evita crashes e facilita debugging.
    /// </summary>
    public static class SafeExecutor
    {
        /// <summary>
        /// Logger para registrar falhas (opcional).
        /// Deve ser configurado externamente.
        /// </summary>
        public static Logger Logger { get; set; }

        /// <summary>
        /// Executa uma ação de forma segura, capturando exceções.
        /// </summary>
        /// <param name="action">Ação a ser executada</param>
        /// <param name="operationName">Nome da operação (para logging)</param>
        /// <returns>True se executou com sucesso, false se houve exceção</returns>
        public static bool Execute(Action action, string operationName = "Unknown")
        {
            if (action == null)
            {
                Logger?.Warning($"SafeExecutor: Null action for operation '{operationName}'");
                return false;
            }

            try
            {
                action.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return false;
            }
        }

        /// <summary>
        /// Executa uma função de forma segura, retornando um valor ou default.
        /// </summary>
        /// <typeparam name="T">Tipo do retorno</typeparam>
        /// <param name="func">Função a ser executada</param>
        /// <param name="defaultValue">Valor padrão em caso de falha</param>
        /// <param name="operationName">Nome da operação (para logging)</param>
        /// <returns>Resultado da função ou defaultValue em caso de exceção</returns>
        public static T Execute<T>(Func<T> func, T defaultValue = default(T), string operationName = "Unknown")
        {
            if (func == null)
            {
                Logger?.Warning($"SafeExecutor: Null function for operation '{operationName}'");
                return defaultValue;
            }

            try
            {
                return func.Invoke();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return defaultValue;
            }
        }

        /// <summary>
        /// Executa uma ação com um objeto, verificando null antes.
        /// </summary>
        /// <typeparam name="T">Tipo do objeto</typeparam>
        /// <param name="obj">Objeto a ser usado</param>
        /// <param name="action">Ação a executar com o objeto</param>
        /// <param name="operationName">Nome da operação</param>
        /// <returns>True se executou com sucesso</returns>
        public static bool ExecuteWithObject<T>(T obj, Action<T> action, string operationName = "Unknown") where T : class
        {
            if (obj == null)
            {
                Logger?.Warning($"SafeExecutor: Null object for operation '{operationName}'");
                return false;
            }

            if (action == null)
            {
                Logger?.Warning($"SafeExecutor: Null action for operation '{operationName}'");
                return false;
            }

            try
            {
                action.Invoke(obj);
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return false;
            }
        }

        /// <summary>
        /// Executa uma função com retry em caso de falha.
        /// </summary>
        /// <typeparam name="T">Tipo do retorno</typeparam>
        /// <param name="func">Função a executar</param>
        /// <param name="maxRetries">Número máximo de tentativas</param>
        /// <param name="defaultValue">Valor padrão se todas as tentativas falharem</param>
        /// <param name="operationName">Nome da operação</param>
        /// <returns>Resultado da função ou defaultValue</returns>
        public static T ExecuteWithRetry<T>(
            Func<T> func,
            int maxRetries = 3,
            T defaultValue = default(T),
            string operationName = "Unknown")
        {
            if (func == null)
            {
                Logger?.Warning($"SafeExecutor: Null function for operation '{operationName}'");
                return defaultValue;
            }

            if (maxRetries < 1)
                maxRetries = 1;

            Exception lastException = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    T result = func.Invoke();

                    if (attempt > 1)
                    {
                        Logger?.Info($"SafeExecutor: Operation '{operationName}' succeeded on attempt {attempt}");
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (attempt < maxRetries)
                    {
                        Logger?.Warning($"SafeExecutor: Operation '{operationName}' failed on attempt {attempt}, retrying...");
                    }
                }
            }

            // Todas as tentativas falharam
            if (lastException != null)
            {
                HandleException(lastException, $"{operationName} (after {maxRetries} attempts)");
            }

            return defaultValue;
        }

        /// <summary>
        /// Trata exceções de forma centralizada.
        /// </summary>
        /// <param name="ex">Exceção capturada</param>
        /// <param name="operationName">Nome da operação que falhou</param>
        private static void HandleException(Exception ex, string operationName)
        {
            if (Logger != null)
            {
                Logger.Error($"SafeExecutor: Operation '{operationName}' failed", ex);
            }
            else
            {
                // Fallback para console se logger não estiver disponível
                System.Diagnostics.Debug.WriteLine($"[SafeExecutor] Operation '{operationName}' failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SafeExecutor] StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Valida um objeto e executa ação se for válido.
        /// </summary>
        /// <typeparam name="T">Tipo do objeto</typeparam>
        /// <param name="obj">Objeto a validar</param>
        /// <param name="validator">Função de validação</param>
        /// <param name="action">Ação a executar se válido</param>
        /// <param name="operationName">Nome da operação</param>
        /// <returns>True se validou e executou com sucesso</returns>
        public static bool ValidateAndExecute<T>(
            T obj,
            Func<T, bool> validator,
            Action<T> action,
            string operationName = "Unknown")
        {
            if (obj == null)
            {
                Logger?.Warning($"SafeExecutor: Null object for operation '{operationName}'");
                return false;
            }

            if (validator == null || action == null)
            {
                Logger?.Warning($"SafeExecutor: Null validator or action for operation '{operationName}'");
                return false;
            }

            try
            {
                bool isValid = validator.Invoke(obj);

                if (!isValid)
                {
                    Logger?.Debug($"SafeExecutor: Validation failed for operation '{operationName}'");
                    return false;
                }

                action.Invoke(obj);
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return false;
            }
        }
    }
}