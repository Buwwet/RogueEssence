﻿using System.Collections.Generic;
using System.IO;
using RogueElements;
using RogueEssence.Data;
using RogueEssence.Dungeon;
using RogueEssence.Script;

namespace RogueEssence.Menu
{
    public class QuicksaveChosenMenu : SingleStripMenu
    {

        private string recordDir;

        public QuicksaveChosenMenu(string dir)
        {
            this.recordDir = dir;

            List<MenuTextChoice> choices = new List<MenuTextChoice>();

            string fileName = recordDir.Substring(recordDir.LastIndexOf('/', recordDir.Length - 2) + 1);
            choices.Add(new MenuTextChoice(Text.FormatKey("MENU_SAVE_RESUME"), ReloadAction));
            if (DiagManager.Instance.DevMode)
                choices.Add(new MenuTextChoice(Text.FormatKey("MENU_REPLAY_REPLAY"), ReplayAction));
            choices.Add(new MenuTextChoice(Text.FormatKey("MENU_DELETE"), DeleteAction));

            choices.Add(new MenuTextChoice(Text.FormatKey("MENU_EXIT"), ExitAction));

            Initialize(new Loc(240, 0), CalculateChoiceLength(choices, 72), choices.ToArray(), 0);
        }
        
        private void cannotRead()
        {
            MenuManager.Instance.AddMenu(MenuManager.Instance.CreateDialogue(Text.FormatKey("DLG_ERR_READ_FILE"),
                Text.FormatKey("DLG_ERR_READ_FILE_FALLBACK", recordDir)), false);
        }

        private void ReloadAction()
        {
            ReplayData replay = DataManager.Instance.LoadReplay(recordDir, true);
            if (replay == null)
                cannotRead();
            else
            {
                MenuManager.Instance.ClearMenus();
                GameManager.Instance.SceneOutcome = Reload(replay);
            }
        }

        private void ReplayAction()
        {
            ReplayData replay = DataManager.Instance.LoadReplay(recordDir, false);
            if (replay == null)
                cannotRead();
            else
            {
                MenuManager.Instance.ClearMenus();
                GameManager.Instance.SceneOutcome = Replay(replay);
            }
        }

        private void DeleteAction()
        {
            if (File.Exists(recordDir))
                File.Delete(recordDir);

            MenuManager.Instance.RemoveMenu();

            if (DataManager.Instance.FoundRecords(PathMod.NoMod(DataManager.ROGUE_PATH)))
                MenuManager.Instance.ReplaceMenu(new QuicksaveMenu());
            else
            {
                MenuManager.Instance.RemoveMenu();
                MenuManager.Instance.ReplaceMenu(new TopMenu());
            }
        }

        private void ExitAction()
        {
            MenuManager.Instance.RemoveMenu();
        }

        public IEnumerator<YieldInstruction> Reload(ReplayData replay)
        {
            GameManager.Instance.BGM("", true);
            yield return CoroutineManager.Instance.StartCoroutine(GameManager.Instance.FadeOut(false));

            DataManager.Instance.MsgLog.Clear();
            GameState state = replay.ReadState();
            DataManager.Instance.SetProgress(state.Save);
            LuaEngine.Instance.LoadSavedData(DataManager.Instance.Save); //notify script engine
            ZoneManager.LoadFromState(state.Zone);
            LuaEngine.Instance.UpdateZoneInstance();

            //NOTE: In order to preserve debug consistency, you SHOULD set the language to that of the quicksave.
            //HOWEVER, it would be too inconvenient for players sharing their quicksaves, thus this feature is LEFT OUT.

            DataManager.Instance.Loading = DataManager.LoadMode.Loading;

            DataManager.Instance.CurrentReplay = replay;


            if (DataManager.Instance.Save.NextDest.IsValid())
            {
                yield return CoroutineManager.Instance.StartCoroutine(GameManager.Instance.MoveToZone(DataManager.Instance.Save.NextDest));
            }
            else
            {
                DataManager.Instance.ResumePlay(DataManager.Instance.CurrentReplay.RecordDir, DataManager.Instance.CurrentReplay.QuicksavePos);
                DataManager.Instance.CurrentReplay = null;

                GameManager.Instance.SetFade(true, false);

                DataManager.Instance.Loading = DataManager.LoadMode.None;

                if (ZoneManager.Instance.CurrentMapID.Segment > -1)
                {
                    GameManager.Instance.MoveToScene(Dungeon.DungeonScene.Instance);
                    GameManager.Instance.BGM(ZoneManager.Instance.CurrentMap.Music, true);
                }
                else
                {
                    GameManager.Instance.MoveToScene(Ground.GroundScene.Instance);
                    GameManager.Instance.BGM(ZoneManager.Instance.CurrentGround.Music, true);
                }

                yield return CoroutineManager.Instance.StartCoroutine(GameManager.Instance.FadeIn());
            }
        }

        public IEnumerator<YieldInstruction> Replay(ReplayData replay)
        {
            GameManager.Instance.BGM("", true);
            yield return CoroutineManager.Instance.StartCoroutine(GameManager.Instance.FadeOut(false));

            DataManager.Instance.MsgLog.Clear();
            GameState state = replay.ReadState();
            DataManager.Instance.SetProgress(state.Save);
            LuaEngine.Instance.LoadSavedData(DataManager.Instance.Save); //notify script engine
            ZoneManager.LoadFromState(state.Zone);
            LuaEngine.Instance.UpdateZoneInstance();

            DataManager.Instance.CurrentReplay = replay;
            
            if (DataManager.Instance.Save.NextDest.IsValid())
            {
                yield return CoroutineManager.Instance.StartCoroutine(GameManager.Instance.MoveToZone(DataManager.Instance.Save.NextDest));
            }
            else
            {
                DataManager.Instance.ResumePlay(DataManager.Instance.CurrentReplay.RecordDir, DataManager.Instance.CurrentReplay.QuicksavePos);
                DataManager.Instance.CurrentReplay = null;

                GameManager.Instance.SetFade(true, false);

                DataManager.Instance.Loading = DataManager.LoadMode.None;

                if (ZoneManager.Instance.CurrentMapID.Segment > -1)
                {
                    GameManager.Instance.MoveToScene(Dungeon.DungeonScene.Instance);
                    GameManager.Instance.BGM(ZoneManager.Instance.CurrentMap.Music, true);
                }
                else
                {
                    GameManager.Instance.MoveToScene(Ground.GroundScene.Instance);
                    GameManager.Instance.BGM(ZoneManager.Instance.CurrentGround.Music, true);
                }
                Content.GraphicsManager.GlobalIdle = Content.GraphicsManager.IdleAction;

                yield return CoroutineManager.Instance.StartCoroutine(GameManager.Instance.FadeIn());
            }
        }
    }
}
