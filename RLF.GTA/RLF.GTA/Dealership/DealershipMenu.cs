using GTA;
using System.Collections.Generic;
using static RLF.GTA.Dealership.DealershipCatalog;

namespace RLF.GTA.Dealership
{
    public sealed class DealershipMenu
    {
        private int _categoryIndex;
        private int _vehicleIndex;
        private bool _inVehicleList;

        private readonly List<string> _categories;

        public DealershipMenu()
        {
            _categories = new List<string>(DealershipCatalog.Categories.Keys);
        }

        public void Reset()
        {
            _categoryIndex = 0;
            _vehicleIndex = 0;
            _inVehicleList = false;
        }

        public VehicleEntry Tick()
        {
            string title;
            string line;

            if (!_inVehicleList)
            {
                title = "Concessionária";
                line = _categories[_categoryIndex];
            }
            else
            {
                var list = DealershipCatalog.Categories[_categories[_categoryIndex]];
                var v = list[_vehicleIndex];
                title = v.Name;
                line = $"Preço: ${v.Price:N0}";
            }

            global::GTA.UI.Screen.ShowSubtitle(
            $"~y~{title}~s~\n{line}\n↑ ↓ navegar | Enter selecionar | Back voltar",
            1
        );

            if (Game.IsControlJustPressed(Control.FrontendUp))
                (_inVehicleList ? ref _vehicleIndex : ref _categoryIndex)--;
            if (Game.IsControlJustPressed(Control.FrontendDown))
                (_inVehicleList ? ref _vehicleIndex : ref _categoryIndex)++;

            if (Game.IsControlJustPressed(Control.FrontendAccept))
            {
                if (!_inVehicleList)
                {
                    _inVehicleList = true;
                    _vehicleIndex = 0;
                }
                else
                {
                    var list = DealershipCatalog.Categories[_categories[_categoryIndex]];
                    return list[_vehicleIndex];
                }
            }

            if (Game.IsControlJustPressed(Control.FrontendCancel))
            {
                if (_inVehicleList)
                    _inVehicleList = false;
                else
                    return null;
            }

            return null;
        }
    }
}
