using System;
using GTA;
using LemonUI;
using LemonUI.Menus;
using RLF.Core.Debug;
using RLF.Core.Economy;
using RLF.Core.Economy.Bank;
using RLF.GTA.Phone.Apps;

namespace RLF.GTA.Phone
{
    /// <summary>
    /// Simulador de menu telefônico
    /// VERSÃO FINAL - Sem sobreposição de menus
    /// </summary>
    public sealed class PhoneMenuSimulator
    {
        private ObjectPool _menuPool;
        private NativeMenu _mainMenu;
        private NativeMenu _contactsMenu;

        private BankPhoneApp _bankApp;
        private EconomySystem _economy;
        private BankSystem _bankSystem;

        private bool _keyPressedLastTick;

        public PhoneMenuSimulator(EconomySystem economy, BankSystem bankSystem)
        {
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _bankSystem = bankSystem ?? throw new ArgumentNullException(nameof(bankSystem));

            _menuPool = new ObjectPool();

            CreateMenus();

            RLFDebug.Info(DebugChannel.System, "[PhoneMenu] Sistema de menu telefônico iniciado");
            System.Diagnostics.Debug.WriteLine("✅ PhoneMenuSimulator criado com sucesso");
        }

        private void CreateMenus()
        {
            _mainMenu = new NativeMenu("CELULAR", "Serviços Disponíveis")
            {
                Alignment = global::GTA.UI.Alignment.Right
            };

            // 🏦 BANCO
            var bankItem = new NativeItem("🏦 Banco", "Acessar conta bancária");
            bankItem.Activated += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine("🏦 Abrindo app de banco...");
                CloseAllMenus(); // 🔥 Fecha tudo antes
                _bankApp.Show();
            };
            _mainMenu.Add(bankItem);

            _mainMenu.Add(new NativeSeparatorItem());

            // Contatos de trabalho
            var contactsItem = new NativeItem("📱 Contatos", "Ver contatos de trabalho");
            contactsItem.Activated += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine("📱 Abrindo contatos...");
                _mainMenu.Visible = false;
                _contactsMenu.Visible = true;
            };
            _mainMenu.Add(contactsItem);

            _menuPool.Add(_mainMenu);

            // Menu de contatos
            _contactsMenu = new NativeMenu("CONTATOS", "Trabalhos Disponíveis")
            {
                Alignment = global::GTA.UI.Alignment.Right
            };

            var fastFoodItem = new NativeItem("🍔 FastFood", "Entregas de comida");
            fastFoodItem.Activated += (sender, e) => OnFastFoodCall();
            _contactsMenu.Add(fastFoodItem);

            var uberItem = new NativeItem("🚕 Uber", "Motorista particular");
            uberItem.Activated += (sender, e) => OnUberCall();
            _contactsMenu.Add(uberItem);

            var backItem = new NativeItem("← Voltar", "Retornar ao menu principal");
            backItem.Activated += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine("← Voltando ao menu principal");
                _contactsMenu.Visible = false;
                _mainMenu.Visible = true;
            };
            _contactsMenu.Add(backItem);

            _menuPool.Add(_contactsMenu);

            // Inicializa app de banco
            _bankApp = new BankPhoneApp(_menuPool, _economy, _bankSystem);
        }

        /// <summary>
        /// MÉTODO PÚBLICO para ser chamado pelo Bootstrap no Tick
        /// </summary>
        public void Update()
        {
            // Processa os menus do LemonUI
            _menuPool.Process();

            // Verifica input
            HandleInput();
        }

        private void HandleInput()
        {
            bool keyDown = Game.IsKeyPressed(System.Windows.Forms.Keys.N);

            // Detecta quando a tecla é pressionada (transição de false -> true)
            if (keyDown && !_keyPressedLastTick)
            {
                System.Diagnostics.Debug.WriteLine("🔑 Tecla N detectada!");
                TogglePhone();
            }

            _keyPressedLastTick = keyDown;
        }

        private void TogglePhone()
        {
            System.Diagnostics.Debug.WriteLine("📱 TogglePhone chamado!");

            // Verifica se QUALQUER menu está aberto
            bool anyMenuOpen = _mainMenu.Visible || _contactsMenu.Visible || _bankApp.IsVisible();

            if (anyMenuOpen)
            {
                // Fecha TUDO
                System.Diagnostics.Debug.WriteLine("   Fechando TODOS os menus");
                CloseAllMenus();
            }
            else
            {
                // Abre o menu principal
                System.Diagnostics.Debug.WriteLine("   Abrindo menu principal");
                _mainMenu.Visible = true;

                global::GTA.UI.Notification.Show(
                    "📱 ~b~Celular Aberto~w~\nNavegue: ~b~↑ ↓~w~ | Selecione: ~g~Enter~w~ | Fechar: ~r~N"
                );
            }
        }

        /// <summary>
        /// 🔥 NOVO: Fecha todos os menus para evitar sobreposição
        /// </summary>
        private void CloseAllMenus()
        {
            _mainMenu.Visible = false;
            _contactsMenu.Visible = false;
            _bankApp.Hide();
        }

        private void OnFastFoodCall()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🍔 Iniciando FastFood...");
                CloseAllMenus(); // Fecha tudo antes de abrir o job

                RLFDebug.Info(DebugChannel.System, "[PhoneMenu] Ligando para FastFood");

                var handler = new RLF.GTA.Jobs.Delivery.DeliveryPhoneHandler();
                handler.OnContactCalled();
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PhoneMenu] Erro ao ligar FastFood", ex);
                global::GTA.UI.Notification.Show("~r~Erro~w~ ao iniciar delivery");
            }
        }

        private void OnUberCall()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚕 Iniciando Uber...");
                CloseAllMenus(); // Fecha tudo antes de abrir o job

                RLFDebug.Info(DebugChannel.System, "[PhoneMenu] Ligando para Uber");

                var handler = new RLF.GTA.Jobs.Uber.Phone.UberPhoneHandler();
                handler.OnContactCalled();
            }
            catch (Exception ex)
            {
                RLFDebug.Error(DebugChannel.System, "[PhoneMenu] Erro ao ligar Uber", ex);
                global::GTA.UI.Notification.Show("~r~Erro~w~ ao iniciar Uber");
            }
        }

        /// <summary>
        /// Verifica se algum menu está visível
        /// </summary>
        public bool IsVisible()
        {
            return _mainMenu.Visible || _contactsMenu.Visible || _bankApp.IsVisible();
        }
    }
}