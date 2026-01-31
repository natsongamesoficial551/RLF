using System;
using System.Windows.Forms;
using GTA;

namespace RLF.GTA.CharacterCreator
{
    public class CharacterCreatorBootstrap : Script
    {
        private bool _initialized;

        public CharacterCreatorBootstrap()
        {
            _initialized = false;
            Tick += OnTick;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!_initialized)
            {
                try
                {
                    CharacterCreatorSystem.Instance.Initialize();
                    _initialized = true;
                }
                catch { }
            }
        }
    }
}