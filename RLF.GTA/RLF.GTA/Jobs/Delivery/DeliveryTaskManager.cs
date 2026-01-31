using System;
using GTA;
using GTA.Math;
using RLF.Core.Debug;

namespace RLF.GTA.Jobs.Delivery
{
    public sealed class DeliveryTaskManager
    {
        private Vector3 _currentDestination;
        private Blip _destinationBlip;
        private bool _hasActiveTask;

        public bool HasActiveTask => _hasActiveTask;
        public Vector3 CurrentDestination => _currentDestination;

        public void StartTask(Vector3 destination)
        {
            try
            {
                ClearCurrentTask();

                _currentDestination = destination;
                _hasActiveTask = true;

                CreateDestinationBlip();
                SetWaypoint();

                RLFDebug.Info(DebugChannel.System, "[Delivery] Nova tarefa iniciada");
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[Delivery] Erro ao iniciar tarefa", ex);
            }
        }

        private void CreateDestinationBlip()
        {
            try
            {
                _destinationBlip?.Delete();

                _destinationBlip = World.CreateBlip(_currentDestination);
                _destinationBlip.Sprite = BlipSprite.Standard;
                _destinationBlip.Color = BlipColor.Yellow;
                _destinationBlip.Scale = 0.9f;
                _destinationBlip.Name = "Destino da Entrega";
                _destinationBlip.IsShortRange = false;
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[Delivery] Erro ao criar blip de destino", ex);
            }
        }

        private void SetWaypoint()
        {
            try
            {
                World.WaypointPosition = _currentDestination;
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[Delivery] Erro ao definir waypoint", ex);
            }
        }

        public void CompleteTask()
        {
            ClearCurrentTask();
            RLFDebug.Info(DebugChannel.System, "[Delivery] Tarefa concluída");
        }

        private void ClearCurrentTask()
        {
            try
            {
                _destinationBlip?.Delete();
                _destinationBlip = null;
                _hasActiveTask = false;
            }
            catch { }
        }

        public void Cleanup()
        {
            ClearCurrentTask();
        }
    }
}