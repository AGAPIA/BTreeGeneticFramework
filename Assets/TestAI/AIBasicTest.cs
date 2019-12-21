using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class AIBasicTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void NewTestScriptSimplePasses()
        {
            // Use the Assert class to test conditions
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator AIBehavior_ProbabilityOfBoxes()
        {
            GameObject gameObj                  = Object.Instantiate(Resources.Load<GameObject>("Prefabs/Game"));
            GameObject gameManagerObj           = gameObj.transform.Find("GameManager").gameObject;

            GameManager gameManagerScript       = gameManagerObj.GetComponent<GameManager>();
            BoxesSpawnScript boxesManager       = gameManagerObj.GetComponent<BoxesSpawnScript>();
            boxesManager.SpawnOnDemand          = true;

            // Take the tanks and position them at certain locations on the map
            GameObject[] allTanks               = GameObject.FindGameObjectsWithTag("Player");

            // Setup the test sceneario: create the desired positions for the stuff on the map
            // This example: tank0 is closer to box but opposite dir, tank1 is very far (no chance) but is in the good dir
            // Also, both tanks need health
            int numAITanks = 2;
            Vector3 desiredBoxPos               = new Vector3(5, 0, -30);
            BoxType desiredBoxType              = BoxType.BOXTYPE_HEALTH;
            Vector3[] desiredAITanksPosition    = { new Vector3(1, 0, -20), new Vector3(-13, 0, -18) };
            Vector3[] desiredAITaksAvgVel       = { (desiredAITanksPosition[0] - desiredBoxPos), (desiredBoxPos - desiredAITanksPosition[1]) };
            float[] desiredHealthPercent        = { 0.5f, 0.5f };
            bool[] desiredShield                = { false, false};
            bool[] desiredWeaponUpgrade         = { false, false };
            float[] desiredAmmoPercent          = { 1.0f, 1.0f};

            // Then setup tanks spawning
            AITanksSpawnConfig[] scenarioConfig             = new AITanksSpawnConfig[numAITanks];
            gameManagerScript.m_useForcedSpawnpointsOrder   = true;
            
            for (int i = 0; i < numAITanks; i++)
            {
                scenarioConfig[i]                           = new AITanksSpawnConfig();
                scenarioConfig[i].pos                       = desiredAITanksPosition[i];
                scenarioConfig[i].avgVel                    = desiredAITaksAvgVel[i];
                scenarioConfig[i].rotation                  = Quaternion.LookRotation(scenarioConfig[i].avgVel, Vector3.up); // Must be always after avgVel !!!

                scenarioConfig[i].desiredAmmoPercent        = desiredAmmoPercent[i];
                scenarioConfig[i].desiredHealthPercent      = desiredHealthPercent[i];
                scenarioConfig[i].forceShield               = desiredShield[i];
                scenarioConfig[i].hasWeaponUpgrade          = desiredWeaponUpgrade[i];
            }
            gameManagerScript.m_forcedSpawnPointsOrder      = scenarioConfig;

            // Wait 1 frame to make spawning happen
            yield return null;

            // Then spawn some boxes at certain locations on the map
            boxesManager.spawnBox(desiredBoxType, desiredBoxPos, Quaternion.identity);

            // Check the probabilities if they are within some "correct" thresholds
            yield return null;

            foreach (Transform child in gameObj.transform)
            {
                //child is your child transform
                GameObject childGO = child.gameObject;
                string str = childGO.name;
            }


            yield return new WaitForSeconds(10.0f);
            //
        }
    }
}
