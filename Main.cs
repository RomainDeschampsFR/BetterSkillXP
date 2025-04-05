using MelonLoader;
using UnityEngine;
using Il2CppInterop;
using Il2CppInterop.Runtime.Injection; 
using System.Collections;
using Il2Cpp;
using ModData;

namespace BetterSkillXP
{
    public class Main : MelonMod
    {
        public static bool dataLoaded = false;
        public static ModDataManager dataManager = new ModDataManager("BetterSkillXP", false);

        public override void OnInitializeMelon()
        {
            Debug.Log($"[{Info.Name}] Version {Info.Version} loaded!");
            Settings.OnLoad();
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (GameManager.IsMainMenuActive())
            {
                dataLoaded = false;
            }

            if (!GameManager.IsBootSceneActive() && !GameManager.IsMainMenuActive() && !GameManager.IsEmptySceneActive() && !dataLoaded)
            {
                MelonCoroutines.Start(LoadParameters());
            }
        }

        public static IEnumerator LoadParameters()
        {
            if (!dataLoaded)
            {
                float waitSeconds = 0.2f;
                for (float t = 0f; t < waitSeconds; t += Time.deltaTime) yield return null;

                string serializedData = dataManager.Load();

                if (serializedData != null)
                {
                    string[] deserializedData = serializedData.Split(";");
                    Patches.accumulatedMendingTime = (float.TryParse(deserializedData[0], out float result0)) ? result0 : 0;
                    Patches.accumulatedCookingTime = (float.TryParse(deserializedData[1], out float result1)) ? result1 : 0;
                    Patches.accumulatedStartingFireTime = (float.TryParse(deserializedData[2], out float result2)) ? result2 : 0;
                    Patches.accumulatedGunsmithingTime = (float.TryParse(deserializedData[3], out float result3)) ? result3 : 0;
                }

                dataLoaded = true;
            }
        }

        public static void SaveParameters()
        {
            if (dataManager != null)
            {
                dataManager.Save($"{Patches.accumulatedMendingTime};{Patches.accumulatedCookingTime};{Patches.accumulatedStartingFireTime};{Patches.accumulatedGunsmithingTime}");
            }
        }
                            

        public static float MaybeIncrementPoints(float accumulatedTime, SkillType skillType)
        {
            float hoursPerPoints = 0;

            if (skillType == SkillType.Cooking) hoursPerPoints = Settings.settings.cookingHours;
            else if (skillType == SkillType.ClothingRepair) hoursPerPoints = Settings.settings.mendingHours;
            else if (skillType == SkillType.Firestarting) hoursPerPoints = Settings.settings.fireStartingMinutes/60;
            else if (skillType == SkillType.Gunsmithing) hoursPerPoints = Settings.settings.gunsmithingHours;

            float result = 0;
            int nbPoints = 0; 

            if (hoursPerPoints != 0)
            {
                result = accumulatedTime / hoursPerPoints;
                nbPoints = (int)result;
            }

            if (nbPoints > 0)
            {
                Patches.allowPointToBeEarned = true;
                GameManager.GetSkillsManager().IncrementPointsAndNotify(skillType, nbPoints, SkillsManager.PointAssignmentMode.AssignOnlyInSandbox);
            }

            return accumulatedTime - (nbPoints * hoursPerPoints);
        }
    }
}