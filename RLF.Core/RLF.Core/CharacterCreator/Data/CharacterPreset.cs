using System;
using RLF.Core.CharacterCreator.Enums;

namespace RLF.Core.CharacterCreator.Data
{
    [Serializable]
    public class CharacterPreset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public CharacterGender Gender { get; set; }
        public CharacterData CharacterData { get; set; }
        public bool IsSystemPreset { get; set; }
        public DateTime CreatedAt { get; set; }

        public CharacterPreset()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;
            IsSystemPreset = false;
        }

        public static CharacterPreset FromCharacterData(CharacterData data, string name, string description = "")
        {
            return new CharacterPreset
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                Gender = data.Gender,
                CharacterData = data.Clone(),
                IsSystemPreset = false,
                CreatedAt = DateTime.Now
            };
        }
    }
}