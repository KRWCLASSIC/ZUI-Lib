using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZUI.Dialogue;
using ZUI.Notifications;
using ZUI.PauseMenu;
using ZUI.Popups;

namespace ZUI.Example
{
    /// <summary>
    /// Helper routines and dialogue sequence builders for the Episode 2 (YozgatMapOlan)
    /// Telerick &amp; Jimmy dialogue showcase in <see cref="ExamplePauseMenuModule"/>.
    /// </summary>
    internal static class ExampleDialogueShowcase
    {
        public static void TriggerDialogueWithEpisodePrompt(string dialogueTitle, Func<ZUIDialogueSequence> sequenceFactory)
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.name == "YozgatMapOlan")
            {
                // Already in Episode 2! Skip active cutscene first, then teleport to After-Chase, then play
                Plugin.Instance.StartCoroutine(SkipTeleportAndPlayRoutine(sequenceFactory));
                return;
            }

            // In another episode (e.g. Episode 1 / ZortaganeMapOlan) or MainMenu
            ZUIPopup.ShowConfirmation(
                title: "Travel to Episode 2?",
                message: $"Telerick's original voice lines for '{dialogueTitle}' only exist in Episode 2 (YozgatMapOlan).\n\nWould you like to travel to Episode 2 and spawn at the After-Chase location so original audio can play?",
                yesText: "Travel (Episode 2)",
                noText: "Play Here (Subtitles)",
                onYes: () =>
                {
                    Plugin.Instance.StartCoroutine(TravelToEpisode2Routine(sequenceFactory));
                },
                onNo: () =>
                {
                    ZUIPauseMenu.Close();
                    var sequence = sequenceFactory();
                    sequence.Play();
                },
                titleHex: "#FFCA28",
                yesButtonHex: "#2E7D32",
                noButtonHex: "#455A64"
            );
        }

        private static IEnumerator TravelToEpisode2Routine(Func<ZUIDialogueSequence> sequenceFactory)
        {
            ZUIPauseMenu.Close();
            ZUIInfoText.Show("Travelling to Episode 2 (Yozgat)...", 4.0f);

            try
            {
                if (NetworkServer.active)
                {
                    MultiplayerNetworkManager.ChangeMapServer("YozgatMapOlan");
                }
                else
                {
                    SceneManager.LoadScene("YozgatMapOlan");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[TravelToEpisode2] Failed to change map: {ex}");
                yield break;
            }

            float timeout = 30f;
            float elapsed = 0f;

            // 1. Wait until the active scene is YozgatMapOlan
            while (elapsed < timeout && SceneManager.GetActiveScene().name != "YozgatMapOlan")
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            // 2. Wait until MultiplayerNetworkManager has fully loaded the map (after its 2s delay and player respawn)
            while (elapsed < timeout && (!MultiplayerNetworkManager.IsMapLoaded ||
                                         !PlayerController.PlayerClient.Enabled ||
                                         PlayerController.PlayerClient.Value == null))
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (elapsed >= timeout)
            {
                Plugin.Log.LogWarning("[TravelToEpisode2] Timed out waiting for YozgatMapOlan to load.");
                yield break;
            }

            // 3. Wait for the opening cutscene (startCinematic / YozgatFirst) to start and register
            float cutsceneWait = 0f;
            while (cutsceneWait < 2.0f && (CinematicHandler.ActiveCinematics == null || CinematicHandler.ActiveCinematics.Count == 0))
            {
                yield return new WaitForSeconds(0.1f);
                cutsceneWait += 0.1f;
            }

            Plugin.Log.LogInfo($"[TravelToEpisode2] Map loaded and cutscene detected ({CinematicHandler.ActiveCinematics?.Count ?? 0} active). Starting skip & teleport routine...");

            // 4. Run sequence in strict order: 1. Skip cutscene, 2. Teleport to After-Chase, 3. Trigger dialogue!
            yield return SkipTeleportAndPlayRoutine(sequenceFactory);
        }

        /// <summary>
        /// Executes the sequence in strict order:
        /// 1. Skips active cutscene so scene triggers and camera don't break.
        /// 2. Teleports player to the After-Chase coordinate (100, 6, -135).
        /// 3. Starts the dialogue sequence with full audio.
        /// </summary>
        private static IEnumerator SkipTeleportAndPlayRoutine(Func<ZUIDialogueSequence> sequenceFactory)
        {
            ZUIPauseMenu.Close();

            // 1. FIRST: Skip active cutscenes
            Plugin.Log.LogInfo("[Episode2] Step 1: Skipping cutscene...");
            SkipActiveCutscenes();

            // Give the engine, cutscene callbacks, and camera a moment to finish ending the cutscene
            yield return new WaitForSeconds(0.4f);

            // Redundant check in case a delayed director or cinematic started on the following frame
            SkipActiveCutscenes();
            yield return new WaitForSeconds(0.1f);

            // 2. SECOND: Teleport player to After-Chase location from SRHM (100, 6, -135)
            Plugin.Log.LogInfo("[Episode2] Step 2: Teleporting player to After-Chase location (100, 6, -135)...");
            if (PlayerController.PlayerClient.Enabled && PlayerController.PlayerClient.Value != null)
            {
                PlayerController.PlayerClient.Value.Teleport(new Vector3(100f, 6f, -135f));
            }

            // Wait for transform synchronization and collision settling
            yield return new WaitForSeconds(0.3f);

            // 3. FINALLY: Trigger dialogue sequence!
            Plugin.Log.LogInfo("[Episode2] Step 3: Triggering dialogue sequence...");
            var sequence = sequenceFactory();
            sequence.Play();
        }

        /// <summary>
        /// Skips any currently active cutscenes using the game's native CinematicHandler registry
        /// (matching vanilla CinematicSkipper behavior) and releases the player cutscene lock.
        /// </summary>
        private static bool SkipActiveCutscenes()
        {
            bool skippedAny = false;

            try
            {
                if (CinematicHandler.ActiveCinematics != null && CinematicHandler.ActiveCinematics.Count > 0)
                {
                    var active = CinematicHandler.ActiveCinematics.ToArray();
                    for (int i = 0; i < active.Length; i++)
                    {
                        if (active[i] != null)
                        {
                            Plugin.Log.LogInfo($"[Cutscene] Skipping CinematicHandler: {active[i].name}");
                            active[i].SkipShared();
                            skippedAny = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[Cutscene] Error skipping ActiveCinematics: {ex.Message}");
            }

            if (!skippedAny)
            {
                try
                {
                    if (YozgatThink.HasYT() && YozgatThink.YT != null)
                    {
                        var field = typeof(YozgatThink).GetField("startCinematic", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (field?.GetValue(YozgatThink.YT) is CinematicHandler startCinematic && startCinematic != null)
                        {
                            Plugin.Log.LogInfo($"[Cutscene] Skipping YozgatThink.startCinematic directly: {startCinematic.name}");
                            startCinematic.SkipShared();
                            skippedAny = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[Cutscene] Error skipping startCinematic directly: {ex.Message}");
                }
            }

            try
            {
                if (PlayerController.PlayerClient.Enabled && PlayerController.PlayerClient.Value != null)
                {
                    PlayerController.PlayerClient.Value.InCutscene = false;
                }
            }
            catch { }

            return skippedAny;
        }

        public static ZUIDialogueSequence BuildElevatorTrickSequence()
        {
            return ZUIDialogueSequence.Create()
                .AddLine(
                    speaker: "Telerick",
                    message: "Pfffff… I tricked you. Do you think it could be that easy? Look around.",
                    speakerHex: "#FFCA28",
                    duration: 5.0f,
                    delayBefore: 0.2f,
                    audioName: "sub_telerick_tricked_0"
                )
                .AddLine(
                    speaker: "Jimmy",
                    message: "Wait, what?! You said this elevator was our ticket out of here!",
                    speakerHex: "#4FC3F7",
                    duration: 3.5f,
                    delayBefore: 0.3f
                )
                .AddLine(
                    speaker: "Telerick",
                    message: "This is a strange quantum elevator. It needs a powerful energy source to work.",
                    speakerHex: "#FFCA28",
                    duration: 4.8f,
                    delayBefore: 0.3f,
                    audioName: "sub_telerick_tricked_1"
                )
                .AddLine(
                    speaker: "Jimmy",
                    message: "Great. So how do we get power back to this rust bucket?",
                    speakerHex: "#4FC3F7",
                    duration: 3.2f,
                    delayBefore: 0.3f
                )
                .AddLine(
                    speaker: "Telerick",
                    message: "Normally, I wouldn’t help you, but I’m starting to warm up to you.",
                    speakerHex: "#FFCA28",
                    duration: 3.5f,
                    delayBefore: 0.3f,
                    audioName: "sub_telerick_tricked_2"
                )
                .AddLine(
                    speaker: "Telerick",
                    message: "If you go to the three cabins around here and start the generators, you can supply the energy.",
                    speakerHex: "#FFCA28",
                    duration: 5.2f,
                    delayBefore: 0.3f,
                    audioName: "sub_telerick_tricked_3"
                )
                .AddLine(
                    speaker: "Telerick",
                    message: "Be nice to my neighbors' houses...",
                    speakerHex: "#FFCA28",
                    duration: 3.0f,
                    delayBefore: 0.4f,
                    audioName: "sub_telerick_tricked_4"
                )
                .WithOnComplete(() =>
                {
                    ZUIInfoText.Show("Objective: Restore Power to the Cabins", 3.5f);
                });
        }

        public static ZUIDialogueSequence BuildFirstMeetSequence()
        {
            return ZUIDialogueSequence.Create()
                .AddLine(
                    speaker: "Telerick",
                    message: "Anyway, no need to drag this out, I don’t understand your language.",
                    speakerHex: "#FFCA28",
                    duration: 4.0f,
                    delayBefore: 0.2f,
                    audioName: "sub_telerick_firstmeet_0"
                )
                .AddLine(
                    speaker: "Jimmy",
                    message: "Hey! Can you at least tell us where the exit is?",
                    speakerHex: "#4FC3F7",
                    duration: 3.2f,
                    delayBefore: 0.3f
                )
                .AddLine(
                    speaker: "Telerick",
                    message: "I can get you out of here. You can take the elevator behind me and leave from here.",
                    speakerHex: "#FFCA28",
                    duration: 4.2f,
                    delayBefore: 0.3f,
                    audioName: "sub_telerick_firstmeet_1"
                )
                .WithOnComplete(() =>
                {
                    ZUIInfoText.Show("Elevator unlocked behind Telerick", 3.0f);
                });
        }
    }
}

