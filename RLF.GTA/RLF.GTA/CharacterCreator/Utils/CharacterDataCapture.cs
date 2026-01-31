using System;
using GTA;
using GTA.Native;
using RLF.Core.CharacterCreator.Data;
using RLF.Core.CharacterCreator.Enums;

namespace RLF.GTA.CharacterCreator.Utils
{
    /// <summary>
    /// Captura os dados do personagem atual do player
    /// </summary>
    public static class CharacterDataCapture
    {
        public static CharacterData CaptureFromPlayer()
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists())
                return null;

            try
            {
                var data = new CharacterData();
                data.Name = "Personagem " + DateTime.Now.ToString("HH:mm:ss");

                // Detectar gênero baseado no modelo
                if (player.Model.Hash == unchecked((int)1885233392)) // mp_m_freemode_01
                {
                    data.Gender = CharacterGender.Male;
                }
                else if (player.Model.Hash == unchecked((int)2627665880)) // mp_f_freemode_01
                {
                    data.Gender = CharacterGender.Female;
                }

                System.Diagnostics.Debug.WriteLine("📸 Capturando dados do personagem atual...");

                // GENETICS - Não é possível capturar, mantém valores padrão
                // (GTA não tem natives para ler head blend data)

                // HAIR
                data.Hair.Style = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, player.Handle, 2);
                data.Hair.PrimaryColor = Function.Call<int>(unchecked((Hash)0x2B16A3BF), player.Handle);
                data.Hair.HighlightColor = Function.Call<int>(unchecked((Hash)0x4CFD0FEA), player.Handle);

                System.Diagnostics.Debug.WriteLine($"   Cabelo capturado: Style {data.Hair.Style}, Color {data.Hair.PrimaryColor}");

                // EYE COLOR
                data.EyeColor = Function.Call<int>(unchecked((Hash)0x76BBA2CE), player.Handle);

                // OVERLAYS
                // OVERLAYS
                for (int i = 0; i < 12; i++)
                {
                    int index = Function.Call<int>(unchecked((Hash)0xA60EF3B6), player.Handle, i);

                    if (index >= 0)
                    {
                        OverlayType type = GetOverlayTypeFromIndex(i);
                        float opacity = 1.0f; // Não há native para ler opacity
                        int primaryColor = 0;
                        int secondaryColor = 0;

                        data.Overlays.SetOverlay(type, index, opacity, primaryColor, secondaryColor);
                        System.Diagnostics.Debug.WriteLine($"   Overlay {type}: Index {index}");
                    }
                }

                // CLOTHING
                foreach (ComponentType type in Enum.GetValues(typeof(ComponentType)))
                {
                    int componentId = GetComponentId(type);
                    int drawable = Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, player.Handle, componentId);
                    int texture = Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, player.Handle, componentId);

                    data.Clothing.SetComponent(type, drawable, texture);

                    if (drawable > 0 || texture > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"   Roupa {type}: Drawable {drawable}, Texture {texture}");
                    }
                }

                // PROPS
                foreach (PropType type in Enum.GetValues(typeof(PropType)))
                {
                    int propId = GetPropId(type);
                    int drawable = Function.Call<int>(Hash.GET_PED_PROP_INDEX, player.Handle, propId);
                    int texture = Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, player.Handle, propId);

                    data.Props.SetProp(type, drawable, texture);

                    if (drawable >= 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"   Prop {type}: Drawable {drawable}, Texture {texture}");
                    }
                }

                System.Diagnostics.Debug.WriteLine("✅ Captura concluída!");
                return data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ao capturar personagem: {ex.Message}");
                return null;
            }
        }

        private static OverlayType GetOverlayTypeFromIndex(int index)
        {
            switch (index)
            {
                case 0: return OverlayType.Blemishes;
                case 1: return OverlayType.FacialHair;
                case 2: return OverlayType.Eyebrows;
                case 3: return OverlayType.Aging;
                case 4: return OverlayType.Makeup;
                case 5: return OverlayType.Blush;
                case 6: return OverlayType.Complexion;
                case 7: return OverlayType.SunDamage;
                case 8: return OverlayType.Lipstick;
                case 9: return OverlayType.Freckles;
                case 10: return OverlayType.ChestHair;
                case 11: return OverlayType.BodyBlemishes;
                default: return OverlayType.Blemishes;
            }
        }

        private static int GetComponentId(ComponentType type)
        {
            switch (type)
            {
                case ComponentType.Head: return 0;
                case ComponentType.Mask: return 1;
                case ComponentType.Hair: return 2;
                case ComponentType.Torso: return 3;
                case ComponentType.Legs: return 4;
                case ComponentType.Bag: return 5;
                case ComponentType.Feet: return 6;
                case ComponentType.Accessories: return 7;
                case ComponentType.Undershirt: return 8;
                case ComponentType.Armor: return 9;
                case ComponentType.Decals: return 10;
                case ComponentType.Tops: return 11;
                default: return 0;
            }
        }

        private static int GetPropId(PropType type)
        {
            switch (type)
            {
                case PropType.Hat: return 0;
                case PropType.Glasses: return 1;
                case PropType.Ear: return 2;
                case PropType.Watch: return 6;
                case PropType.Bracelet: return 7;
                default: return 0;
            }
        }
    }
}