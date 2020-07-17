using Kingmaker.Items;

namespace CraftMagicItems.Patches.Harmony
{
    /// <summary>Class containing validations for Harmony patched methods</summary>
    public static class Validation
    {
        /// <summary>Checks whether the specified <paramref name="weapon" /> can be enchanted</summary>
        /// <param name="weapon"><see cref="ItemEntityWeapon" /> to evaluate</param>
        /// <param name="owner"><see cref="ItemEntity" /> owning the weapon(?); maybe shields or two-sided weapons?</param>
        /// <returns>whether enchantments are valud for <paramref name="weapon" /></returns>
        public static bool EquipmentEnchantmentValid(ItemEntityWeapon weapon, ItemEntity owner)
        {
            if ((weapon == owner) ||
                (weapon != null && (weapon.Blueprint.IsNatural || weapon.Blueprint.IsUnarmed)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}