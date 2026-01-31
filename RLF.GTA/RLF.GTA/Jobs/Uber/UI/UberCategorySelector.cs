using System;
using System.Collections.Generic;
using GTA;
using LemonUI;
using LemonUI.Menus;
using RLF.Core.Debug;
using RLF.GTA.Jobs.Uber.Ride;
using RLF.GTA.Jobs.Uber.Vehicle;

namespace RLF.GTA.Jobs.Uber.UI
{
    public sealed class UberCategorySelector
    {
        private ObjectPool _menuPool;
        private NativeMenu _categoryMenu;
        private Action<RideCategory> _onCategorySelected;
        private Action _onCancelled;

        public UberCategorySelector()
        {
            _menuPool = new ObjectPool();
        }

        public void Show(
            global::GTA.Vehicle vehicle,
            Action<RideCategory> onSelected,
            Action onCancelled)
        {
            _onCategorySelected = onSelected;
            _onCancelled = onCancelled;

            CreateMenu(vehicle);
            _categoryMenu.Visible = true;
        }

        private void CreateMenu(global::GTA.Vehicle vehicle)
        {
            _categoryMenu = new NativeMenu("UBER", "Selecione o Serviço")
            {
                Offset = new System.Drawing.PointF(50, 0)
            };

            var availableCategories = GetAvailableCategories(vehicle);

            foreach (var category in availableCategories)
            {
                var item = CreateCategoryItem(category);
                _categoryMenu.Add(item);
            }

            var cancelItem = new NativeItem("❌ Cancelar", "Voltar ao menu anterior");
            cancelItem.Activated += (s, e) =>
            {
                _categoryMenu.Visible = false;
                _onCancelled?.Invoke();
            };
            _categoryMenu.Add(cancelItem);

            _menuPool.Add(_categoryMenu);
        }

        private List<RideCategory> GetAvailableCategories(global::GTA.Vehicle vehicle)
        {
            var categories = new List<RideCategory>();

            RideCategory? detectedCategory = UberVehicleCategory.GetCategory(vehicle);

            if (!detectedCategory.HasValue)
            {
                RLFDebug.Warning(DebugChannel.System, "[UberCategorySelector] Veículo sem categoria válida");
                return categories;
            }

            switch (detectedCategory.Value)
            {
                case RideCategory.UberBlack:
                    categories.Add(RideCategory.UberBlack);
                    categories.Add(RideCategory.UberX);
                    break;

                case RideCategory.UberPool:
                    categories.Add(RideCategory.UberPool);
                    categories.Add(RideCategory.UberX);
                    break;

                case RideCategory.UberX:
                    categories.Add(RideCategory.UberX);
                    break;
            }

            return categories;
        }

        private NativeItem CreateCategoryItem(RideCategory category)
        {
            string icon = GetCategoryIcon(category);
            string name = GetCategoryName(category);
            string description = GetCategoryDescription(category);

            var item = new NativeItem($"{icon} {name}", description);

            item.Activated += (s, e) =>
            {
                _categoryMenu.Visible = false;
                _onCategorySelected?.Invoke(category);
            };

            return item;
        }

        private string GetCategoryIcon(RideCategory category)
        {
            switch (category)
            {
                case RideCategory.UberBlack:
                    return "🎩";
                case RideCategory.UberPool:
                    return "👥";
                case RideCategory.UberX:
                    return "🚗";
                default:
                    return "🚕";
            }
        }

        private string GetCategoryName(RideCategory category)
        {
            switch (category)
            {
                case RideCategory.UberBlack:
                    return "Uber Black";
                case RideCategory.UberPool:
                    return "Uber Pool";
                case RideCategory.UberX:
                    return "Uber X";
                default:
                    return "Uber";
            }
        }

        private string GetCategoryDescription(RideCategory category)
        {
            switch (category)
            {
                case RideCategory.UberBlack:
                    return "Veículos de luxo - Pagamento premium";
                case RideCategory.UberPool:
                    return "Viagens compartilhadas";
                case RideCategory.UberX:
                    return "Serviço padrão";
                default:
                    return "Serviço Uber";
            }
        }

        public void Process()
        {
            _menuPool?.Process();
        }

        public void Hide()
        {
            if (_categoryMenu != null)
            {
                _categoryMenu.Visible = false;
            }
        }
    }
}