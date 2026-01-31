using RLF.Core.CharacterCreator.Data;
using RLF.Core.CharacterCreator.Enums;

namespace RLF.Core.CharacterCreator.Interfaces
{
    public interface ICharacterBuilder
    {
        void ApplyGenetics(CharacterGenetics genetics);
        void ApplyFaceFeature(FaceFeature feature, float value);
        void ApplyAllFaceFeatures(CharacterFaceFeatures features);
        void ApplyOverlay(CharacterOverlay overlay);
        void ApplyAllOverlays(CharacterOverlays overlays);
        void ApplyHair(CharacterHair hair);
        void ApplyEyeColor(int eyeColor);
        void ApplyComponent(ComponentType component, int drawable, int texture);
        void ApplyAllComponents(CharacterClothing clothing);
        void ApplyProp(PropType prop, int drawable, int texture);
        void ApplyAllProps(CharacterProps props);
        void ApplyFullCharacter(CharacterData character);
        void ClearProp(PropType prop);
        void ClearAllProps();
        int GetMaxDrawables(ComponentType component);
        int GetMaxTextures(ComponentType component, int drawable);
        int GetMaxPropDrawables(PropType prop);
        int GetMaxPropTextures(PropType prop, int drawable);
    }
}