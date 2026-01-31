using GTA;
using GTA.Native;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Drawing;
using RLF.Core;
using RLF.Core.Debug;
using RLF.Core.Jobs.Core;
using RLF.Core.Jobs.Enums;
using RLF.GTA.CoreIntegration;

namespace RLF.GTA.Jobs.Postal
{
    /// <summary>
    /// Sistema principal de gerenciamento dos pontos de trabalho de Carteiro
    /// </summary>
    public class PostalWorkSystem
    {
        // Configurações
        private const float INTERACTION_RADIUS = 2.5f;
        private const bool DEBUG_MODE = false;

        // Blips criados
        private readonly List<Blip> _workBlips;

        // Estado
        private PostalWorkLocation _nearestLocation = null;
        private bool _isProcessing = false;

        // Referência ao job
        private PostalJob _job;
        private bool _jobInitialized = false;

        public PostalWorkSystem()
        {
            _workBlips = new List<Blip>();
            CreateWorkBlips();
            RLFDebug.Info(DebugChannel.System, "[PostalWorkSystem] Sistema inicializado");
        }

        /// <summary>
        /// Cria os blips nos pontos de trabalho
        /// </summary>
        private void CreateWorkBlips()
        {
            try
            {
                foreach (var location in PostalWorkLocations.WorkLocations)
                {
                    // Criar blip
                    Blip blip = World.CreateBlip(location.InteractionPosition);
                    
                    if (blip != null && blip.Exists())
                    {
                        blip.Sprite = BlipSprite.CrateDrop;  // Ícone de pacote/entrega
                        blip.Color = BlipColor.Yellow;
                        blip.Scale = 0.8f;
                        blip.Name = "Trabalho de Carteiro";
                        blip.IsShortRange = true;

                        _workBlips.Add(blip);

                        RLFDebug.Info(DebugChannel.System, $"[PostalWorkSystem] Blip criado: {location.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PostalWorkSystem] Erro ao criar blips", ex);
            }
        }

        /// <summary>
        /// Atualização principal do sistema (chamado a cada frame)
        /// </summary>
        public void Tick()
        {
            try
            {
                // Não processar se já está em processamento
                if (_isProcessing)
                    return;

                Ped player = Game.Player.Character;
                if (player == null || !player.Exists())
                    return;

                // Inicializar job se necessário
                if (!_jobInitialized)
                {
                    TryInitializeJob();
                }

                // Se job está ativo, não mostrar prompts
                if (_job != null && _job.Status == JobStatus.OnShift && _job.CurrentShift.IsActive)
                    return;

                Vector3 playerPos = player.Position;

                _nearestLocation = null;
                float nearestDistance = float.MaxValue;

                // Verificar proximidade com qualquer ponto de trabalho
                foreach (var location in PostalWorkLocations.WorkLocations)
                {
                    float distance = playerPos.DistanceTo(location.InteractionPosition);

                    // Debug visual - marker no ponto
                    if (DEBUG_MODE)
                    {
                        World.DrawMarker(
                            MarkerType.VerticalCylinder,
                            location.InteractionPosition,
                            Vector3.Zero,
                            Vector3.Zero,
                            new Vector3(0.4f, 0.4f, 0.8f),
                            Color.FromArgb(120, 255, 255, 0)
                        );
                    }

                    // Atualizar o mais próximo
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        _nearestLocation = location;
                    }
                }

                // Verificar se está no raio do ponto mais próximo
                if (_nearestLocation != null && nearestDistance <= INTERACTION_RADIUS)
                {
                    // Mostrar prompt (chamado TODO FRAME para não piscar)
                    DisplayHelpText("Pressione ~INPUT_CONTEXT~ para começar a trabalhar de Carteiro");

                    // Verificar input
                    if (Game.IsControlJustPressed(Control.Context))
                    {
                        StartPostalWork(_nearestLocation);
                    }
                }
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PostalWorkSystem] Erro no Tick", ex);
            }
        }

        /// <summary>
        /// Tenta inicializar o PostalJob
        /// </summary>
        private void TryInitializeJob()
        {
            try
            {
                var core = RLFCore.Instance;
                if (core == null || core.State != CoreState.Running)
                    return;

                var economy = EconomyBridge.Current;
                if (economy == null)
                    return;

                var jobSystem = core.Systems.Get("JobSystem") as JobSystem;
                if (jobSystem == null)
                    return;

                _job = jobSystem.Registry.Get(JobType.Delivery) as PostalJob;

                if (_job == null)
                {
                    _job = new PostalJob(core.Logger, core.EventManager, economy);
                    jobSystem.Registry.Register(_job);
                    RLFDebug.Info(DebugChannel.System, "[PostalWorkSystem] PostalJob criado e registrado");
                }

                _jobInitialized = true;
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PostalWorkSystem] Erro ao inicializar job", ex);
            }
        }

        /// <summary>
        /// Inicia o trabalho de carteiro
        /// </summary>
        private void StartPostalWork(PostalWorkLocation location)
        {
            if (_isProcessing)
                return;

            try
            {
                _isProcessing = true;

                // Verificar se job foi inicializado
                if (_job == null)
                {
                    global::GTA.UI.Notification.Show("~r~Sistema de empregos indisponível\n~w~Tente novamente");
                    _isProcessing = false;
                    return;
                }

                // Verificar se já está em turno
                if (_job.Status == JobStatus.OnShift && _job.CurrentShift.IsActive)
                {
                    global::GTA.UI.Notification.Show("~y~Você já está trabalhando!");
                    _isProcessing = false;
                    return;
                }

                // Tentar iniciar turno
                var core = RLFCore.Instance;
                var jobSystem = core.Systems.Get("JobSystem") as JobSystem;

                if (jobSystem != null)
                {
                    bool started = jobSystem.TryStartShift(JobType.Delivery);

                    if (started)
                    {
                        global::GTA.UI.Notification.Show(
                            $"~g~Turno de Carteiro Iniciado!~w~\n" +
                            $"Local: {location.Name}\n" +
                            $"~y~{_job.CurrentShift.TasksTotal}~w~ entregas disponíveis\n" +
                            $"~g~Pegue a bicicleta e comece!"
                        );

                        RLFDebug.Info(DebugChannel.System, $"[PostalWorkSystem] Turno iniciado em {location.Name}");

                        // Spawnar bicicleta no local designado
                        SpawnBikeAtLocation(location);
                    }
                    else
                    {
                        string message = _job.GetStatusMessage();
                        global::GTA.UI.Notification.Show($"~y~{message}");
                        RLFDebug.Info(DebugChannel.System, $"[PostalWorkSystem] Turno não disponível: {message}");
                    }
                }

                _isProcessing = false;
            }
            catch (Exception ex)
            {
                _isProcessing = false;
                RLFDebug.Error(DebugChannel.System, "[PostalWorkSystem] Erro ao iniciar trabalho", ex);
                global::GTA.UI.Notification.Show("~r~Erro ao iniciar trabalho");
            }
        }

        /// <summary>
        /// Spawna a bicicleta no local designado
        /// </summary>
        private void SpawnBikeAtLocation(PostalWorkLocation location)
        {
            try
            {
                Vector3 spawnPos = World.GetNextPositionOnStreet(location.BikeSpawnPosition);
                float heading = 0f;

                Vehicle bike = World.CreateVehicle(
                    new Model(PostalConfig.PostalBike),
                    spawnPos,
                    heading
                );

                if (bike != null && bike.Exists())
                {
                    bike.IsPersistent = true;
                    bike.PlaceOnGround();

                    // Criar blip temporário na bicicleta
                    Blip bikeBlip = bike.AddBlip();
                    bikeBlip.Sprite = BlipSprite.PersonalVehicleBike;
                    bikeBlip.Color = BlipColor.Blue;
                    bikeBlip.Scale = 0.8f;
                    bikeBlip.Name = "Bicicleta dos Correios";
                    bikeBlip.IsShortRange = false;

                    // Marcar waypoint
                    World.WaypointPosition = spawnPos;

                    global::GTA.UI.Notification.Show(
                        "~b~Bicicleta pronta!~w~\n" +
                        "Siga o GPS até a bicicleta"
                    );

                    RLFDebug.Info(DebugChannel.System, "[PostalWorkSystem] Bicicleta spawnada");
                }
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PostalWorkSystem] Erro ao spawnar bicicleta", ex);
            }
        }

        /// <summary>
        /// Exibe texto de ajuda na tela (chamado a cada frame quando necessário)
        /// </summary>
        private void DisplayHelpText(string message)
        {
            try
            {
                // Método correto - deve ser chamado TODO FRAME para não piscar
                global::GTA.UI.Screen.ShowHelpTextThisFrame(message);
            }
            catch
            {
                // Fallback silencioso
            }
        }

        /// <summary>
        /// Limpa os blips ao destruir o sistema
        /// </summary>
        public void Cleanup()
        {
            try
            {
                foreach (var blip in _workBlips)
                {
                    if (blip != null && blip.Exists())
                    {
                        blip.Delete();
                    }
                }
                _workBlips.Clear();

                RLFDebug.Info(DebugChannel.System, "[PostalWorkSystem] Cleanup concluído");
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PostalWorkSystem] Erro no Cleanup", ex);
            }
        }
    }
}
