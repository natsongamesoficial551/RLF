using System;
using GTA;
using GTA.Native;
using RLF.Core.CharacterCreator.Data;
using RLF.Core.CharacterCreator.Enums;

namespace RLF.GTA.CharacterCreator.Core
{
    public class CharacterBuilder
    {
        private Ped _ped;
        private CharacterGenetics _lastGenetics;
        private CharacterHair _lastHair;

        public void SetPed(Ped ped) => _ped = ped;
        public bool IsValid() => _ped != null && _ped.Exists();

        public void ApplyGenetics(CharacterGenetics genetics)
        {
            if (!IsValid() || genetics == null) return;

            try
            {
                _lastGenetics = genetics;

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("🧬 APLICANDO GENETICS");
                System.Diagnostics.Debug.WriteLine($"   ShapeFirst: {genetics.ShapeFirst}");
                System.Diagnostics.Debug.WriteLine($"   ShapeMix: {genetics.ShapeMix}");

                Function.Call(Hash.SET_PED_HEAD_BLEND_DATA,
                    _ped.Handle,
                    Clamp(genetics.ShapeFirst, 0, 45),
                    Clamp(genetics.ShapeSecond, 0, 45),
                    Clamp(genetics.ShapeThird, 0, 45),
                    Clamp(genetics.SkinFirst, 0, 45),
                    Clamp(genetics.SkinSecond, 0, 45),
                    Clamp(genetics.SkinThird, 0, 45),
                    Clamp(genetics.ShapeMix, 0f, 1f),
                    Clamp(genetics.SkinMix, 0f, 1f),
                    Clamp(genetics.ThirdMix, 0f, 1f),
                    false);

                System.Diagnostics.Debug.WriteLine("✅ Genetics aplicado");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERRO ApplyGenetics: {ex.Message}");
            }
        }

        public void ApplyAllFaceFeatures(CharacterFaceFeatures features)
        {
            if (!IsValid() || features == null) return;

            System.Diagnostics.Debug.WriteLine("👤 Aplicando face features...");

            for (int i = 0; i < 20; i++)
            {
                try
                {
                    float value = Clamp(features.GetFeature((FaceFeature)i), -1f, 1f);
                    Function.Call((Hash)0x71A5C1DBA060049E, _ped.Handle, i, value);
                }
                catch { }
            }

            System.Diagnostics.Debug.WriteLine("✅ Face features aplicados");
        }

        // ✅ CORRIGIDO: Removido Script.Wait() - este método não é assíncrono
        public void ApplyHair(CharacterHair hair)
        {
            if (!IsValid() || hair == null) return;

            try
            {
                _lastHair = hair;

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("💇 APLICANDO CABELO");
                System.Diagnostics.Debug.WriteLine($"   Style: {hair.Style}");
                System.Diagnostics.Debug.WriteLine($"   PrimaryColor: {hair.PrimaryColor}");
                System.Diagnostics.Debug.WriteLine($"   HighlightColor: {hair.HighlightColor}");

                int style = Clamp(hair.Style, 0, 76);
                int primary = Clamp(hair.PrimaryColor, 0, 63);
                int highlight = Clamp(hair.HighlightColor, 0, 63);

                // Aplicar modelo do cabelo
                System.Diagnostics.Debug.WriteLine($"✂️ [1/2] Aplicando style {style}...");
                Function.Call(
                    Hash.SET_PED_COMPONENT_VARIATION,
                    _ped.Handle,
                    2,
                    style,
                    0,
                    0
                );

                // Aplicar cores
                System.Diagnostics.Debug.WriteLine($"🎨 [2/2] Aplicando cores {primary}/{highlight}...");
                Function.Call(
                    (Hash)0x4CFFC65454C93A49,
                    _ped.Handle,
                    primary,
                    highlight
                );

                System.Diagnostics.Debug.WriteLine("✅ CABELO APLICADO COM SUCESSO!");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"❌ ERRO ApplyHair: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            }
        }

        // ✅ Re-aplicar cores do cabelo (usado após overlays que resetam cores)
        private void ReapplyHairColors()
        {
            if (!IsValid() || _lastHair == null) return;

            try
            {
                int primary = Clamp(_lastHair.PrimaryColor, 0, 63);
                int highlight = Clamp(_lastHair.HighlightColor, 0, 63);

                System.Diagnostics.Debug.WriteLine($"🔄 Re-aplicando cores do cabelo: {primary}/{highlight}");
                Function.Call((Hash)0x4CFFC65454C93A49, _ped.Handle, primary, highlight);
            }
            catch { }
        }

        public void ApplyEyeColor(int colorIndex)
        {
            if (!IsValid()) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"👁️ Aplicando cor dos olhos: {colorIndex}");
                Function.Call((Hash)0x50B56988B170AFDF, _ped.Handle, Clamp(colorIndex, 0, 31));
            }
            catch { }
        }

        public void ApplyOverlay(CharacterOverlay overlay)
        {
            if (!IsValid() || overlay == null) return;

            try
            {
                int idx = GetOverlayIndex(overlay.Type);

                if (overlay.Index < 0)
                {
                    Function.Call(Hash.SET_PED_HEAD_OVERLAY, _ped.Handle, idx, 0, 0f);
                }
                else
                {
                    Function.Call(Hash.SET_PED_HEAD_OVERLAY, _ped.Handle, idx,
                        Clamp(overlay.Index, 0, 30),
                        Clamp(overlay.Opacity, 0f, 1f));

                    if (overlay.Type == OverlayType.FacialHair ||
                        overlay.Type == OverlayType.Eyebrows ||
                        overlay.Type == OverlayType.ChestHair)
                    {
                        Function.Call((Hash)0x497BF74A7B9CB952, _ped.Handle, idx, 1,
                            Clamp(overlay.PrimaryColor, 0, 63),
                            Clamp(overlay.SecondaryColor, 0, 63));
                    }
                    else if (overlay.Type == OverlayType.Blush || overlay.Type == OverlayType.Lipstick)
                    {
                        Function.Call((Hash)0x497BF74A7B9CB952, _ped.Handle, idx, 2,
                            Clamp(overlay.PrimaryColor, 0, 63),
                            Clamp(overlay.SecondaryColor, 0, 63));
                    }
                }

                // ✅ Re-aplicar cores do cabelo após barba/sobrancelhas
                if (overlay.Type == OverlayType.FacialHair || overlay.Type == OverlayType.Eyebrows)
                {
                    ReapplyHairColors();
                }
            }
            catch { }
        }

        public void ApplyAllOverlays(CharacterOverlays overlays)
        {
            if (!IsValid() || overlays == null) return;

            System.Diagnostics.Debug.WriteLine("🎨 Aplicando overlays...");

            foreach (OverlayType type in Enum.GetValues(typeof(OverlayType)))
            {
                var overlay = overlays.GetOverlay(type);
                if (overlay != null) ApplyOverlay(overlay);
            }

            // ✅ Re-aplicar cores do cabelo após TODOS os overlays
            ReapplyHairColors();

            System.Diagnostics.Debug.WriteLine("✅ Overlays aplicados");
        }

        private int GetOverlayIndex(OverlayType type)
        {
            switch (type)
            {
                case OverlayType.Blemishes: return 0;
                case OverlayType.FacialHair: return 1;
                case OverlayType.Eyebrows: return 2;
                case OverlayType.Aging: return 3;
                case OverlayType.Makeup: return 4;
                case OverlayType.Blush: return 5;
                case OverlayType.Complexion: return 6;
                case OverlayType.SunDamage: return 7;
                case OverlayType.Lipstick: return 8;
                case OverlayType.Freckles: return 9;
                case OverlayType.ChestHair: return 10;
                case OverlayType.BodyBlemishes: return 11;
                default: return 0;
            }
        }

        public void ApplyComponent(ComponentType type, int drawable, int texture)
        {
            if (!IsValid()) return;

            try
            {
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, _ped.Handle,
                    GetComponentId(type),
                    Clamp(drawable, 0, 500),
                    Clamp(texture, 0, 100), 0);
            }
            catch { }
        }

        public void ApplyClothing(CharacterClothing clothing)
        {
            if (!IsValid() || clothing == null) return;

            System.Diagnostics.Debug.WriteLine("👕 Aplicando roupas...");

            foreach (ComponentType type in Enum.GetValues(typeof(ComponentType)))
            {
                // ✅ NÃO deixar roupa sobrescrever o cabelo (componente 2)
                // (mesmo que o enum tenha nome diferente, o ID 2 é o Hair no GTA)
                if ((int)type == 2)
                    continue;

                var item = clothing.GetComponent(type);
                if (item != null)
                    ApplyComponent(type, item.DrawableId, item.TextureId);
            }

            System.Diagnostics.Debug.WriteLine("✅ Roupas aplicadas");
        }

        private int GetComponentId(ComponentType type) => (int)type;

        public void ApplyProp(PropType type, int drawable, int texture)
        {
            if (!IsValid()) return;

            try
            {
                int propId = GetPropId(type);

                if (drawable < 0)
                    Function.Call(Hash.CLEAR_PED_PROP, _ped.Handle, propId);
                else
                    Function.Call(Hash.SET_PED_PROP_INDEX, _ped.Handle, propId,
                        Clamp(drawable, 0, 200),
                        Clamp(texture, 0, 50), true);
            }
            catch { }
        }

        public void ApplyProps(CharacterProps props)
        {
            if (!IsValid() || props == null) return;

            System.Diagnostics.Debug.WriteLine("🎩 Aplicando props...");

            foreach (PropType type in Enum.GetValues(typeof(PropType)))
            {
                var item = props.GetProp(type);
                if (item != null) ApplyProp(type, item.DrawableId, item.TextureId);
            }

            System.Diagnostics.Debug.WriteLine("✅ Props aplicados");
        }

        private int GetPropId(PropType type)
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

        public void ClearAllProps()
        {
            if (!IsValid()) return;
            try { Function.Call(Hash.CLEAR_ALL_PED_PROPS, _ped.Handle); } catch { }
        }

        public void ApplyDefaultAppearance()
        {
            if (!IsValid()) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Aplicando aparência padrão...");

                Function.Call(Hash.SET_PED_HEAD_BLEND_DATA, _ped.Handle, 0, 0, 0, 0, 0, 0, 0.5f, 0.5f, 0f, false);
                Function.Call(Hash.SET_PED_COMPONENT_VARIATION, _ped.Handle, 2, 0, 0, 0);
                Function.Call((Hash)0x4CFFC65454C93A49, _ped.Handle, 0, 0);
                ClearAllProps();

                System.Diagnostics.Debug.WriteLine("✅ Aparência padrão aplicada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Erro ApplyDefaultAppearance: {ex.Message}");
            }
        }

        // ✅ CORRIGIDO: Método principal - chamadas diretas sem Script.Wait
        public void ApplyFullCharacter(CharacterData data)
        {
            if (!IsValid() || data == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("👤 APLICANDO PERSONAGEM COMPLETO");
                System.Diagnostics.Debug.WriteLine($"   Nome: {data.Name}");
                System.Diagnostics.Debug.WriteLine($"   Gênero: {data.Gender}");
                System.Diagnostics.Debug.WriteLine($"   Cabelo Style: {data.Hair?.Style}");
                System.Diagnostics.Debug.WriteLine($"   Cabelo Cor: {data.Hair?.PrimaryColor}");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");

                // ORDEM SEGURA (SEM WAIT)
                ApplyGenetics(data.Genetics);
                ApplyAllFaceFeatures(data.FaceFeatures);
                ApplyEyeColor(data.EyeColor);

                // CABELO ANTES DOS OVERLAYS
                ApplyHair(data.Hair);

                // OVERLAYS (barba/sobrancelha resetam cabelo)
                ApplyAllOverlays(data.Overlays);

                // ROUPAS E PROPS
                ApplyClothing(data.Clothing);
                ApplyProps(data.Props);

                // GARANTIA FINAL DO CABELO
                ReapplyHairColors();

                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine("✅ PERSONAGEM APLICADO COM SUCESSO!");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
                System.Diagnostics.Debug.WriteLine($"❌ ERRO ApplyFullCharacter: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"   Stack: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine("════════════════════════════════════════");
            }
        }

        public void ApplyFaceFeature(FaceFeature feature, float value)
        {
            if (!IsValid()) return;

            try
            {
                float safe = Clamp(value, -1f, 1f);
                Function.Call((Hash)0x71A5C1DBA060049E, _ped.Handle, (int)feature, safe);
            }
            catch { }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}