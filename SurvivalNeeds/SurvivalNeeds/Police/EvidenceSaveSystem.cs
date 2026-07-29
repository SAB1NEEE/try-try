using GTA;
using GTA.Native;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SurvivalNeeds.Systems
{
    public class EvidenceSaveSystem
    {
        private readonly string evidenceFile;

        public EvidenceSaveSystem()
        {
            string folder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "SurvivalNeeds"
            );

            Directory.CreateDirectory(folder);

            evidenceFile = Path.Combine(
                folder,
                "evidence.ini"
            );
        }

        public class WeaponEntry
        {
            public WeaponHash Weapon;
            public int Ammo;
        }

        public void SaveWeapons(List<WeaponEntry> weapons)
        {
            List<string> lines = new List<string>();

            lines.Add("WeaponCount=" + weapons.Count);

            for (int i = 0; i < weapons.Count; i++)
            {
                lines.Add(
                    "Weapon" + i + "=" +
                    weapons[i].Weapon
                );

                lines.Add(
                    "Ammo" + i + "=" +
                    weapons[i].Ammo
                );
            }

            File.WriteAllLines(
                evidenceFile,
                lines
            );
        }
    }
}