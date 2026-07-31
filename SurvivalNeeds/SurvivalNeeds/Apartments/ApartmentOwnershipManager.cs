using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SurvivalNeeds.Apartments
{
    public class ApartmentOwnershipManager
    {
        private readonly string ownershipFolder;

        private readonly Dictionary<string, HashSet<string>>
            ownedApartmentsByProfile =
                new Dictionary<string, HashSet<string>>();

        public ApartmentOwnershipManager()
        {
            ownershipFolder =
                GetOwnershipFolder();

            Directory.CreateDirectory(
                ownershipFolder
            );
        }

        //====================================================
        // CHECK OWNERSHIP
        //====================================================

        public bool IsOwned(
            string profileId,
            string apartmentId)
        {
            profileId =
                NormalizeProfileId(
                    profileId
                );

            if (string.IsNullOrWhiteSpace(
                apartmentId))
            {
                return false;
            }

            EnsureProfileLoaded(
                profileId
            );

            return ownedApartmentsByProfile[
                profileId
            ].Contains(
                apartmentId
            );
        }

        //====================================================
        // ADD OWNERSHIP
        //====================================================

        public bool AddOwnership(
            string profileId,
            string apartmentId)
        {
            profileId =
                NormalizeProfileId(
                    profileId
                );

            if (string.IsNullOrWhiteSpace(
                apartmentId))
            {
                return false;
            }

            EnsureProfileLoaded(
                profileId
            );

            HashSet<string> ownedApartments =
                ownedApartmentsByProfile[
                    profileId
                ];

            if (ownedApartments.Contains(
                apartmentId))
            {
                return false;
            }

            ownedApartments.Add(
                apartmentId
            );

            SaveProfile(
                profileId
            );

            return true;
        }

        //====================================================
        // LOAD PROFILE
        //====================================================

        private void EnsureProfileLoaded(
            string profileId)
        {
            if (ownedApartmentsByProfile.ContainsKey(
                profileId))
            {
                return;
            }

            HashSet<string> ownedApartments =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase
                );

            string savePath =
                GetProfileSavePath(
                    profileId
                );

            if (File.Exists(
                savePath))
            {
                string[] lines =
                    File.ReadAllLines(
                        savePath
                    );

                foreach (string line in lines)
                {
                    string apartmentId =
                        line == null
                            ? string.Empty
                            : line.Trim();

                    if (string.IsNullOrWhiteSpace(
                        apartmentId))
                    {
                        continue;
                    }

                    ownedApartments.Add(
                        apartmentId
                    );
                }
            }

            ownedApartmentsByProfile[
                profileId
            ] =
                ownedApartments;
        }

        //====================================================
        // SAVE PROFILE
        //====================================================

        private void SaveProfile(
            string profileId)
        {
            EnsureProfileLoaded(
                profileId
            );

            string savePath =
                GetProfileSavePath(
                    profileId
                );

            string[] apartmentIds =
                new string[
                    ownedApartmentsByProfile[
                        profileId
                    ].Count
                ];

            ownedApartmentsByProfile[
                profileId
            ].CopyTo(
                apartmentIds
            );

            Array.Sort(
                apartmentIds,
                StringComparer.OrdinalIgnoreCase
            );

            File.WriteAllLines(
                savePath,
                apartmentIds
            );
        }

        //====================================================
        // SAVE ALL
        //====================================================

        public void SaveAll()
        {
            foreach (
                string profileId
                in ownedApartmentsByProfile.Keys)
            {
                SaveProfile(
                    profileId
                );
            }
        }

        //====================================================
        // FILE PATH
        //====================================================

        private string GetProfileSavePath(
            string profileId)
        {
            return Path.Combine(
                ownershipFolder,
                "apartment_ownership_" +
                profileId +
                ".ini"
            );
        }

        private string GetOwnershipFolder()
        {
            string baseDirectory =
                AppDomain.CurrentDomain.BaseDirectory;

            string survivalNeedsFolder =
                Path.Combine(
                    "SurvivalNeeds"
                );

            string apartmentsFolder =
                Path.Combine(
                    survivalNeedsFolder,
                    "apartments"
                );

            return apartmentsFolder;
        }

        //====================================================
        // PROFILE ID
        //====================================================

        private string NormalizeProfileId(
            string profileId)
        {
            if (string.IsNullOrWhiteSpace(
                profileId))
            {
                return "DEFAULT";
            }

            foreach (
                char invalidCharacter
                in Path.GetInvalidFileNameChars())
            {
                profileId =
                    profileId.Replace(
                        invalidCharacter,
                        '_'
                    );
            }

            return profileId.Trim();
        }
    }
}